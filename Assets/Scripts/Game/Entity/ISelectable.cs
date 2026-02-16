public interface ISelectable
{
    string DisplayName { get; }
    string DisplayDescription { get; }
    string GetSelectionDetails();
}
