﻿using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class OpenFaceTransfer : MonoBehaviour
{
    private static Thread opThread;
    private static readonly int[] AvailableActionUnits = {1, 2, 4, 5, 6, 7, 9, 10, 12, 14, 15, 17, 20, 23, 25, 26, 45};
    
    public Image ofTrackingPreview;

    public bool IsInitialized { get; private set; }
    public bool IsTracking { get; private set; }
    public float DetectionCertainty { get; private set; }

    public Vector3 HeadPosition { get; private set; }
    public Vector3 HeadRotation { get; private set; }
    public Vector2 EyesGazeAngle { get; private set; }
    public Vector3 LeftEyeGazeDirection { get; private set; }
    public Vector3 RightEyeGazeDirection { get; private set; }
    public Vector3[] LandmarkPositions { get; private set; }
    public float[] ActionUnitIntensities { get; private set; }
   
    public UnityEvent OnOpenFaceInitialized { get; } = new UnityEvent();
    public UnityEvent OnTrackingStarted { get; } = new UnityEvent();
    public UnityEvent OnTrackingLost { get; } = new UnityEvent();

    private void Start()
    {
        // Not running?
        if (opThread == null)
        {
            print("Initializing OpenFace");
            
            tex = new Texture2D(640, 480, TextureFormat.ARGB32, false);
            ofTrackingPreview.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero, 100f, 0U, SpriteMeshType.FullRect);

            pixel32 = tex.GetPixels32();
            // Pin pixel32 array
            pixelHandle = GCHandle.Alloc(pixel32, GCHandleType.Pinned);
            // Get the pinned address
            pixelPtr = pixelHandle.AddrOfPinnedObject();

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
        if (OpStatusCode() < 0) return;
        
        if (!IsInitialized)
        {
            print("OpenFace initialized");
            IsInitialized = true;
            OnOpenFaceInitialized.Invoke();
        }
            
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
                
                using (TransferOpData(OpDataType.DetectedLandmarks, out var data, out var length))
                {
                    Assert.AreEqual(68 * 3, length);
                    LandmarkPositions = new Vector3[68];
                    var p = 0;
                    for (var i = 0; i < 68; i++)
                    {
                        LandmarkPositions[i] = new Vector3(data[p], data[p + 1], data[p + 2]);
                        p += 3;
                    }
                }

                using (TransferOpData(OpDataType.ActionUnits, out var data, out var length))
                {
                    Assert.AreEqual(17, length);
                    ActionUnitIntensities = new float[65];
                    var p = 0;
                    foreach (var index in AvailableActionUnits)
                    {
                        ActionUnitIntensities[index] = data[p++];
                    }
                }
                
                if (!IsTracking)
                {
                    IsTracking = true;
                    OnTrackingStarted.Invoke();
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
        if (pixelHandle.IsAllocated)
        {
            pixelHandle.Free();
        }
    }

    private enum OpDataType
    {
        DetectionCertainty = 0,
        GazeDirections = 1,
        PoseEstimate = 2,
        DetectedLandmarks = 3,
        ActionUnits = 4
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
