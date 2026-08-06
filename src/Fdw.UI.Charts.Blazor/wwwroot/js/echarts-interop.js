// echarts-interop.js
// ES module — imported dynamically by EChartsRenderer via
//   JS.InvokeAsync("import", "./_content/Fdw.UI.Charts.Blazor/js/echarts-interop.js")
//
// Exports:
//   render(element, option)  — init or update an ECharts instance on element
//   dispose(element)         — dispose the ECharts instance attached to element

// Why: WeakMap keyed by the DOM element so instances are automatically released when the
// element is garbage-collected, with no manual bookkeeping required. Multiple ECharts
// renderers on the same page each own their own instance without a shared global registry.
const _instances = new WeakMap();

// Why resolved from import.meta.url rather than a literal '/_content/<PackageId>/js/...':
// the vendored bundle always sits next to this module, so deriving its URL from this module's
// own URL is correct by construction. A hardcoded absolute path had to be kept in sync with the
// PackageId by hand — it was still spelled FractalDataWorks.* after the rename to Fdw.*, so every
// load 404'd — and it also breaks whenever the app is hosted under a sub-path, because the
// leading '/' escapes the base href.
const _LIB_SRC = new URL('./echarts.min.js', import.meta.url).href;

// Why: ensure the vendored UMD bundle is loaded before calling window.echarts.
// Injecting a <script> tag and awaiting its onload event is the standard pattern for
// lazily loading UMD globals from inside an ES module (dynamic import() does not work for UMDs
// that assign to window globals).
async function ensureLoaded() {
    if (window.echarts) {
        return;
    }
    await new Promise(function (resolve, reject) {
        var script = document.createElement('script');
        script.src = _LIB_SRC;
        script.onload = resolve;
        script.onerror = function () {
            reject(new Error('Failed to load echarts.min.js from ' + _LIB_SRC));
        };
        document.head.appendChild(script);
    });
}

// render(element, option)
// Ensures the ECharts UMD bundle is loaded, then initialises or updates the chart instance
// attached to element. Re-calling render with a new option updates the existing instance
// without reinitialising it, which preserves animations and zoom state.
export async function render(element, option) {
    if (!element) {
        return;
    }

    await ensureLoaded();

    var instance = _instances.get(element);
    if (!instance) {
        instance = window.echarts.init(element);
        _instances.set(element, instance);
    }

    // Why: notMerge=true ensures the previous option is fully replaced rather than merged,
    // which avoids stale series data when chart type or encodings change.
    instance.setOption(option, true);
}

// dispose(element)
// Disposes the ECharts instance attached to element and removes it from the map.
// Safe to call when no instance exists (e.g. error state, never rendered).
export function dispose(element) {
    if (!element) {
        return;
    }

    var instance = _instances.get(element);
    if (instance) {
        instance.dispose();
        _instances.delete(element);
    }
}
