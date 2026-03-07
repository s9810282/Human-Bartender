using UnityEngine;

public static class Logger
{
    [System.Diagnostics.Conditional("ENABLE_LOGS")]
    public static void Log(object message)
    {
        Debug.Log(message);
    }

    [System.Diagnostics.Conditional("ENABLE_LOGS")]
    public static void Log(object message, Object context)
    {
        Debug.Log(message, context);
    }

    [System.Diagnostics.Conditional("ENABLE_LOGS")]
    public static void LogWarning(object message)
    {
        Debug.LogWarning(message);
    }

    [System.Diagnostics.Conditional("ENABLE_LOGS")]
    public static void LogWarning(object message, Object context)
    {
        Debug.LogWarning(message, context);
    }


    [System.Diagnostics.Conditional("ENABLE_LOGS")]
    public static void LogError(object message)
    {
        Debug.LogError(message);
    }

    [System.Diagnostics.Conditional("ENABLE_LOGS")]
    public static void LogError(object message, Object context)
    {
        Debug.LogError(message, context);
    }
}