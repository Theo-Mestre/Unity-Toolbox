using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

#region Enums
public enum InputContext
{
    None = -1,
    Global,
    UI,
    FPS,
    Orbital,
}

public enum CursorLockIntent
{
    None,
    Locked,
    Unlocked,
}
#endregion

public class InputManager : MonoBehaviour
{
    #region Singleton Implementation
    // InputManager instance reference
    private static InputManager instance;

    // InputManager instance property
    public static InputManager Instance
    {
        get
        {
            if (instance == null)
            {
#if UNITY_2023_1_OR_NEWER
                instance = FindFirstObjectByType<InputManager>();
#else
                instance = FindObjectOfType<InputManager>();
#endif
                if (instance == null)
                {
                    var obj = new GameObject(nameof(InputManager));
                    instance = obj.AddComponent<InputManager>();
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
            Debug.LogWarning("Destroying duplicate instance of InputManager.");
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
    #endregion

    #region Fields
    // Inputs
    private PlayerInputConfig input;
    private readonly HashSet<InputContext> activeContexts = new();

    // Cursor Policy
    [SerializeField]
    private readonly Dictionary<InputContext, CursorLockIntent> contextCursorPolicy = new()
    {
        { InputContext.Global,   CursorLockIntent.None },
        { InputContext.FPS,      CursorLockIntent.Locked },
        { InputContext.Orbital,  CursorLockIntent.Unlocked },
        { InputContext.UI,       CursorLockIntent.Unlocked }
    };
    private readonly HashSet<object> cursorUnlockRequests = new();
    #endregion

    protected virtual void Init()
    {
        input = new PlayerInputConfig();

        EnableContext(InputContext.Global);
    }
    protected virtual void Shutdown()
    {
        DisableContext(InputContext.Global);
        activeContexts.Clear();
        cursorUnlockRequests.Clear();
    }

    #region Registration
    public static void Register(IInputConsumer consumer)
    {
        consumer.BindInputs(Instance.input);
    }
    #endregion

    #region Context Control
    public static void EnableContext(InputContext context)
    {
        bool isActivated = Instance.activeContexts.Add(context);
        if (!isActivated) return;

        Instance.GetActionMap(context)?.Enable();
        Instance.RecomputeCursorState();
    }

    public static void DisableContext(InputContext context)
    {
        bool isRemoved = Instance.activeContexts.Remove(context);
        if (!isRemoved) return;

        Instance.GetActionMap(context)?.Disable();
        Instance.RecomputeCursorState();
    }
    #endregion

    #region Cursor Control
    public static void RequestCursorUnlock(object requester)
    {
        if (!Instance.cursorUnlockRequests.Add(requester)) return;

        Instance.RecomputeCursorState();
    }
    public static void ReleaseCursorUnlock(object requester)
    {
        if (!Instance.cursorUnlockRequests.Remove(requester)) return;

        Instance.RecomputeCursorState();
    }
    #endregion

    #region Internal
    private InputActionMap GetActionMap(InputContext context)
    {
        return context switch
        {
            InputContext.Global => input.Global,
            InputContext.FPS => input.FPS,
            InputContext.Orbital => input.Orbital,
            InputContext.UI => input.UI,
            _ => null
        };
    }

    private void RecomputeCursorState()
    {
        // Unlock cursor if there is no active request
        if (cursorUnlockRequests.Count > 0)
        {
            ApplyCursor(CursorLockIntent.Unlocked);
            return;
        }

        // Resolve from active contexts
        foreach (var context in activeContexts)
        {
            if (!contextCursorPolicy.TryGetValue(context, out var intent)) continue;

            if (intent != CursorLockIntent.None)
            {
                ApplyCursor(intent);
                return;
            }
        }

        ApplyCursor(CursorLockIntent.Unlocked);
    }

    private void ApplyCursor(CursorLockIntent intent)
    {
        switch (intent)
        {
            case CursorLockIntent.Locked:
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                break;

            case CursorLockIntent.Unlocked:
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                break;
        }

        EventDispatcher.Broadcast(Events.OnCursorStateChanged, new("CursorLockIntent", intent));
    }

    #endregion
}