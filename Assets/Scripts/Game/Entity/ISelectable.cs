public readonly struct SelectionData
{
    public SelectionData(string title, string description, string type)
    {
        Title = title;
        Description = description;
        Type = type;
    }

    public string Title { get; }

    public string Description { get; }

    public string Type { get; }
}

public interface ISelectable
{
    string DisplayName { get; }
    string DisplayDescription { get; }
    SelectionData GetSelectionData();
}
