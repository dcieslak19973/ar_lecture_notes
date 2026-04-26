/// <summary>
/// Simple service locator. Not a DI container — just a typed registry
/// that keeps Unity-friendly and avoids reflection overhead on Android.
/// </summary>
public static class ServiceLocator
{
    private static readonly System.Collections.Generic.Dictionary<System.Type, object> _services =
        new System.Collections.Generic.Dictionary<System.Type, object>();

    public static void Register<T>(T instance) => _services[typeof(T)] = instance;

    public static T Get<T>()
    {
        if (_services.TryGetValue(typeof(T), out var service))
            return (T)service;
        throw new System.InvalidOperationException(
            $"Service {typeof(T).Name} not registered. Did AppBootstrap run?");
    }

    public static bool IsRegistered<T>() => _services.ContainsKey(typeof(T));

    public static void Clear() => _services.Clear();
}
