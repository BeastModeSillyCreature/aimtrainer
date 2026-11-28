using UnityEngine;
#if USE_ONNX
using Unity.InferenceEngine;
#endif

public enum PredictionServiceType
{
    Heuristic,      // оценка навыка по правилу
    Onnx,           // нейросеть ONNX
    LinearFormula   // резервная формула D = Accuracy * 1.5
}

// Настройки модуля DDA в виде ассета (Create -> AimTrainer -> DDA Settings).
// Можно подменить ассет или поменять тип сервиса прямо в Play Mode: DDARunner пересоберёт контроллер.
[CreateAssetMenu(menuName = "AimTrainer/DDA Settings", fileName = "DDASettings")]
public class DDASettings : ScriptableObject
{
    public PredictionServiceType prediction = PredictionServiceType.Onnx;
    public DDAMode defaultMode = DDAMode.Aggressive;
    [Min(0.01f)] public float transitionSeconds = 1.5f;
    [Min(1f)] public float maxInferenceMs = 10f;

#if USE_ONNX
    public ModelAsset onnxModel;
#endif

    // Ссылка на модель (null, если ONNX не подключён): нужна, чтобы заметить смену модели в Play Mode
    public Object ModelReference
    {
        get
        {
#if USE_ONNX
            return onnxModel;
#else
            return null;
#endif
        }
    }
}
