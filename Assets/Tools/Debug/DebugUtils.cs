using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace System.Runtime.CompilerServices
{
    // This attribute allow to capture the expression of the argument without user intervention.
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    internal sealed class CallerArgumentExpressionAttribute : Attribute
    {
        public CallerArgumentExpressionAttribute(string parameterName)
        {
            ParameterName = parameterName;
        }

        public string ParameterName { get; }
    }
}

namespace Utilities
{
    public struct ErrorContext
    {
        public string CallerName;
        public string CallerClassName;
        public string FileName;
        public int LineNumber;

        public static ErrorContext Default()
        {
            return new ErrorContext
            {
                CallerName = "UnknownMethod",
                CallerClassName = "UnknownClass",
                FileName = "UnknownFile",
                LineNumber = -1
            };
        }
    }

    public class Toolbox
    {
        static public ErrorContext CreateErrorContext(int skipFrames)
        {
            var stackFrame = new StackFrame(skipFrames, true);
            var method = stackFrame.GetMethod();

            if (stackFrame == null || method == null)
                return ErrorContext.Default();


            string caller = method.Name;
            string callerClass = method.DeclaringType == null ? 
                "UnknownClass" : method.DeclaringType.Name;

            string fileName = stackFrame.GetFileName();
            int lineNumber = stackFrame.GetFileLineNumber();

            // Keep only the relative path starting from "Assets/"
            fileName = fileName.Replace("\\", "/");
            fileName = fileName.Contains("Assets/") ?
                fileName[fileName.IndexOf("Assets/")..] : fileName;

            return new ErrorContext
            {
                CallerName = caller,
                CallerClassName = callerClass,
                FileName = fileName,
                LineNumber = lineNumber
            };
        }
    }

    /// <summary>
    /// An utility class for logging message in a formated pattern with caller context info
    /// </summary>
    public class Log
    {
        #region Stack Frames Skip Management

        // The frame skip is used to get the correct caller information for the log messages.
        // The default value is 2 to skip the formatting method and the Log method (e.g. Assert, IsNull, etc.).
        // You can use PushSkipFrame to add additional skip frames for specific log calls (e.g. when logging from a helper method).
        private static readonly int skipFrames = 2;
        private static int tempSkipFrames = 0;
        /// <summary>
        /// Return the total skip frames for the next log call and reset the temporary skip frames.
        /// </summary>
        private static int SkipFrames
        {
            get
            {
                int totalSkipFrames = skipFrames + tempSkipFrames;
                tempSkipFrames = 0;
                return totalSkipFrames;
            }
        }

        /// <summary>
        /// Add additional skip frames for the next log call.
        /// The skip frames will be reset after the next log call.
        /// </summary>
        static public void PushSkipFrame(int count = 1)
        {
            tempSkipFrames += count;
        }
        #endregion

        /// <summary>
        /// Check the given condition and log an error message if the condition is false. In the editor, it will also pause the editor for debugging.
        /// </summary>
        /// <param name="condition">the condition to check</param>
        /// <param name="message">the message to log</param>
        static public void Assert(bool condition, string message = "Assertion failed!")
        {
            if (!condition) return;
            PushSkipFrame();
            Error(message);
#if UNITY_EDITOR
            EditorApplication.isPaused = true;
#endif
        }

        /// <summary>
        /// Check if the given UnityEngine.Object reference is null and log an error if it is.
        /// </summary>
        /// <param name="obj">the UnityEngine.Object reference that need to be checked.</param>
        /// <param name="objName">the name of the object automatically extracted by the CallerArgumentExpression attribute.</param>
        /// <returns>
        /// true if the object is null; otherwise, false.
        /// </returns>
        static public bool IsNull(Object obj, [CallerArgumentExpression("obj")] string objName = null)
        {
            if (obj != null) return false;

            PushSkipFrame();
            Error($"{objName}: reference is null!");

            return true;
        }

        /// <summary>
        /// Check if the given CLR object reference is null and log an error if it is.
        /// </summary>
        /// <param name="obj">the CLR object reference that need to be checked.</param>
        /// <param name="objName">the name of the object automatically extracted by the CallerArgumentExpression attribute.</param>
        /// <returns>true if the object is null; otherwise, false.</returns>
        static public bool IsNullCLR(object obj, [CallerArgumentExpression("obj")] string objName = null)
        {
            if (obj != null) return false;

            PushSkipFrame();
            Error($"{objName}: reference is null!");

            return true;
        }

        /// <summary>
        /// Log an informational message with caller context information (method name, class name).
        /// </summary>
        /// <param name="message">the message to log</param>
        /// <param name="context">the context object to associate with the log entry</param>
        static public void Info(string message, Object context = null)
        {
            var errorContext = Toolbox.CreateErrorContext(SkipFrames);
            Debug.Log(FormatLogMessage(message, errorContext), context);
        }

        /// <summary>
        /// Log a warning message with caller context information (method name, class name).
        /// </summary>
        /// <param name="message">the message to log</param>
        /// <param name="context">the context object to associate with the log entry</param>
        static public void Warn(string message, Object context = null)
        {
            var errorContext = Toolbox.CreateErrorContext(SkipFrames);
            Debug.LogWarning(FormatLogMessage(message, errorContext), context);
        }

        /// <summary>
        /// Log an error message with caller context information (method name, class name).
        /// </summary>
        /// <param name="message">the message to log</param>
        /// <param name="context">the context object to associate with the log entry</param>
        static public void Error(string message, Object context = null)
        {
            var errorContext = Toolbox.CreateErrorContext(SkipFrames);
            Debug.LogError(FormatLogMessage(message, errorContext), context);
        }

        #region Formatting Helpers
        static private string FormatLogMessage(string message, ErrorContext context)
        {
            string prefix = FormatLogPrefix(context);
            string suffix = FormatLogSuffix(context);
            return $"{prefix}: {message}{suffix}";
        }
        static private string FormatLogPrefix(ErrorContext context)
        {
            return $"[{context.CallerClassName}.{context.CallerName}]";
        }
        static private string FormatLogSuffix(ErrorContext context)
        {
            return $"\nRef:{context.FileName}({context.LineNumber})";
        }
        #endregion
    }
}