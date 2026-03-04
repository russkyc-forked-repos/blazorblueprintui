namespace BlazorBlueprint.Components;

/// <summary>
/// Contains information about an item that has been dropped into a zone or sortable list.
/// </summary>
/// <typeparam name="T">The type of item being dragged and dropped.</typeparam>
public sealed class DropItemInfo<T>
{
    /// <summary>
    /// Initializes a new instance of <see cref="DropItemInfo{T}"/>.
    /// </summary>
    /// <param name="item">The item that was dropped.</param>
    /// <param name="sourceZone">The identifier of the zone the item was dragged from.</param>
    /// <param name="targetZone">The identifier of the zone the item was dropped into.</param>
    /// <param name="targetIndex">The index in the target zone where the item was dropped, or -1 if not applicable.</param>
    public DropItemInfo(T? item, string sourceZone, string targetZone, int targetIndex)
    {
        Item = item;
        SourceZone = sourceZone;
        TargetZone = targetZone;
        TargetIndex = targetIndex;
    }

    /// <summary>Gets the item that was dropped.</summary>
    public T? Item { get; }

    /// <summary>Gets the identifier of the zone the item was dragged from.</summary>
    public string SourceZone { get; }

    /// <summary>Gets the identifier of the zone the item was dropped into.</summary>
    public string TargetZone { get; }

    /// <summary>
    /// Gets the index position in the target zone where the item was dropped.
    /// Returns -1 when the drop zone does not track position (e.g. <see cref="BbDropZone{T}"/>).
    /// </summary>
    public int TargetIndex { get; }
}
