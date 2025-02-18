using System;
using System.Diagnostics;
using Mk.Routines;


public static class Asr {
#line hidden
    [Conditional (FLAGS.UNITY_EDITOR)]
    public static void IsNotNull<T> ([System.Diagnostics.CodeAnalysis.DoesNotReturnIf (parameterValue: false)] T obj, string msg = null, UnityEngine.Object context = null) where T : class {
        if (obj != null) {
            return;
        }

        Fail (msg, context);
    }

    [Conditional (FLAGS.UNITY_EDITOR)]
    public static void IsNotNullOrEmpty (string s, string msg = null, UnityEngine.Object context = null) {
        if (!string.IsNullOrEmpty (s)) {
            return;
        }

        Fail (msg, context);
    }

    [Conditional (FLAGS.UNITY_EDITOR)]
    public static void IsTrue ([System.Diagnostics.CodeAnalysis.DoesNotReturnIf (parameterValue: false)] bool expr, string msg = null, UnityEngine.Object context = default) {
        if (expr) {
            return;
        }

        Fail (msg, context);
    }


    [Conditional (FLAGS.UNITY_EDITOR)]
    public static void IsFalse ([System.Diagnostics.CodeAnalysis.DoesNotReturnIf (parameterValue: true)] bool expr, string msg = null, UnityEngine.Object context = default) {
        if (!expr) {
            return;
        }

        Fail (msg, context);
    }

    // ReSharper disable Unity.PerformanceAnalysis
    [Conditional (FLAGS.UNITY_EDITOR)]
    public static void Fail (string userMessage = null, UnityEngine.Object context = default, int skipFrames = 2) {
        if (string.IsNullOrEmpty (userMessage)) {
            userMessage = $"{CallerSourceLine (skipFrames)}";
        }
        else {
            userMessage = $"{userMessage}:\n {CallerSourceLine (skipFrames)}";
        }

        if (context) {
            UnityEngine.Debug.LogError (userMessage, context);
        }

        throw new Err (userMessage);
    }

    internal class Err : Exception {
        public Err (string msg) : base (msg) { }
    }
#line default


    public static string CallerSourceLine (int skipFrames = 0) {
#if UNITY_EDITOR
        var trace = new StackTrace (1 + skipFrames, true);
        var fr = trace.GetFrame (0);
        var line = fr.GetFileLineNumber () - 1;
        if (line < 0)
            return string.Empty;

        var fileName = fr.GetFileName ();
        if (!System.IO.File.Exists (fileName))
            return string.Empty;

        var lines = System.IO.File.ReadAllLines (fileName);
        return lines[line].Trim ();
#else
            return string.Empty;
#endif
    }
}