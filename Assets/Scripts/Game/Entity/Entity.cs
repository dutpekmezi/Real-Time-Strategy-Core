using Game.Entity;
using UnityEngine;

public class Entity : MonoBehaviour, ISelectable
{
    [SerializeField] protected MapEntityData entityData;

    public string DisplayName => string.IsNullOrWhiteSpace(entityData.Name) ? gameObject.name : entityData.Name;

    public string DisplayDescription => string.IsNullOrWhiteSpace(entityData.Description)
        ? "No Descrpition."
        : entityData.Description;

    public virtual SelectionData GetSelectionData()
    {
        return new SelectionData(DisplayName, DisplayDescription, "-");
    }
}
