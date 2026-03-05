/**
 * Drag-and-drop helpers for BbDragDrop — a thin BlazorSortable-style wrapper around SortableJS.
 *
 * API:
 *   init(element, dotNetRef, options)  — create/update a SortableJS instance
 *   destroy(element)                   — destroy a SortableJS instance
 *
 * Callbacks invoked on dotNetRef:
 *   JsOnUpdate(oldIndex, newIndex)  — item reordered within the same list
 *   JsOnAdd(oldIndex, newIndex)     — item added to this list from another group list
 *   JsOnRemove(oldIndex, newIndex)  — item removed from this list to another group list
 */
import Sortable from './sortable.esm.js';

/** @type {Map<Element, Sortable>} */
const instances = new Map();

/**
 * Initialises (or re-initialises) a SortableJS instance on the element.
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
        group:         group,
        sort:          options.sort !== false,
        handle:        options.handle  ? '.' + options.handle  : undefined,
        filter:        options.filter  ? '.' + options.filter  : undefined,
        animation:     options.animation ?? 150,
        forceFallback: options.forceFallback === true,
        ghostClass:    'bb-drag-ghost',
        chosenClass:   'bb-drag-chosen',
        dragClass:     'bb-drag-dragging',

        onUpdate(evt) {
            dotNetRef.invokeMethodAsync('JsOnUpdate', evt.oldIndex, evt.newIndex).catch(() => {});
        },
        onAdd(evt) {
            dotNetRef.invokeMethodAsync('JsOnAdd', evt.oldIndex, evt.newIndex).catch(() => {});
        },
        onRemove(evt) {
            dotNetRef.invokeMethodAsync('JsOnRemove', evt.oldIndex, evt.newIndex).catch(() => {});
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
