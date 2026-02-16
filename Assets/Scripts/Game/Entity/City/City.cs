using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class City : MonoBehaviour, ISelectable
{
    [SerializeField] private string cityName;
    [TextArea]
    [SerializeField] private string cityDescription;

    public string DisplayName => string.IsNullOrWhiteSpace(cityName) ? gameObject.name : cityName;
    public string DisplayDescription => string.IsNullOrWhiteSpace(cityDescription)
        ? "Bu şehir için henüz bir açıklama eklenmedi."
        : cityDescription;

    public string GetSelectionDetails()
    {
        return $"{DisplayName}\n{DisplayDescription}";
    }
}
