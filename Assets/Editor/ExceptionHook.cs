#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;

[InitializeOnLoad]
public static class ExceptionHook
{
    static ExceptionHook()
    {
        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            Debug.LogError("[ExceptionHook] UnhandledException: " + e.ExceptionObject);
        };

        Application.logMessageReceived += (condition, stackTrace, type) =>
        {
            if (type == LogType.Exception || type == LogType.Error)
            {
                Debug.Log($"[ExceptionHook] Caught log type={type}\nCondition={condition}\nStack={stackTrace}");
            }
        };
        Debug.Log("[ExceptionHook] Installed");
    }
}
#endif