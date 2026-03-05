using System.Text;
using BlazorBlueprint.Primitives.Utilities;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace BlazorBlueprint.Components;

/// <summary>
/// A drag-and-drop sortable list implemented entirely with the HTML5 Drag and Drop
/// API and Blazor event handlers — no external JS library required.
/// <para>
/// Because Blazor owns the DOM at all times there is no "pre-drag state revert
/// flash" that plagued the previous SortableJS integration.
/// </para>
/// </summary>
/// <remarks>
/// <para>
/// <strong>Basic reorder</strong> — bind <see cref="Items"/> and handle
/// <see cref="OnUpdate"/> to reorder your list.
/// </para>
/// <para>
/// <strong>Cross-list (kanban / move)</strong> — give multiple lists the same
/// <see cref="Group"/> name.  Handle <see cref="OnRemove"/> on the source and
/// <see cref="OnAdd"/> on the target.  <see cref="OnAdd"/> receives the actual item
/// being dropped so you do not need to track it separately.
/// </para>
/// <para>
/// <strong>Clone mode</strong> — set <see cref="Pull"/> to <c>"clone"</c> on the
/// source.  <see cref="OnRemove"/> is never fired; <see cref="OnAdd"/> on the target
/// receives a copy of the item.
/// </para>
/// <para>
/// <strong>Handle</strong> — set <see cref="Handle"/> to a CSS class that appears
/// inside your <see cref="ItemTemplate"/>.  Only dragging that element initiates a
/// drag; clicking elsewhere does nothing.
/// </para>
/// </remarks>
/// <typeparam name="TItem">Type of item in the list. Must be non-null.</typeparam>
public partial class BbDragDrop<TItem> : ComponentBase, IAsyncDisposable where TItem : notnull
{
    private ElementReference _container;
    private IJSObjectReference? _jsModule;
    private bool _jsInitialized;

    // ── local drag state (within this list) ──────────────────────────────
    private TItem?  _localDragItem;
    private int     _localDragIndex   = -1;
    private int     _dropTargetIndex  = -1;
    private bool    _isContainerDragOver;

    [Inject] private IJSRuntime    JSRuntime { get; set; } = default!;
    [Inject] private DragDropState DragState { get; set; } = default!;

    // ─────────────────────────────────────────────────────────────────────────────
    // Parameters
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets the SortableJS group name.
    /// Lists with the same group name can exchange items via drag-and-drop.
    /// Leave <c>null</c> (default) to disable cross-list drag.
    /// </summary>
    [Parameter]
    public string? Group { get; set; }

    /// <summary>
    /// Gets or sets the <c>pull</c> mode for cross-list drags.
    /// <list type="bullet">
    ///   <item><c>null</c> (default) — items are moved to the target list.</item>
    ///   <item><c>"clone"</c> — items are copied; the original stays in place.</item>
    /// </list>
    /// Only meaningful when <see cref="Group"/> is set.
    /// </summary>
    [Parameter]
    public string? Pull { get; set; }

    /// <summary>
    /// Gets or sets whether items from other lists in the same group can be dropped
    /// into this list.  Defaults to <c>true</c>.
    /// </summary>
    [Parameter]
    public bool Put { get; set; } = true;

    /// <summary>
    /// Gets or sets whether items within this list can be reordered.
    /// Defaults to <c>true</c>.
    /// </summary>
    [Parameter]
    public bool Sort { get; set; } = true;

    /// <summary>
    /// Gets or sets a CSS class inside each <see cref="ItemTemplate"/> that acts as
    /// the drag handle.  Only elements with this class will initiate a drag.
    /// Defaults to <c>null</c> (entire item is draggable).
    /// </summary>
    [Parameter]
    public string? Handle { get; set; }

    /// <summary>
    /// Gets or sets a predicate that returns <c>true</c> for items that should
    /// not be draggable.  Disabled items are rendered with <c>draggable="false"</c>
    /// and cannot be picked up.
    /// </summary>
    [Parameter]
    public Func<TItem, bool>? ItemDisabled { get; set; }

