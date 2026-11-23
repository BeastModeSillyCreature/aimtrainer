// Сервис предсказания на нейросети ONNX (ЛР №3: OnnxPredictionService).
// Использует Unity Inference Engine (пакет com.unity.ai.inference): он читает .onnx напрямую
// и не требует сторонних DLL. Компилируется только если добавлен символ USE_ONNX.
#if USE_ONNX
using System;
using Unity.InferenceEngine;

public sealed class OnnxPredictionService : IPredictionService, IDisposable
{
    private readonly Worker _worker;
    private readonly float[] _logits = new float[4]; // буфер результата переиспользуется

    public OnnxPredictionService(ModelAsset modelAsset)
    {
        Model model = ModelLoader.Load(modelAsset);
        _worker = new Worker(model, BackendType.CPU); // для крошечной модели CPU быстрее и надёжнее GPU

        // Прогрев: первый запуск в редакторе медленный (компиляция), поэтому делаем его заранее
        Predict(new float[] { 0.5f, 900f });
    }

    // features: [точность 0..1, время до попадания в мс] -> 4 логита (Easy, Medium, Hard, AdaptiveExtreme)
    public float[] Predict(float[] features)
    {
        using (Tensor<float> input = new Tensor<float>(new TensorShape(1, features.Length), features))
        {
            _worker.Schedule(input);

            Tensor<float> output = _worker.PeekOutput() as Tensor<float>;
            float[] result = output.DownloadToArray();

            if (result == null || result.Length != _logits.Length)
                throw new InvalidOperationException("Unexpected model output size");

            Array.Copy(result, _logits, _logits.Length);
            return _logits;
        }
    }

    public void Dispose()
    {
        _worker?.Dispose();
    }
}
#endif
