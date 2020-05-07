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

    public float headAngleMultiplier = 2f;
    public float bodyAngleMultiplier = 0.8f;
    public float eyeBallMultiplier = 1f;
    public float tweenDuration = 0.2f;
    public float eyesTweenDuration = 0.6f;

    private Vector3 headOriginPosition = Vector3.zero;
    private Vector3 headOriginRotation = Vector3.zero;
    private Vector2 eyesGazeOriginAngle = Vector2.zero;
    
    private CubismParameter paramAngleX;
    private CubismParameter paramAngleY;
    private CubismParameter paramAngleZ;
    private CubismParameter paramBodyAngleZ;
    private CubismParameter paramEyeBallX;
    private CubismParameter paramEyeBallY;

    private void Awake()
    {
        paramAngleX = model.Parameters.FindById(paramAngleXKey);
        paramAngleY = model.Parameters.FindById(paramAngleYKey);
        paramAngleZ = model.Parameters.FindById(paramAngleZKey);
        paramBodyAngleZ = model.Parameters.FindById(paramBodyAngleZKey);
        paramEyeBallX = model.Parameters.FindById(paramEyeBallXKey);
        paramEyeBallY = model.Parameters.FindById(paramEyeBallYKey);
        calibrateHeadOriginButton.onClick.AddListener(OnCalibrateHeadOrigin);
        ofTransfer.OnTrackingInitialized.AddListener(OnCalibrateHeadOrigin);
    }

    private void OnCalibrateHeadOrigin()
    {
        headOriginPosition = ofTransfer.HeadPosition;
        headOriginRotation = ofTransfer.HeadRotation;
        eyesGazeOriginAngle = ofTransfer.EyesGazeAngle;
    }

    private void LateUpdate()
    {
        DOTween.To(() => paramAngleX.Value, v => paramAngleX.Value = v,
            (ofTransfer.HeadRotation.y - headOriginRotation.y) * Mathf.Rad2Deg * headAngleMultiplier, tweenDuration);
        DOTween.To(() => paramAngleY.Value, v => paramAngleY.Value = v,
            -(ofTransfer.HeadRotation.x - headOriginRotation.x) * Mathf.Rad2Deg * headAngleMultiplier, tweenDuration);
        DOTween.To(() => paramAngleZ.Value, v => paramAngleZ.Value = v,
            -(ofTransfer.HeadRotation.z - headOriginRotation.z) * Mathf.Rad2Deg * headAngleMultiplier, tweenDuration);
        
        DOTween.To(() => paramBodyAngleZ.Value, v => paramBodyAngleZ.Value = v,
            -(ofTransfer.HeadRotation.z - headOriginRotation.z) * Mathf.Rad2Deg * bodyAngleMultiplier, tweenDuration);
        
        DOTween.To(() => paramEyeBallX.Value, v => paramEyeBallX.Value = v,
            -(ofTransfer.EyesGazeAngle.x - eyesGazeOriginAngle.x) * eyeBallMultiplier, eyesTweenDuration);
        DOTween.To(() => paramEyeBallY.Value, v => paramEyeBallY.Value = v,
            -(ofTransfer.EyesGazeAngle.y - eyesGazeOriginAngle.y) * eyeBallMultiplier, eyesTweenDuration);
    }
    
}