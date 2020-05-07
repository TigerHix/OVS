﻿using System;
using System.Runtime.InteropServices;
using System.Threading;
 using Live2D.Cubism.Core;
 using Microsoft.Win32.SafeHandles;
using UnityEngine;
 using UnityEngine.Events;
 using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class OpenFaceTransfer : MonoBehaviour
{
    private static Thread opThread;

    public Image ofTrackingPreview;

    public bool IsInitialized { get; private set; }
    public bool IsTracking { get; private set; }
    public float DetectionCertainty { get; private set; }
    public Vector3 HeadPosition { get; private set; }
    public Vector3 HeadRotation { get; private set; }
    public Vector2 EyesGazeAngle { get; private set; }
    public Vector3 LeftEyeGazeDirection { get; private set; }
    public Vector3 RightEyeGazeDirection { get; private set; }

    public UnityEvent OnTrackingInitialized { get; } = new UnityEvent();
    public UnityEvent OnTrackingStarted { get; } = new UnityEvent();
    public UnityEvent OnTrackingLost { get; } = new UnityEvent();

    private void Awake()
    {
        tex = new Texture2D(640, 480, TextureFormat.ARGB32, false);
        pixel32 = tex.GetPixels32();
        // Pin pixel32 array
        pixelHandle = GCHandle.Alloc(pixel32, GCHandleType.Pinned);
        // Get the pinned address
        pixelPtr = pixelHandle.AddrOfPinnedObject();

        ofTrackingPreview.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero, 100f, 0U, SpriteMeshType.FullRect);
    }

    private void Start()
    {
        // Not running?
        if (opThread == null)
        {
            print("Starting daemon");
            opThread = new Thread(OpThread);
            opThread.Start();
        }
    }

    private void OpThread()
    {
        OpEntry("C:\\OVS\\OVS\\Assets\\Plugins\\FaceLandmarkVid.dll");
    }
    
    private Texture2D tex;
    private Color32[] pixel32;

    private GCHandle pixelHandle;
    private IntPtr pixelPtr;

    private unsafe void Update()
    {
        using (TransferOpData(OpDataType.DetectionCertainty, out var certaintyData, out _))
        {
            // Actual certainty is in the range -1~1.
            DetectionCertainty = Mathf.Max(0, (certaintyData[0] - 0.4f) / (1 - 0.4f));
            if (DetectionCertainty > 0f)
            {
                using (TransferOpData(OpDataType.PoseEstimate, out var data, out _))
                {
                    HeadPosition = new Vector3(data[0], data[1], data[2]);
                    HeadRotation = new Vector3(data[3], data[4], data[5]);
                }

                using (TransferOpData(OpDataType.GazeDirections, out var data, out _))
                {
                    EyesGazeAngle = new Vector2(data[0], data[1]);
                    LeftEyeGazeDirection = new Vector3(data[2], data[3], data[4]);
                    RightEyeGazeDirection = new Vector3(data[5], data[6], data[7]);
                }
                
                if (!IsTracking)
                {
                    IsTracking = true;
                    OnTrackingStarted.Invoke();
                    if (!IsInitialized)
                    {
                        print("OpenFace initialized");
                        IsInitialized = true;
                        OnTrackingInitialized.Invoke();
                    }
                }
            }
            else
            {
                if (IsTracking)
                {
                    IsTracking = false;
                    OnTrackingLost.Invoke();
                }
            }
        }

        // Convert Mat to Texture2D
        TransferRawCapturedImage(pixelPtr, tex.width, tex.height);
        
        // Update the Texture2D with array updated in C++
        tex.SetPixels32(pixel32);
        tex.Apply();
    }

    private void OnApplicationQuit()
    {
        opThread?.Abort();
        opThread = null;
        OpExit();
        pixelHandle.Free();
    }

    private enum OpDataType
    {
        DetectionCertainty = 0,
        GazeDirections = 1,
        PoseEstimate = 2,
        DetectedLandmarks = 3
    }
    
    private static unsafe OpDataSafeHandle TransferOpData(OpDataType type, out float* data, out int length)
    {
        if (!OpTransferData((int) type, out var floatsHandle, out data, out length))
        {
            throw new InvalidOperationException();
        }
        return floatsHandle;
    }

    private class OpDataSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public OpDataSafeHandle() : base(true)
        {
        }

        protected override bool ReleaseHandle()
        {
            return OpReleaseData(handle);
        }
    }
    
    [DllImport("FaceLandmarkVid.dll", EntryPoint = "Start", CallingConvention = CallingConvention.StdCall)]
    static extern int OpEntry(string directory);
    
    [DllImport("FaceLandmarkVid.dll", EntryPoint = "Quit", CallingConvention = CallingConvention.StdCall)]
    static extern void OpExit();
    
    [DllImport("FaceLandmarkVid.dll", EntryPoint = "StatusCode", CallingConvention = CallingConvention.StdCall)]
    static extern int OpStatusCode();
    
    [DllImport("FaceLandmarkVid.dll", EntryPoint = "TransferRawCapturedImage", CallingConvention = CallingConvention.StdCall)]
    static extern void TransferRawCapturedImage(IntPtr data, int width, int height);
    
    [DllImport("FaceLandmarkVid.dll", EntryPoint = "TransferData", CallingConvention = CallingConvention.StdCall)]
    static extern unsafe bool OpTransferData(int id, out OpDataSafeHandle handle, out float* data, out int length);

    [DllImport("FaceLandmarkVid.dll", EntryPoint = "ReleaseData", CallingConvention = CallingConvention.StdCall)]
    static extern bool OpReleaseData(IntPtr itemsHandle);

}
