/**
 * Minimal drag-drop helpers for BbDragDrop.
 *
 * The component uses the native HTML5 Drag and Drop API via Blazor event handlers
 * for all drag logic — no external library needed.
 *
 * This module provides one optional helper for the Handle feature:
 *   setupHandleFilter(element, handleClass)
 *     Attaches a capture-phase dragstart listener to `element` that cancels any drag
 *     that does not originate from a descendant with the given CSS class.
 *     This lets a drag handle restrict which part of an item can initiate a drag
 *     without requiring Blazor to intercept every individual mousedown event.
 */

/** @type {Map<Element, (e: DragEvent) => void>} */
const handleListeners = new Map();

/**
 * Installs (or replaces) a handle-class filter on the given container element.
 * Call once per component when Handle is set.
 *
 * @param {Element} element     The BbDragDrop container element.
 * @param {string}  handleClass The CSS class that marks valid drag handles.
 */
export function setupHandleFilter(element, handleClass) {
    // Remove any previously installed listener for this element.
    removeHandleFilter(element);
    if (!element || !handleClass) { return; }

    const listener = (/** @type {DragEvent} */ e) => {
        if (!e.target || !(/** @type {Element} */ (e.target)).closest('.' + handleClass)) {
            e.preventDefault();
            e.stopPropagation();
        }
    };

    element.addEventListener('dragstart', listener, true /* capture */);
    handleListeners.set(element, listener);
}

/**
 * Removes the handle filter from the given element. Safe to call when no filter exists.
 * @param {Element} element
 */
export function removeHandleFilter(element) {
    if (!element) { return; }
    const listener = handleListeners.get(element);
    if (listener) {
        element.removeEventListener('dragstart', listener, true);
        handleListeners.delete(element);
    }
}
