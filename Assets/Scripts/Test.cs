using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;
using UniRx.Async;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Test : MonoBehaviour
{

    [DllImport("FaceLandmarkVid.dll", EntryPoint = "Start", CallingConvention = CallingConvention.StdCall)]
    static extern int OpEntry(string directory);
    
    [DllImport("FaceLandmarkVid.dll", EntryPoint = "Quit", CallingConvention = CallingConvention.StdCall)]
    static extern void OpExit();
    
    [DllImport("FaceLandmarkVid.dll", EntryPoint = "StatusCode", CallingConvention = CallingConvention.StdCall)]
    static extern int OpStatusCode();

    private static Thread opThread;
    
    private async void Start()
    {
        print("wtf? ");
        //await UniTask.Delay(TimeSpan.FromSeconds(1));
        print("Status: " + OpStatusCode());
        
        // Not running?
        if (OpStatusCode() < 0)
        {
            opThread = new Thread(OpThread);
            opThread.Start();
        }
    }

    private void OpThread()
    {
        OpEntry("C:\\OVS\\OVS\\Assets\\Plugins\\FaceLandmarkVid.dll");
    }

    private unsafe void Update()
    {
        using (TransferOpData(OpDataType.DetectionCertainty, out var certaintyData, out _))
        {
            //print("Certainty: " + certaintyData[0]);
            if (certaintyData[0] > 0.4f)
            {
                using (TransferOpData(OpDataType.PoseEstimate, out var poseData, out _))
                {
                    var message = "";
                    for (var i = 0; i < 6; i++) message += poseData[i] + ", ";
                    message = message.Substring(0, message.Length - 2);
                    print($"Pose estimate: ({message})");
                }
            }
        }
    }

    private void OnApplicationQuit()
    {
    }
    
    [DllImport("FaceLandmarkVid.dll", EntryPoint = "TransferData", CallingConvention = CallingConvention.StdCall)]
    static extern unsafe bool OpTransferData(int id, out OpDataSafeHandle handle, out float* data, out int length);

    [DllImport("FaceLandmarkVid.dll", EntryPoint = "ReleaseData", CallingConvention = CallingConvention.StdCall)]
    static extern bool OpReleaseData(IntPtr itemsHandle);
    
    static unsafe OpDataSafeHandle TransferOpData(OpDataType type, out float* data, out int length)
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

    private enum OpDataType
    {
        DetectionCertainty = 0,
        GazeDirections = 1,
        PoseEstimate = 2,
        DetectedLandmarks = 3
    }

}
