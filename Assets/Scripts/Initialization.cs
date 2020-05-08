using Cysharp.Threading.Tasks;
using UnityEngine;

public class Initialization : MonoBehaviour
{
    private void Start()
    {
        var playerLoop = UnityEngine.LowLevel.PlayerLoop.GetCurrentPlayerLoop();
        PlayerLoopHelper.Initialize(ref playerLoop);
    }
}