public readonly struct SelectionData
{
    public SelectionData(string title, string description, string terrain)
    {
        Title = title;
        Description = description;
        Terrain = terrain;
    }

    public string Title { get; }

    public string Description { get; }

    public string Terrain { get; }
}

public interface ISelectable
{
    string DisplayName { get; }
    string DisplayDescription { get; }
    SelectionData GetSelectionData();
}
