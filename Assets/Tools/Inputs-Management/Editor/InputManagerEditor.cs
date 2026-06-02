using System;
using System.Collections.Generic;
using UnityEditor;

[CustomEditor(typeof(InputManager))]
public class InputManagerEditor : Editor
{
    private InputManager manager;

    private void OnEnable()
    {
        manager = (InputManager)target;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Cursor Policy", EditorStyles.boldLabel);

        DrawCursorPolicyEditor();
    }

    private void DrawCursorPolicyEditor()
    {
        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        var field = typeof(InputManager).GetField("contextCursorPolicy", flags);

        if (field == null)
        {
            EditorGUILayout.HelpBox("contextCursorPolicy field not found.", MessageType.Error);
            return;
        }

        var dictionary = field.GetValue(manager) as Dictionary<InputContext, CursorLockIntent>;
        if (dictionary == null)
        {
            EditorGUILayout.HelpBox("Dictionary is null.", MessageType.Error);
            return;
        }

        EditorGUI.BeginChangeCheck();

        foreach (InputContext context in Enum.GetValues(typeof(InputContext)))
        {
            if (context == InputContext.None)
                continue;

            dictionary.TryGetValue(context, out var current);

            var newValue = (CursorLockIntent)EditorGUILayout.EnumPopup(
                context.ToString(),
                current
            );

            dictionary[context] = newValue;
        }

        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(manager);
        }
    }
}
