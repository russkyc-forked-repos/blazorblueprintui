namespace BlazorBlueprint.Components;

/// <summary>
/// Defines the layout orientation for a <see cref="BbSortable{T}"/> component.
/// </summary>
public enum SortableLayout
{
    /// <summary>Items are stacked vertically in a single column.</summary>
    List,

    /// <summary>Items are arranged in a CSS grid (column count controlled by <c>Class</c>).</summary>
    Grid
}
