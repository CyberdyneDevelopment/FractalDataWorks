// cytoscape-interop.js
// ES module — Cytoscape.js canvas interop for Fdw.UI.Canvas.Blazor.
// PackageId: Fdw.UI.Canvas.Blazor
// Vendored UMD lib: cytoscape.min.js, shipped alongside this module in wwwroot/js.

// Why resolved from import.meta.url rather than a literal '/_content/<PackageId>/js/...':
// the vendored bundle always sits next to this module, so deriving its URL from this
// module's own URL is correct by construction. A hardcoded absolute path had to be kept
// in sync with the PackageId by hand — it was still spelled FractalDataWorks.* after the
// rename to Fdw.*, so every load 404'd — and it also breaks whenever the app is hosted
// under a sub-path, because the leading '/' escapes the base href.
const _LIB_SRC = new URL('./cytoscape.min.js', import.meta.url).href;

// Why: WeakMap keyed by the container element so instances are automatically released
// when the element is garbage-collected, with no manual bookkeeping required.
const _instances = new WeakMap();

/**
 * Ensures window.cytoscape is available.
 * Injects the vendored UMD script tag and awaits its onload event when absent.
 */
async function ensureCytoscape() {
    if (window.cytoscape) {
        return;
    }
    await new Promise((resolve, reject) => {
        const script = document.createElement('script');
        script.src = _LIB_SRC;
        script.onload = resolve;
        script.onerror = () => reject(new Error('Failed to load Cytoscape lib from ' + _LIB_SRC));
        document.head.appendChild(script);
    });
}

/**
 * Renders (or re-renders) a Cytoscape graph inside the given element.
 * @param {HTMLElement} element - The host container element (@ref from Blazor).
 * @param {Array} elements - Cytoscape elements array:
 *   nodes: [{data:{id,label}, position:{x,y}}]
 *   edges: [{data:{id,source,target,label}}]
 * @param {string} layoutName - Cytoscape layout name ('preset', 'breadthfirst', etc.).
 */
export async function render(element, elements, layoutName) {
    await ensureCytoscape();

    // Destroy any existing instance for this element before re-creating.
    const existing = _instances.get(element);
    if (existing) {
        existing.destroy();
        _instances.delete(element);
    }

    const cy = window.cytoscape({
        container: element,
        elements: elements,
        layout: {
            name: layoutName,
            padding: 30,
        },
        style: [
            {
                selector: 'node',
                style: {
                    'background-color': '#1e293b',
                    'border-color': '#334155',
                    'border-width': 1.5,
                    'label': 'data(label)',
                    'color': '#e2e8f0',
                    'font-size': '10px',
                    'font-family': 'monospace',
                    'text-valign': 'center',
                    'text-halign': 'center',
                    'width': 120,
                    'height': 40,
                    'shape': 'roundrectangle',
                },
            },
            {
                selector: 'edge',
                style: {
                    'width': 1.5,
                    'line-color': '#64748b',
                    'target-arrow-color': '#64748b',
                    'target-arrow-shape': 'triangle',
                    'curve-style': 'bezier',
                    'label': 'data(label)',
                    'font-size': '9px',
                    'font-family': 'monospace',
                    'color': '#64748b',
                    'text-rotation': 'autorotate',
                },
            },
        ],
    });

    _instances.set(element, cy);
}

/**
 * Destroys the Cytoscape instance associated with the given element.
 * @param {HTMLElement} element - The container element.
 */
export function dispose(element) {
    const cy = _instances.get(element);
    if (cy) {
        cy.destroy();
        _instances.delete(element);
    }
}
