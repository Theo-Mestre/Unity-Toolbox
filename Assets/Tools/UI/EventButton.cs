using UnityEngine;
using UnityEngine.UI;

public class EventButton : MonoBehaviour
{
    [SerializeField, StringNotNullOrEmpty] private string eventName = "";
    [SerializeField, NotNull] private Button button = null;

    public void Awake()
    {
        ReferenceValidator.Validate(this);
    }

    public void Start()
    {
        // disable the component if the button or string is invalid
        if (button == null || string.IsNullOrEmpty(eventName))
        {
            enabled = false;
            return;
        }

        button.onClick.AddListener(OnButtonClicked);
    }

    public void OnDestroy()
    {
        if (button == null) return;

        button.onClick.RemoveListener(OnButtonClicked);
    }

    public void OnButtonClicked()
    {
        EventDispatcher.Broadcast(eventName);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }
    }
}
#endif