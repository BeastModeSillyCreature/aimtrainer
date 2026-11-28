using System;
using System.Collections.Generic;
using UnityEngine;

// Простой реестр сервисов: компоненты берут нужные интерфейсы отсюда, а не создают конкретные классы сами.
// Заполняется один раз в GameBootstrap. В тестах сюда можно положить заглушки (Register).
public static class ServiceLocator
{
    private static readonly Dictionary<Type, object> Services = new Dictionary<Type, object>();

    public static void Register<T>(T service) where T : class
    {
        Services[typeof(T)] = service;
    }

    public static T Get<T>() where T : class
    {
        if (Services.TryGetValue(typeof(T), out object service)) return (T)service;
        throw new InvalidOperationException($"Service {typeof(T).Name} is not registered. Add GameBootstrap to the scene.");
    }

    public static void Clear()
    {
        Services.Clear();
    }

    // На случай отключённой перезагрузки домена при входе в Play Mode: статические данные не должны переживать запуск
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        Services.Clear();
    }
}
