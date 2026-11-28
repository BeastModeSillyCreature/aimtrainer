using System;
using UnityEngine;

// Фабрика стратегий (ЛР №5, этап 4): собирает нужные реализации по настройкам DDASettings.
// Благодаря ей DDARunner не знает про конкретные классы.
public static class StrategyFactory
{
    public static IPredictionService CreatePredictionService(DDASettings settings)
    {
        switch (settings.prediction)
        {
            case PredictionServiceType.LinearFormula:
                Debug.Log("[DDA] Prediction service: linear formula");
                return new LinearPredictionService();

            case PredictionServiceType.Onnx:
                return CreateOnnxOrFallback(settings);

            default:
                Debug.Log("[DDA] Prediction service: heuristic");
                return new HeuristicPredictionService();
        }
    }

    public static IDecisionStrategy CreateDecisionStrategy()
    {
        return new FsmDecisionStrategy();
    }

    public static IDecisionStrategy CreateFallbackStrategy()
    {
        return new LinearFallbackDecisionStrategy();
    }

    public static AimDDAController CreateController(DDASettings settings, IDifficultyValidator validator)
    {
        AimDDAController controller = new AimDDAController(
            CreatePredictionService(settings),
            CreateDecisionStrategy(),
            CreateFallbackStrategy(),
            validator);
        controller.MaxInferenceMs = settings.maxInferenceMs;
        return controller;
    }

    private static IPredictionService CreateOnnxOrFallback(DDASettings settings)
    {
#if USE_ONNX
        if (settings.onnxModel != null)
        {
            try
            {
                IPredictionService onnx = new OnnxPredictionService(settings.onnxModel);
                Debug.Log("[DDA] Prediction service: ONNX neural network");
                return onnx;
            }
            catch (Exception e)
            {
                // E-2.1: ошибка загрузки .onnx -> резервная формула D = Accuracy * 1.5
                Debug.LogWarning($"[DDA] Cannot load ONNX model ({e.Message}). Using linear formula.");
                return new LinearPredictionService();
            }
        }

        Debug.LogWarning("[DDA] ONNX model is not assigned in DDASettings. Using linear formula.");
        return new LinearPredictionService();
#else
        Debug.LogWarning("[DDA] USE_ONNX is not defined (Inference Engine is not set up). Using linear formula.");
        return new LinearPredictionService();
#endif
    }
}
