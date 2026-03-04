using System.Diagnostics.CodeAnalysis;
using BlazorBlueprint.Primitives.Utilities;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorBlueprint.Components;

/// <summary>
/// A drop-target zone that accepts items dragged from <see cref="BbSortable{T}"/> components.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="BbDropZone{T}"/> is a sink-only drop target. It accepts dragged items but has no
/// items of its own. Use it alongside <see cref="BbSortable{T}"/> inside a shared
/// <see cref="BbDropContainer{T}"/> to create drag-and-drop UIs where items can be moved
/// from a sortable list into a target area (e.g. a trash can or an "archive" column).
/// </para>
/// <para>
/// When no <see cref="ChildContent"/> is provided the zone renders a built-in drop indicator
/// that reacts to the drag state automatically.
/// </para>
/// </remarks>
/// <typeparam name="T">The type of item that can be dropped. Must be non-null.</typeparam>
public partial class BbDropZone<T> : ComponentBase, IDisposable where T : notnull
{
    private int _dragOverCount;

    [CascadingParameter]
    private BbDropContainer<T>? Container { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier for this drop zone.
    /// Used by <see cref="BbDropContainer{T}.CanDrop"/> to determine whether drops are allowed.
    /// </summary>
    [Parameter, EditorRequired]
    public string ZoneIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the content to render inside the drop zone.
    /// When omitted a default drop-indicator UI is shown.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Gets or sets a template that renders a preview of the item being dragged
    /// when the cursor is hovering over this zone and the drop is allowed.
    /// Only used when <see cref="ChildContent"/> is not set.
    /// The template receives the dragged item as its <c>context</c>.
    /// </summary>
    [Parameter]
    public RenderFragment<T>? DragItemTemplate { get; set; }

    /// <summary>
    /// Gets or sets a per-zone function that overrides the container-level
    /// <see cref="BbDropContainer{T}.CanDrop"/> check.
    /// Return <c>true</c> to allow a drop, <c>false</c> to reject it.
    /// </summary>
    [Parameter]
    public Func<T, bool>? CanDrop { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when an item is successfully dropped into this zone.
    /// </summary>
    [Parameter]
    public EventCallback<DropItemInfo<T>> OnItemDropped { get; set; }

    /// <summary>
    /// Gets or sets the accessible label announced by screen readers.
    /// Defaults to "Drop zone {ZoneIdentifier}".
    /// </summary>
    [Parameter]
    public string? AriaLabel { get; set; }

    /// <summary>
    /// Gets or sets additional CSS classes applied when a valid drag is hovering over this zone.
    /// </summary>
    [Parameter]
    public string? DragOverClass { get; set; }

    /// <summary>
    /// Gets or sets additional CSS classes applied when an invalid drag (CanDrop = false)
    /// is hovering over this zone.
    /// </summary>
    [Parameter]
    public string? DragOverNoDropClass { get; set; }

    /// <summary>
    /// Gets or sets additional CSS classes applied to the root element.
    /// </summary>
    [Parameter]
    public string? Class { get; set; }

    private bool IsDragOver => _dragOverCount > 0;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (Container is not null)
        {
            Container.TransactionStarted += OnTransactionChanged;
            Container.TransactionEnded += OnTransactionChanged;
        }
    }

    private void OnTransactionChanged(object? sender, EventArgs e) => StateHasChanged();

    /// <inheritdoc />
    public void Dispose()
    {
        if (Container is not null)
        {
            Container.TransactionStarted -= OnTransactionChanged;
            Container.TransactionEnded -= OnTransactionChanged;
        }

        GC.SuppressFinalize(this);
    }

    private bool CanDropCurrentItem()
    {
        if (Container is null || !Container.HasTransaction)
        {
            return false;
        }

        var item = Container.DraggedItem;
        if (item is null)
        {
            return false;
        }

        if (CanDrop is not null)
        {
            return CanDrop(item);
        }

        return Container.CanDropItem(ZoneIdentifier);
    }

    private void HandleDragEnter(DragEventArgs args) => _dragOverCount++;

    private void HandleDragLeave(DragEventArgs args) => _dragOverCount = Math.Max(0, _dragOverCount - 1);

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Blazor event handler must be instance method.")]
    private void HandleDragOver(DragEventArgs args)
    {
        // preventDefault is handled by the :preventDefault directive on the element
    }

    private async Task HandleDrop(DragEventArgs args)
    {
        _dragOverCount = 0;

        if (Container is null || !Container.HasTransaction || !CanDropCurrentItem())
        {
            return;
        }

        var item = Container.DraggedItem;
        var sourceZone = Container.SourceZone;
        await Container.CommitTransactionAsync(ZoneIdentifier, -1);

        if (OnItemDropped.HasDelegate && item is not null)
        {
            await OnItemDropped.InvokeAsync(new DropItemInfo<T>(item, sourceZone, ZoneIdentifier, -1));
        }
    }

    private string ZoneClass => ClassNames.cn(
        "relative flex items-center justify-center",
        "min-h-[100px] rounded-lg border-2 border-dashed",
        "transition-all duration-200",
        IsDragOver && CanDropCurrentItem()
            ? ClassNames.cn("border-primary bg-primary/5 scale-[1.01]", DragOverClass)
            : IsDragOver
                ? ClassNames.cn("border-destructive bg-destructive/5", DragOverNoDropClass)
                : Container?.HasTransaction == true && CanDropCurrentItem()
                    ? "border-primary/40 bg-primary/5"
                    : "border-muted-foreground/30 bg-muted/10",
        Class
    );
}
