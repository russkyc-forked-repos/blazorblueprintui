namespace BlazorBlueprint.Components;

/// <summary>
/// Scoped service that carries drag state across <see cref="BbDragDrop{TItem}"/>
/// component instances during a single drag operation.
/// <para>
/// Registered automatically by
/// <see cref="ServiceCollectionExtensions.AddBlazorBlueprintComponents"/>.
/// One instance per Blazor circuit (Server) or per application (WASM).
/// </para>
/// </summary>
public sealed class DragDropState
{
    // ── live drag data ────────────────────────────────────────────────────

    /// <summary>The item currently being dragged, or <c>null</c> when idle.</summary>
    public object? Item { get; private set; }

    /// <summary>The group name of the source list, or <c>null</c> if not grouped.</summary>
    public string? Group { get; private set; }

    /// <summary>Whether the source list uses clone mode (<c>Pull="clone"</c>).</summary>
    public bool IsClone { get; private set; }

    /// <summary>The index of the item in the source list at the time dragging started.</summary>
    public int OldIndex { get; private set; }

    /// <summary>Returns <c>true</c> while a drag operation is in progress.</summary>
    public bool IsActive => Item is not null;

    // ── post-drop tracking ────────────────────────────────────────────────

    /// <summary>
    /// Set to <c>true</c> by the target component when a cross-list drop is
    /// accepted.  The source component's <c>dragend</c> handler checks this
    /// flag to decide whether to fire <see cref="BbDragDrop{TItem}.OnRemove"/>.
    /// </summary>
    public bool DropOccurred { get; private set; }

    /// <summary>The index at which the item was inserted in the target list.</summary>
    public int DropTargetIndex { get; private set; }

    // ── internal API (called only by BbDragDrop<TItem>) ──────────────────

    internal void Begin(object item, string? group, bool isClone, int oldIndex)
    {
        Item            = item;
        Group           = group;
        IsClone         = isClone;
        OldIndex        = oldIndex;
        DropOccurred    = false;
        DropTargetIndex = -1;
    }

    internal void RecordDrop(int targetIndex)
    {
        DropOccurred    = true;
        DropTargetIndex = targetIndex;
    }

    internal void Clear()
    {
        Item            = null;
        Group           = null;
        IsClone         = false;
        OldIndex        = -1;
        DropOccurred    = false;
        DropTargetIndex = -1;
    }
}
