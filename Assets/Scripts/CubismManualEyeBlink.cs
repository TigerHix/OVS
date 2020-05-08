using Cysharp.Threading.Tasks;
using Live2D.Cubism.Framework;
using UnityEditor;
using UnityEngine;

/**
 * Based on CubismAutoEyeBlinkInput.
 */
public class CubismManualEyeBlink : MonoBehaviour
{
    
    /// <summary>
    /// Timescale.
    /// </summary>
    [SerializeField, Range(1f, 20f)]
    public float Timescale = 10f;
    
    /// <summary>
    /// Target controller.
    /// </summary>
    private CubismEyeBlinkController Controller { get; set; }

    /// <summary>
    /// Time until next eye blink.
    /// </summary>
    private float T { get; set; } = float.MaxValue;

    /// <summary>
    /// Control over whether output should be evaluated.
    /// </summary>
    private Phase CurrentPhase { get; set; } = Phase.Idling;

    /// <summary>
    /// Used for switching from <see cref="Phase.ClosingEyes"/> to <see cref="Phase.OpeningEyes"/> and back to <see cref="Phase.Idling"/>.
    /// </summary>
    private float LastValue { get; set; }

    #region Unity Event Handling

    /// <summary>
    /// Called by Unity. Initializes input.
    /// </summary>
    private void Start()
    {
        Controller = GetComponent<CubismEyeBlinkController>();
    }

    public async void Close()
    {
        if (CurrentPhase != Phase.Idling) return;
        
        T = Mathf.PI * -0.5f;
        LastValue = 1f;
        CurrentPhase = Phase.ClosingEyes;
        
        while (CurrentPhase == Phase.ClosingEyes)
        {
            // Evaluate eye blinking.
            T += (Time.deltaTime * Timescale);
            var value = Mathf.Abs(Mathf.Sin(T));

            if (value > LastValue)
            {
                value = 0f;
                CurrentPhase = Phase.ClosedEyes;
            }

            Controller.EyeOpening = value;
            LastValue = value;

            await UniTask.Yield(PlayerLoopTiming.PostLateUpdate);
        }
    }

    public async void Open()
    {
        if (CurrentPhase != Phase.ClosedEyes) return;

        CurrentPhase = Phase.OpeningEyes;
        
        while (CurrentPhase == Phase.OpeningEyes)
        {
            // Evaluate eye blinking.
            T += (Time.deltaTime * Timescale);
            var value = Mathf.Abs(Mathf.Sin(T));
            
            if (value < LastValue)
            {
                value = 1f;
                CurrentPhase = Phase.Idling;
                T = float.MaxValue;
            }
            
            Controller.EyeOpening = value;
            LastValue = value;
            
            await UniTask.Yield(PlayerLoopTiming.PostLateUpdate);
        }
    }

    #endregion

    /// <summary>
    /// Internal states.
    /// </summary>
    private enum Phase
    {
        /// <summary>
        /// Idle state.
        /// </summary>
        Idling,

        /// <summary>
        /// State when closing eyes.
        /// </summary>
        ClosingEyes,
        
        ClosedEyes,

        /// <summary>
        /// State when opening eyes.
        /// </summary>
        OpeningEyes
    }
}

#if UNITY_EDITOR

[CustomEditor(typeof(CubismManualEyeBlink))]
public class CubismManualEyeBlinkEditor : Editor
{
    public override async void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var component = (CubismManualEyeBlink) target;

        if (GUILayout.Button("Blink"))
        {
            component.Close();
        }
    }
}

#endif