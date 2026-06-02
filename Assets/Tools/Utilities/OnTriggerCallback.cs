using UnityEngine;
using UnityEngine.Events;
using Utilities;

public class OnTriggerCallback
    : MonoBehaviour
{
    #region Fields
    #region Editor Code
#if UNITY_EDITOR
    [Header("Debug")]
    [SerializeField] private bool debugMode = false;
#endif
    #endregion

    [Header("Collision Settings")]
    [SerializeField] private string acceptedColliderTag;

    [Header("Events")]
    [SerializeField] public UnityEvent OnTriggerEnterCallback = null;
    [Space]
    [SerializeField] public UnityEvent OnTriggerExitCallback = null;
    #endregion

    #region properties
    private bool IsTagValid => !string.IsNullOrEmpty(acceptedColliderTag);
    #endregion

    #region Methods
    private void OnTriggerEnter(Collider other)
    {
        if (!IsTagValid || other.tag != acceptedColliderTag) return;

        #region Editor Code
#if UNITY_EDITOR
        if (debugMode)
        {
            Log.Info($"{other.gameObject.name} entered trigger {name}", this);
        }
#endif
        #endregion

        OnTriggerEnterCallback?.Invoke();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsTagValid || other.tag != acceptedColliderTag) return;

        #region Editor Code
#if UNITY_EDITOR
        if (debugMode)
        {
            Log.Info($"{other.gameObject.name} exited trigger {name}", this);
        }
#endif
        #endregion

        OnTriggerExitCallback?.Invoke();
    }
    #endregion
}