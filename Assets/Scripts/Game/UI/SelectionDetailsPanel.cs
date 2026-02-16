using DG.Tweening;
using TMPro;
using UnityEngine;

public class SelectionDetailsPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI detailsText;
    [SerializeField] private TextMeshProUGUI typeText;
    [SerializeField] private string emptyStateText = "Bir şehir seçin";
    [SerializeField] private float tweenDuration = 0.25f;

    private RectTransform _panelRect;
    private Tween _moveTween;
    private Vector2 _shownPosition;
    private Vector2 _hiddenPosition;
    private bool _isOpen;

    private void Awake()
    {
        _panelRect = transform as RectTransform;

        if (_panelRect != null)
        {
            _shownPosition = _panelRect.anchoredPosition;
            _hiddenPosition = new Vector2(-_panelRect.rect.width, _shownPosition.y);
            _panelRect.anchoredPosition = _hiddenPosition;
        }

        ShowNoSelection();
    }

    private void OnDestroy()
    {
        _moveTween?.Kill();
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

        SetOpenState(true);
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

        SetOpenState(false);
    }

    private void SetOpenState(bool open)
    {
        if (_panelRect == null || _isOpen == open)
        {
            return;
        }

        _isOpen = open;
        _moveTween?.Kill();
        Vector2 targetPosition = open ? _shownPosition : _hiddenPosition;
        _moveTween = _panelRect.DOAnchorPos(targetPosition, tweenDuration).SetEase(Ease.OutCubic);
    }
}