    /// <summary>
    /// Gets or sets the list of items to render.
    /// The list is never mutated by the component — update it inside
    /// <see cref="OnUpdate"/>, <see cref="OnRemove"/>, and <see cref="OnAdd"/>.
    /// </summary>
    [Parameter]
    public IList<TItem> Items { get; set; } = new List<TItem>();

    /// <summary>
    /// Gets or sets the template used to render each item.
    /// </summary>
    [Parameter]
    public RenderFragment<TItem>? ItemTemplate { get; set; }

    /// <summary>
    /// Fired when an item is reordered within this list.
    /// Receives <c>(OldIndex, NewIndex)</c>.  Reorder <see cref="Items"/> here.
    /// </summary>
    [Parameter]
    public EventCallback<(int OldIndex, int NewIndex)> OnUpdate { get; set; }

    /// <summary>
    /// Fired when an item is moved out of this list into another group list
    /// (not fired in clone mode).
    /// Receives <c>(OldIndex, NewIndex)</c> where <c>NewIndex</c> is the insertion
    /// position in the target list.  Remove the item at <c>OldIndex</c> here.
    /// </summary>
    [Parameter]
    public EventCallback<(int OldIndex, int NewIndex)> OnRemove { get; set; }

    /// <summary>
    /// Fired when an item from another group list is dropped into this list.
    /// Receives <c>(Item, OldIndex, NewIndex)</c> — <c>Item</c> is the actual object
    /// being dropped so you do not need a separate tracking variable.
    /// <c>OldIndex</c> is the position in the source list; <c>NewIndex</c> is the
    /// insertion position here.  Insert <c>Item</c> at <c>NewIndex</c> here.
    /// </summary>
    [Parameter]
    public EventCallback<(TItem Item, int OldIndex, int NewIndex)> OnAdd { get; set; }

    /// <summary>
    /// Gets or sets an accessible label for the drag list container
    /// (mapped to <c>aria-label</c> on the root element).
    /// Screen readers use this to announce what the list contains.
    /// </summary>
    [Parameter]
    public string? AriaLabel { get; set; }

    /// <summary>
    /// Gets or sets additional CSS classes applied to the container element.
    /// Use flex/grid layout classes here (e.g. <c>"flex flex-col gap-2"</c> or
    /// <c>"grid grid-cols-4 gap-3"</c>).
    /// </summary>
    [Parameter]
    public string? Class { get; set; }

    // ─────────────────────────────────────────────────────────────────────────────
    // Lifecycle
    // ─────────────────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) { return; }

        if (Handle is null) { return; }

