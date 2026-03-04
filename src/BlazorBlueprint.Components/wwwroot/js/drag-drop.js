/**
 * Drag-and-drop animation helpers for BbSortable.
 * Uses the FLIP (First, Last, Invert, Play) technique to animate items
 * to their new positions after a reorder or cross-zone transfer.
 */

/** @type {Map<Element, Map<Element, DOMRect>>} */
const snapshots = new Map();

/**
 * Captures the current bounding rectangles of all animatable children
 * in the container. Call this BEFORE the Blazor state change that reorders items.
 * @param {Element} containerEl - The sortable item container element.
 */
export function capturePositions(containerEl) {
    if (!containerEl) { return; }
    const rects = new Map();
    for (const child of containerEl.children) {
        if (child.dataset.flipIgnore) { continue; }
        rects.set(child, child.getBoundingClientRect());
    }
    snapshots.set(containerEl, rects);
}

/**
 * Plays the FLIP animation by comparing stored positions to current positions.
 * Call this in OnAfterRenderAsync AFTER Blazor has re-rendered.
 * @param {Element} containerEl - The sortable item container element.
 * @param {number} durationMs - Animation duration in milliseconds.
 */
export function playFlip(containerEl, durationMs) {
    if (!containerEl) { return; }
    const rects = snapshots.get(containerEl);
    if (!rects) { return; }
    snapshots.delete(containerEl);

    for (const child of containerEl.children) {
        if (child.dataset.flipIgnore) { continue; }
        if (!rects.has(child)) { continue; }

        const prev = rects.get(child);
        const curr = child.getBoundingClientRect();
        const dx = prev.left - curr.left;
        const dy = prev.top - curr.top;

        if (Math.abs(dx) < 1 && Math.abs(dy) < 1) { continue; }

        // Apply inverse transform (makes item appear at its old position)
        child.style.transition = 'none';
        child.style.transform = `translate(${dx}px, ${dy}px)`;

        // Next frame: animate back to natural position
        requestAnimationFrame(() => {
            child.style.transition = `transform ${durationMs}ms cubic-bezier(0.25, 0.46, 0.45, 0.94)`;
            child.style.transform = '';
            child.addEventListener('transitionend', () => {
                child.style.transition = '';
            }, { once: true });
        });
    }
}
