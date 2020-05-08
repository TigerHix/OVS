using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class DebugInformation : MonoBehaviour
{
    public Text text;
    public OpenFaceTransfer ofTransfer;
    
    public void Update()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Is initialized: {ofTransfer.IsInitialized}");
        if (ofTransfer.IsInitialized)
        {
            sb.AppendLine($"Is tracking: {ofTransfer.IsTracking}");
            sb.AppendLine($"Detection certainty: {ofTransfer.DetectionCertainty}");
            sb.AppendLine();

            if (ofTransfer.IsTracking)
            {
                sb.AppendLine($"Head position: {ofTransfer.HeadPosition}");
                sb.AppendLine($"Head rotation: {ofTransfer.HeadRotation}");
                sb.AppendLine($"Eyes gaze angle: {ofTransfer.EyesGazeAngle}");
                sb.AppendLine("Action units:");
                sb.AppendLine($"\tAU01 - Inner brow raiser: {ofTransfer.ActionUnitIntensities[1]}");
                sb.AppendLine($"\tAU02 - Outer brow raiser: {ofTransfer.ActionUnitIntensities[2]}");
                sb.AppendLine($"\tAU04 - Brow lowerer: {ofTransfer.ActionUnitIntensities[4]}");
                sb.AppendLine($"\tAU05 - Upper lid raiser: {ofTransfer.ActionUnitIntensities[5]}");
                sb.AppendLine($"\tAU06 - Cheek raiser: {ofTransfer.ActionUnitIntensities[6]}");
                sb.AppendLine($"\tAU07 - Lid tightener: {ofTransfer.ActionUnitIntensities[7]}");
                sb.AppendLine($"\tAU09 - Nose wrinkler: {ofTransfer.ActionUnitIntensities[9]}");
                sb.AppendLine($"\tAU10 - Upper lip raiser: {ofTransfer.ActionUnitIntensities[10]}");
                sb.AppendLine($"\tAU12 - Lip corner puller: {ofTransfer.ActionUnitIntensities[12]}");
                sb.AppendLine($"\tAU14 - Dimpler: {ofTransfer.ActionUnitIntensities[14]}");
                sb.AppendLine($"\tAU15 - Lip corner depressor: {ofTransfer.ActionUnitIntensities[15]}");
                sb.AppendLine($"\tAU17 - Chin raiser: {ofTransfer.ActionUnitIntensities[17]}");
                sb.AppendLine($"\tAU20 - Lip stretcher: {ofTransfer.ActionUnitIntensities[20]}");
                sb.AppendLine($"\tAU23 - Lip tightener: {ofTransfer.ActionUnitIntensities[23]}");
                sb.AppendLine($"\tAU25 - Lips apart: {ofTransfer.ActionUnitIntensities[25]}");
                sb.AppendLine($"\tAU26 - Jaw drop: {ofTransfer.ActionUnitIntensities[26]}");
                sb.AppendLine($"\tAU45 - Blink: {ofTransfer.ActionUnitIntensities[45]}");
            }
        }

        text.text = sb.ToString();
        LayoutRebuilder.ForceRebuildLayoutImmediate(transform.parent.transform as RectTransform);
    }
}