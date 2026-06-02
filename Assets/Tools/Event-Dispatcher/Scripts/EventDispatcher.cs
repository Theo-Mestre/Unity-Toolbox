using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventDispatcher : MonoBehaviour
{
    // The dictionary of bindings
    private Dictionary<string, Action<ParamList>> bindingsDictionary;

#if UNITY_EDITOR
    // The event dispatch tracker will track all event dispatches in a log file (editor only)
    EventDispatchTracker tracker = new EventDispatchTracker();
#endif

    #region Singleton Implementation
    // EventDispatcher instance reference
    private static EventDispatcher instance;
    // EventDispatcher instance property
    public static EventDispatcher Instance
    {
        get
        {
            if (instance == null)
            {
#if UNITY_2023_1_OR_NEWER
                instance = FindFirstObjectByType<EventDispatcher>();
#else
                instance = FindObjectOfType<EventDispatcher>();
#endif
                if (instance == null)
                {
                    var obj = new GameObject(nameof(EventDispatcher));
                    instance = obj.AddComponent<EventDispatcher>();
                }

                DontDestroyOnLoad(instance.gameObject);
                instance.Init();
            }

            return instance;
        }
    }

    public static bool IsValid()
    {
        return instance != null;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogWarning("Destroying duplicate instance of EventDispatcher.");
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        Init();
    }
    private void OnDestroy()
    {
        if (instance == this)
        {
            Shutdown();
            instance = null;
        }
    }

    protected virtual void Init()
    {
        if (bindingsDictionary == null)
        {
            bindingsDictionary = new Dictionary<string, Action<ParamList>>();
        }
    }
    protected virtual void Shutdown()
    {
        ClearAllBindings();

#if UNITY_EDITOR
        tracker.LogEventDispatchInfos();
        tracker.ClearEventDispatchInfos();
        tracker = null;
#endif
    }
    #endregion

    #region Binding Functions
    /// <summary>
    /// Binds a listener to an event. If the event does not exist, it will be created.
    /// </summary>
    public static void Bind(string eventName, Action<ParamList> listener)
    {
        if (Instance.bindingsDictionary.TryGetValue(eventName, out Action<ParamList> thisEvent))
        {
            thisEvent += listener;
            Instance.bindingsDictionary[eventName] = thisEvent;
        }
        else
        {
            thisEvent += listener;
            Instance.bindingsDictionary.Add(eventName, thisEvent);
        }
    }

    /// <summary>
    /// Unbinds a binding from an event.
    /// </summary>
    public static void Unbind(string eventName, Action<ParamList> binding)
    {
        if (instance == null) return;

        Action<ParamList> thisEvent;
        if (Instance.bindingsDictionary.TryGetValue(eventName, out thisEvent))
        {
            thisEvent -= binding;
            Instance.bindingsDictionary[eventName] = thisEvent;
        }
    }

    /// <summary>
    /// Broadcasts the event and trigger all bindings associated to the event name.
    /// </summary>
    public static void Broadcast(string eventName, ParamList parameters = null)
    {
#if UNITY_EDITOR
        Instance.tracker.AddEventDispatchInfo(CreateEventDispatchInfo(eventName, parameters));
#endif

        if (!Instance.bindingsDictionary.TryGetValue(eventName, out Action<ParamList> _event)) return;

        if (_event == null)
        {
            Debug.LogWarning("The event " + eventName + " does not have any listeners.");
            return;
        }

        _event.Invoke(parameters);
    }

    /// <summary>
    /// Broadcasts the event after a delay in seconds and trigger all bindings.
    /// </summary>
    public static void BroadcastDelayed(string eventName, ParamList param, float delayInSeconds)
    {
        Instance.StartCoroutine(Instance.DelayedBroadcast(eventName, param, delayInSeconds));
    }

    private IEnumerator DelayedBroadcast(string eventName, ParamList param, float delayInSeconds)
    {
        yield return new WaitForSeconds(delayInSeconds);
        Broadcast(eventName, param);
    }

    /// <summary>
    /// Clears all event bindings from the internal event dictionary.
    /// </summary>
    /// <remarks>This method removes all registered event listeners, effectively resetting the event system. 
    /// Use this method with caution, as it will prevent any previously registered events from being
    /// triggered.</remarks>
    public static void ClearAllBindings()
    {
        if (instance == null) return;
        instance.bindingsDictionary.Clear();

    }
    #endregion Listener Functions

    #region Getter Functions
    public static int GetBindingsCount(string eventName)
    {
        if (Instance.bindingsDictionary.TryGetValue(eventName, out Action<ParamList> thisEvent) && thisEvent != null)
        {
            return thisEvent.GetInvocationList().Length;
        }
        return 0;
    }
    public static int GetNumBound()
    {
        return Instance.bindingsDictionary.Count;
    }
    public static bool IsBound(string eventName)
    {
        return Instance.bindingsDictionary.ContainsKey(eventName);
    }
    public static bool HasBinding(string eventName, Action<ParamList> listener)
    {
        if (!Instance.bindingsDictionary.TryGetValue(eventName, out Action<ParamList> thisEvent)) return false;

        Delegate[] invocationList = thisEvent.GetInvocationList();

        foreach (Delegate action in invocationList)
        {
            if (action.Equals(listener))
            {
                return true;
            }
        }

        return false;
    }
    #endregion Getter Functions

    #region DebugTracking
#if UNITY_EDITOR
    static private EventDispatchTracker.EventDispatchInfo CreateEventDispatchInfo(string eventName, ParamList parameters)
    {
        return new EventDispatchTracker.EventDispatchInfo(eventName
            , Time.realtimeSinceStartup
            , (uint)Time.frameCount
            , GetBindingsCount(eventName)
            , parameters);
    }
#endif
    #endregion
}

public class ParamList
    : Dictionary<string, object>
{
    public ParamList() : base() { }
    public ParamList(string key, object obj)
         : base()
    {
        if (string.IsNullOrEmpty(key))
        {
            Debug.LogWarning("Key cannot be null or empty in ParamList constructor.");
            return;
        }
        this[key] = obj;
    }
    public ParamList(Dictionary<string, object> _other)
        : base(_other) { }

    /// <summary>
    /// Gets the value associated with the specified key and check its type. In case of failure, returns defaultValue.
    /// </summary>
    public T Get<T>(string key, T defaultValue = default)
    {
        if (TryGetValue(key, out object value) && value is T typedValue)
        {
            return typedValue;
        }
        Debug.LogWarning($"Key '{key}' not found in ParamList or value is not of type {typeof(T)}.");
        return defaultValue;
    }

    /// <summary>
    /// Adds or updates a key-value pair and returns the ParamList for chaining.
    /// </summary>
    public ParamList Set<T>(string key, T value)
    {
        this[key] = value;
        return this;
    }

    public override string ToString()
    {
        var parts = new List<string>();
        foreach (var kvp in this)
        {
            parts.Add($"{kvp.Key}: {kvp.Value}");
        }
        return $"ParamList {{ {string.Join(", ", parts)} }}";
    }
}