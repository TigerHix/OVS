using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Live2D.Cubism.Core;
using UnityEngine;
using UnityEngine.UI;

public class ModelController : MonoBehaviour
{
    
    public CubismModel model;

    public OpenFaceTransfer ofTransfer;
    public Button calibrateHeadOriginButton;

    public string paramAngleXKey = "PARAM_ANGLE_X";
    public string paramAngleYKey = "PARAM_ANGLE_Y";
    public string paramAngleZKey = "PARAM_ANGLE_Z";
    public string paramBodyAngleZKey = "PARAM_BODY_ANGLE_Z";
    public string paramEyeBallXKey = "PARAM_EYE_BALL_X";
    public string paramEyeBallYKey = "PARAM_EYE_BALL_Y";
    public string paramBrowLYKey = "PARAM_BROW_L_Y";
    public string paramBrowRYKey = "PARAM_BROW_R_Y";
    public string paramMouthOpenYKey = "PARAM_MOUTH_OPEN_Y";
    public string paramMouthFormKey = "PARAM_MOUTH_FORM";

    public float headAngleMultiplier = 2f;
    public float bodyAngleMultiplier = 0.8f;
    public float eyeBallMultiplier = 1f;
    public float browMultiplier = 1f;
    public float mouthOpenMultiplier = 1f;
    public float mouthFormMultiplier = 1f;
    public float tweenDuration = 0.2f;
    public float eyesTweenDuration = 0.6f;
    public float browsTweenDuration = 0.1f;
    public float mouthTweenDuration = 0.1f;
    public Dictionary<int, float> auSmoothingFrames = new Dictionary<int, float>
    {
        {1, 12},
        {12, 4},
        {23, 4},
        {25, 4}
    };

    private Vector3 headOriginPosition = Vector3.zero;
    private Vector3 headOriginRotation = Vector3.zero;
    private Vector2 eyesGazeOriginAngle = Vector2.zero;
    private float[] originAu;
    private readonly Queue<float>[] lastAu = new Queue<float>[65];
    
    private CubismParameter paramAngleX;
    private CubismParameter paramAngleY;
    private CubismParameter paramAngleZ;
    private CubismParameter paramBodyAngleZ;
    private CubismParameter paramEyeBallX;
    private CubismParameter paramEyeBallY;
    private CubismParameter paramBrowLY;
    private CubismParameter paramBrowRY;
    private CubismParameter paramMouthOpenY;
    private CubismParameter paramMouthForm;
    
    private bool isCalibrated;

    private void Awake()
    {
        for (var i = 0; i < lastAu.Length; i++) lastAu[i] = new Queue<float>();
        paramAngleX = model.Parameters.FindById(paramAngleXKey);
        paramAngleY = model.Parameters.FindById(paramAngleYKey);
        paramAngleZ = model.Parameters.FindById(paramAngleZKey);
        paramBodyAngleZ = model.Parameters.FindById(paramBodyAngleZKey);
        paramEyeBallX = model.Parameters.FindById(paramEyeBallXKey);
        paramEyeBallY = model.Parameters.FindById(paramEyeBallYKey);
        paramBrowLY = model.Parameters.FindById(paramBrowLYKey);
        paramBrowRY = model.Parameters.FindById(paramBrowRYKey);
        paramMouthOpenY = model.Parameters.FindById(paramMouthOpenYKey);
        paramMouthForm = model.Parameters.FindById(paramMouthFormKey);
        calibrateHeadOriginButton.onClick.AddListener(OnCalibratePoseOrigin);
    }

    private void OnCalibratePoseOrigin()
    {
        headOriginPosition = ofTransfer.HeadPosition;
        headOriginRotation = ofTransfer.HeadRotation;
        eyesGazeOriginAngle = ofTransfer.EyesGazeAngle;
        originAu = new float[65];
        ofTransfer.ActionUnitIntensities.CopyTo(originAu, 0);
    }

    private void LateUpdate()
    {
        if (!ofTransfer.IsInitialized) return;

        if (!isCalibrated)
        {
            isCalibrated = true;
            OnCalibratePoseOrigin();
        }
        
        // Face angle
        DOTween.To(() => paramAngleX.Value, v => paramAngleX.Value = v,
            (ofTransfer.HeadRotation.y - headOriginRotation.y) * Mathf.Rad2Deg * headAngleMultiplier, tweenDuration);
        DOTween.To(() => paramAngleY.Value, v => paramAngleY.Value = v,
            -(ofTransfer.HeadRotation.x - headOriginRotation.x) * Mathf.Rad2Deg * headAngleMultiplier, tweenDuration);
        DOTween.To(() => paramAngleZ.Value, v => paramAngleZ.Value = v,
            -(ofTransfer.HeadRotation.z - headOriginRotation.z) * Mathf.Rad2Deg * headAngleMultiplier, tweenDuration);
        
        // Body angle
        DOTween.To(() => paramBodyAngleZ.Value, v => paramBodyAngleZ.Value = v,
            -(ofTransfer.HeadRotation.z - headOriginRotation.z) * Mathf.Rad2Deg * bodyAngleMultiplier, tweenDuration);
        
        // Eye ball angle
        DOTween.To(() => paramEyeBallX.Value, v => paramEyeBallX.Value = v,
            -(ofTransfer.EyesGazeAngle.x - eyesGazeOriginAngle.x) * eyeBallMultiplier, eyesTweenDuration);
        DOTween.To(() => paramEyeBallY.Value, v => paramEyeBallY.Value = v,
            -(ofTransfer.EyesGazeAngle.y - eyesGazeOriginAngle.y) * eyeBallMultiplier, eyesTweenDuration);
        
        // Eyebrow raise
        DOTween.To(() => paramBrowLY.Value, 
            v => {
                paramBrowLY.Value = v;
                paramBrowRY.Value = v;
            },
            AuRollingAverage(1) * browMultiplier,
            browsTweenDuration);
        
        // Mouth open
        DOTween.To(() => paramMouthOpenY.Value, v => paramMouthOpenY.Value = v,
            AuRollingAverage(25) * mouthOpenMultiplier, mouthTweenDuration);

        // Mouth form
        DOTween.To(() => paramMouthForm.Value, v => paramMouthForm.Value = v,
            (AuRollingAverage(12) - AuRollingAverage(23) * 3) * mouthFormMultiplier, mouthTweenDuration);

        for (var i = 0; i < lastAu.Length; i++)
        {
            if (!auSmoothingFrames.ContainsKey(i)) continue;
            var q = lastAu[i];
            q.Enqueue(ofTransfer.ActionUnitIntensities[i]);
            if (q.Count > auSmoothingFrames[i])
            {
                q.Dequeue();
            }
        }
    }
    
    private float AuRollingAverage(int i)
    {
        return lastAu[i].Count < auSmoothingFrames[i] ? originAu[i] : lastAu[i].Average() - originAu[i];
    }
    
}