using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SelectionDetailsPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI detailsText;
    [SerializeField] private TextMeshProUGUI terrainText;
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
