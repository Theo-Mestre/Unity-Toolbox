using UnityEngine;

public class WidgetSwitcher : MonoBehaviour
{
    [SerializeField] private RectTransform switcher;
    [SerializeField] private int activeIndexOnStart = 0;
    private int activeWidgetIndex = 0;
    private int widgetNum = 0;

    private void OnEnable()
    {
        if (switcher == null)
            switcher = GetComponent<RectTransform>();

        activeWidgetIndex = activeIndexOnStart;
        widgetNum = transform.childCount;

        Invoke("ProcessWidgetIndexChanges", 0.1f);
    }
    public int GetWidgetNum() { return widgetNum; }
    public int GetActiveWidgetIndex() { return activeWidgetIndex; }
    public void SetActiveWidgetIndex(int index)
    {
        if (activeWidgetIndex == index) return;

        if (activeWidgetIndex < 0 || activeWidgetIndex > widgetNum)
        {
            Debug.LogWarning($"WidgetSwitcher: Trying to activate an invalid index (id:{index})");
            return;
        }

        activeWidgetIndex = index;

        ProcessWidgetIndexChanges();
    }
    private void ProcessWidgetIndexChanges()
    {
        if (widgetNum != switcher.childCount)
            widgetNum = switcher.childCount;

        int i = 0;
        foreach (Transform child in switcher)
        {
            child.gameObject.SetActive(i == activeWidgetIndex);
            i++;
        }
    }
}
