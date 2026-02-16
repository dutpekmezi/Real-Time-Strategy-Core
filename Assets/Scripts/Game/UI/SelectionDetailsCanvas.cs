using UnityEngine;
using UnityEngine.UI;

public class SelectionDetailsCanvas : MonoBehaviour
{
    [SerializeField] private Text detailsText;
    [SerializeField] private string emptyStateText = "Bir şehir seçin";

    private void Awake()
    {
        ShowNoSelection();
    }

    public void ShowSelection(ISelectable selectable)
    {
        if (detailsText == null)
        {
            return;
        }

        detailsText.text = selectable != null ? selectable.GetSelectionDetails() : emptyStateText;
    }

    public void ShowNoSelection()
    {
        if (detailsText == null)
        {
            return;
        }

        detailsText.text = emptyStateText;
    }
}
