using TMPro;
using UnityEngine;

public class SelectionDetailsPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI detailsText;
    [SerializeField] private TextMeshProUGUI typeText;
    [SerializeField] private string emptyStateText = "Bir şehir seçin";

    private void Awake()
    {
        ShowNoSelection();
    }

    public void ShowSelection(ISelectable selectable)
    {
        if (selectable == null)
        {
            ShowNoSelection();
            return;
        }

        SelectionData selectionData = selectable.GetSelectionData();

        if (titleText != null)
        {
            titleText.text = selectionData.Title;
        }

        if (detailsText != null)
        {
            detailsText.text = selectionData.Description;
        }

        if (typeText != null)
        {
            typeText.text = selectionData.Type;
        }
    }

    public void ShowNoSelection()
    {
        if (titleText != null)
        {
            titleText.text = emptyStateText;
        }

        if (detailsText != null)
        {
            detailsText.text = string.Empty;
        }

        if (typeText != null)
        {
            typeText.text = string.Empty;
        }
    }
}
