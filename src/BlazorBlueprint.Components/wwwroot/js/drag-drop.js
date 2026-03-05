/**
 * Drag-and-drop helpers for BbDragDrop.
 *
 * Follows the exact BlazorSortable DOM-reversion pattern:
 * for every drag operation the DOM is reverted to its pre-drag state BEFORE the
 * .NET callbacks are invoked.  This keeps Blazor's virtual-DOM diff in sync with
 * the real DOM and guarantees stable, correct re-renders regardless of list size.
 *
 * API:
 *   init(element, dotNetRef, options)  — create a SortableJS instance on element
 *   destroy(element)                   — destroy the SortableJS instance
 *
 * .NET callbacks invoked on dotNetRef:
 *   JsOnUpdate(oldIndex, newIndex)  — in-list reorder
 *   JsOnRemove(oldIndex, newIndex)  — item left this list (source of a move or clone)
 *   JsOnAdd(oldIndex, newIndex)     — item arrived in this list (target of a move or clone)
 */
import Sortable from './sortable.esm.js';

/** @type {Map<Element, Sortable>} */
const instances = new Map();

/**
 * @param {Element} element
 * @param {object}  dotNetRef  Blazor DotNetObjectReference
 * @param {object}  options
 */
export function init(element, dotNetRef, options) {
    destroy(element);
    if (!element || !dotNetRef) { return; }

    const group = options.group
        ? { name: options.group, pull: options.pull || true, put: options.put !== false }
        : null;

    const sortable = Sortable.create(element, {
        group,
        sort:          options.sort !== false,
        handle:        options.handle  ? '.' + options.handle  : undefined,
        filter:        options.filter  ? '.' + options.filter  : undefined,
        animation:     options.animation ?? 150,
        forceFallback: options.forceFallback === true,
        ghostClass:    'bb-drag-ghost',
        chosenClass:   'bb-drag-chosen',
        dragClass:     'bb-drag-dragging',

        onUpdate(evt) {
            // Revert DOM so Blazor re-renders from a clean pre-drag state.
            evt.item.remove();
            evt.to.insertBefore(evt.item, evt.to.children[evt.oldIndex] ?? null);

            dotNetRef.invokeMethodAsync('JsOnUpdate', evt.oldIndex, evt.newIndex)
                     .catch(() => {});
        },

        onRemove(evt) {
            // In clone mode SortableJS leaves a clone in the source — remove it.
            if (evt.pullMode === 'clone') {
                evt.clone.remove();
            }

            // Revert: move the item out of the target and back into the source.
            evt.item.remove();
            evt.from.insertBefore(evt.item, evt.from.children[evt.oldIndex] ?? null);

            dotNetRef.invokeMethodAsync('JsOnRemove', evt.oldIndex, evt.newIndex)
                     .catch(() => {});
        },

        onAdd(evt) {
            // The DOM was already reverted by the source's onRemove handler above.
            // Just notify the target .NET instance so it can update its data model.
            dotNetRef.invokeMethodAsync('JsOnAdd', evt.oldIndex, evt.newIndex)
                     .catch(() => {});
        },
    });

    instances.set(element, sortable);
}

/**
 * Destroys the SortableJS instance on the element. Safe to call multiple times.
 * @param {Element} element
 */
export function destroy(element) {
    if (!element) { return; }
    const inst = instances.get(element);
    if (inst) {
        inst.destroy();
        instances.delete(element);
    }
}
