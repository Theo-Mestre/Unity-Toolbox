using System.Reflection;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using Utilities;

public static class ConsoleRedirector
{
    private static bool s_suppressOnOpen = false;

    [OnOpenAsset(0)]
    public static bool OnOpenAsset(int instanceID, int line)
    {
        // Prevent recursive calls
        if (s_suppressOnOpen)
        {
            s_suppressOnOpen = false;
            return false;
        }

        string activeText = GetConsoleActiveText();
        if (string.IsNullOrEmpty(activeText))
            return false;

        if (!activeText.Contains("Assets/"))
            return false;

        string assetPath = "";

        int start = activeText.IndexOf("Ref:");
        if (start < 0) return false;

        int end = activeText.IndexOf(".cs", start);
        if (end < 0) return false;

        start += "Ref:".Length;
        assetPath = activeText.Substring(start, end - start + ".cs".Length);

        int parsedLine = 0;
        int startIndex = activeText.IndexOf('(', end);
        int endIndex = activeText.IndexOf(')', end);

        if (startIndex >= 0 && endIndex >= 0 && endIndex > startIndex)
        {
            string lineStr = activeText.Substring(startIndex + 1, endIndex - startIndex - 1);
            int.TryParse(lineStr, out parsedLine);
        }

        // Get MonoScript asset at path
        var mono = AssetDatabase.LoadAssetAtPath<MonoScript>(assetPath);
        if (mono != null)
        {
            s_suppressOnOpen = true;
            AssetDatabase.OpenAsset(mono, parsedLine);
            return true;
        }

        // Try to open directly from disk path of the asset can't be found
        string fullDiskPath = System.IO.Path.Combine(Application.dataPath.Substring(0, Application.dataPath.Length - "Assets".Length), assetPath);
        if (System.IO.File.Exists(fullDiskPath))
        {
            UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(fullDiskPath, parsedLine);
            return true;
        }

        return false;
    }

    static string GetConsoleActiveText()
    {
        // ConsoleWindow type lives in the editor assembly
        var consoleWindowType = typeof(EditorWindow).Assembly.GetType("UnityEditor.ConsoleWindow");
        if (consoleWindowType == null)
            return null;

        // ms_ConsoleWindow is the static instance
        var fieldInfo = consoleWindowType.GetField("ms_ConsoleWindow", BindingFlags.Static | BindingFlags.NonPublic);
        var consoleInstance = fieldInfo?.GetValue(null);
        if (consoleInstance == null)
            return null;

        // m_ActiveText holds the selected log's full text (private field)
        var textField = consoleWindowType.GetField("m_ActiveText", BindingFlags.Instance | BindingFlags.NonPublic);
        var activeText = textField?.GetValue(consoleInstance) as string;
        return activeText;
    }
}
