using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using Utilities;

[AttributeUsage(AttributeTargets.Field)]
public class NotNullAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Field)]
public class RequireElementsCountAttribute : Attribute
{
    public int RequiredCount { get; private set; }
    public RequireElementsCountAttribute(int requiredCount)
    {
        RequiredCount = requiredCount;
    }
}

[AttributeUsage(AttributeTargets.Field)]
public class StringNotNullOrEmptyAttribute : Attribute { }

public static class ReferenceValidator
{
    public static void Validate(object owner)
    {
        var fields = GetFieldInfos(owner);

        foreach (var field in fields)
        {
            // Skip private fields that aren't [SerializeField]
            bool isSerializeField = field.GetCustomAttribute<SerializeField>() == null;
            if (isSerializeField) continue;

            if (Attribute.IsDefined(field, typeof(NotNullAttribute)))
                ValidateNotNullField(field, owner);

            if (Attribute.IsDefined(field, typeof(RequireElementsCountAttribute)))
                ValidateRequireElementsCountField(field, owner);

            if (Attribute.IsDefined(field, typeof(StringNotNullOrEmptyAttribute)))
                ValidateStringNotNullOrEmptyField(field, owner);
        }
    }

    private static FieldInfo[] GetFieldInfos(object owner)
    {
        if (owner == null) return Array.Empty<FieldInfo>();

        return owner.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
    }

    #region NotNull Validation
    private static void ValidateNotNullField(FieldInfo field, object owner)
    {
        var value = field.GetValue(owner);
        bool isMissing = IsMissing(value, field.FieldType);

        if (!isMissing) return;

        Log.PushSkipFrame(2);
        Log.Error($"Missing reference '{field.Name}'!\n"
            + $"Please assign a valid reference in the inspector."
            , owner as UnityEngine.Object);
    }
    private static bool IsMissing(object value, Type fieldType)
    {
        // CLR null check
        if (value == null)
            return true;

        // UnityEngine.Object null check
        if (IsUnityObjectMissing(value, fieldType))
            return true;

        // Handle arrays and generic Lists
        if (IsContainerMissing(value, fieldType))
            return true;

        return false;
    }
    private static bool IsUnityObjectMissing(object obj, Type fieldType)
    {
        if (!typeof(UnityEngine.Object).IsAssignableFrom(fieldType)) return false;

        return (obj as UnityEngine.Object) == null;
    }
    private static bool IsContainerMissing(object obj, Type fieldType)
    {
        if (!typeof(IEnumerable).IsAssignableFrom(fieldType)) return false;

        if (obj is Array array)
        {
            foreach (var element in array)
            {
                if (IsMissing(element, element?.GetType() ?? typeof(object)))
                    return true;
            }
        }
        else if (fieldType.IsGenericType && typeof(IList).IsAssignableFrom(fieldType))
        {
            if (obj is not IList list) return true;

            foreach (var element in list)
            {
                if (IsMissing(element, element?.GetType() ?? typeof(object)))
                    return true;
            }
        }
        return false;
    }
    #endregion

    #region RequireElementsCount Validation
    private static void ValidateRequireElementsCountField(FieldInfo field, object owner)
    {
        var attribute = field.GetCustomAttribute<RequireElementsCountAttribute>();
        var value = field.GetValue(owner);
        int actualCount = 0;

        if (value is Array array)
        {
            actualCount = array.Length;
        }
        else if (value is IList list)
        {
            actualCount = list.Count;
        }


        if (actualCount <= attribute.RequiredCount) return;

        var context = Utilities.Toolbox.CreateErrorContext(2);
        Debug.LogError($"[{context.CallerClassName}.{context.CallerName}]"
            + $": Field '{field.Name}' requires at least {attribute.RequiredCount} elements, but has {actualCount}.\n"
            + $"Please ensure the collection meets the required element count."
            , owner as UnityEngine.Object);
    }
    #endregion

    #region StringNotNullOrEmpty Validation
    private static void ValidateStringNotNullOrEmptyField(FieldInfo field, object owner)
    {
        var value = field.GetValue(owner) as string;

        if (!string.IsNullOrEmpty(value)) return;

        Log.PushSkipFrame(2);
        Log.Error($"Missing or empty string reference '{field.Name}'!\n"
            + $"Please assign a valid non-empty string in the inspector."
            , owner as UnityEngine.Object);
    }
    #endregion
}