        // Install the handle filter via minimal JS only when a handle class is set.
        try
        {
            _jsModule = await JSRuntime.InvokeAsync<IJSObjectReference>(
                "import", "./_content/BlazorBlueprint.Components/js/drag-drop.js");

            await _jsModule.InvokeVoidAsync("setupHandleFilter", _container, Handle);
            _jsInitialized = true;
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException
                                        or ObjectDisposedException or InvalidOperationException)
        {
            // JS not available during prerendering or after circuit disconnect.
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_jsModule is not null && _jsInitialized)
        {
            try
            {
                await _jsModule.InvokeVoidAsync("removeHandleFilter", _container);
            }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException
                                            or ObjectDisposedException) { }
        }

        if (_jsModule is not null)
        {
            try { await _jsModule.DisposeAsync(); }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException
                                            or ObjectDisposedException) { }
        }

        GC.SuppressFinalize(this);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // HTML5 Drag and Drop event handlers
    // ─────────────────────────────────────────────────────────────────────────────

    private void HandleDragStart(DragEventArgs _, TItem item, int index)
    {
        if (!IsDraggable(item)) { return; }

        _localDragItem  = item;
        _localDragIndex = index;
        _dropTargetIndex = index;

        // Register in shared state for cross-list operations.
        if (Group is not null)
        {
            DragState.Begin(item, Group, Pull == "clone", index);
        }
    }

    private void HandleItemDragOver(int index)
    {
        // Always track the hovered slot, whether for same-list sort or cross-list.
        if (!CanReceiveDrop()) { return; }
        _dropTargetIndex  = index;
        _isContainerDragOver = true;
    }

    private void HandleContainerDragOver(DragEventArgs _)
    {
        if (!CanReceiveDrop()) { return; }
        _isContainerDragOver = true;
    }

    private void HandleContainerDragLeave(DragEventArgs _)
    {
        _isContainerDragOver = false;
        if (_localDragItem is null)
        {
            // Dragging from another list — reset the drop hint
            _dropTargetIndex = -1;
        }
    }

    private async Task HandleItemDrop(int index)
    {
        _isContainerDragOver = false;

        // ── Same-list reorder ─────────────────────────────────────────────
        if (_localDragItem is not null)
        {
            var fromIndex = _localDragIndex;
            var toIndex   = index;

            _localDragItem  = default;
            _localDragIndex = -1;
            _dropTargetIndex = -1;

            if (Sort && fromIndex != toIndex && OnUpdate.HasDelegate)
            {
                await OnUpdate.InvokeAsync((fromIndex, toIndex));
            }

            StateHasChanged();
            return;
        }

        // ── Cross-list drop ───────────────────────────────────────────────
        if (!Put || !CanReceiveCrossListDrop()) { return; }

        var item     = (TItem)DragState.Item!;
        var oldIndex = DragState.OldIndex;
        var newIndex = Math.Clamp(index, 0, Items.Count);

        DragState.RecordDrop(newIndex);

        _dropTargetIndex     = -1;
        _isContainerDragOver = false;

        if (OnAdd.HasDelegate)
        {
            await OnAdd.InvokeAsync((item, oldIndex, newIndex));
        }

        StateHasChanged();
    }

    private Task HandleContainerDrop(DragEventArgs _) =>
        // Fired when dropping onto the container itself (empty list or below all items).
        HandleItemDrop(Items.Count);

    private async Task HandleDragEnd(DragEventArgs _)
    {
        // Fires on the SOURCE after the drop (or after the drag is cancelled).
        var wasLocalDrag = _localDragItem is not null;

        _localDragItem   = default;
        _localDragIndex  = -1;
        _dropTargetIndex = -1;
        _isContainerDragOver = false;

        if (!wasLocalDrag && DragState.IsActive && DragState.DropOccurred
            && !DragState.IsClone && OnRemove.HasDelegate)
        {
            // Cross-list move: source must remove the item now that drop succeeded.
            var oldIndex = DragState.OldIndex;
            var newIndex = DragState.DropTargetIndex;
            DragState.Clear();
            await OnRemove.InvokeAsync((oldIndex, newIndex));
        }
        else
        {
            DragState.Clear();
        }

        StateHasChanged();
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────────

    private bool IsDraggable(TItem item)
    {
        if (!Sort && Group is null) { return false; }
        if (ItemDisabled?.Invoke(item) == true) { return false; }
        return true;
    }

    private bool CanReceiveDrop()
    {
        // Same-list drag: always fine (governed by Sort).
        if (_localDragItem is not null) { return Sort; }

        // Cross-list drag.
        return CanReceiveCrossListDrop();
    }

    private bool CanReceiveCrossListDrop()
    {
        if (!Put || !DragState.IsActive) { return false; }
        if (Group is null) { return false; }
        if (DragState.Group != Group) { return false; }
        // Don't accept if the dragged item is not of type TItem.
        if (DragState.Item is not TItem) { return false; }
        return true;
    }

    private string GetWrapperClass(TItem item, int index)
    {
        var classes = new StringBuilder();

        // Dim the item currently being dragged from this list.
        if (_localDragItem is not null && _localDragIndex == index)
        {
            classes.Append(" opacity-50 cursor-grabbing");
        }

        // Highlight the slot that would receive the drop.
        if (_dropTargetIndex == index && (_localDragItem is not null || CanReceiveCrossListDrop()))
        {
            classes.Append(" bb-drag-over");
        }

        return classes.ToString().TrimStart();
    }

    private string CssClass => ClassNames.cn("bb-drag-list", Class);

    private bool ShouldShowEmptyHint =>
        _isContainerDragOver && Items.Count == 0 && CanReceiveDrop();
}
