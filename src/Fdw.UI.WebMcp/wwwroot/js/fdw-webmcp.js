// fdw-webmcp.js
// ES module — imported dynamically by WebMcpBridge via
//   JS.InvokeAsync("import", "./_content/Fdw.UI.WebMcp/js/fdw-webmcp.js")
//
// Exports:
//   register(handle, dotNetRef, toolsJson) — register page-scoped tools, returns a result object
//   unregister(handle)                     — abort the registrations owned by handle
//   isSupported()                          — feature-detect the WebMCP model context
//
// Each WebMcpBridge instance owns one handle (a GUID string) and one AbortController, so a page
// may host several bridges and each tears down independently on component dispose.

// Why: keyed by the bridge's handle rather than a DOM element — a WebMcpBridge registers tools
// against the document, not against a node, so there is no element to key a WeakMap on. The
// bridge always calls unregister() from DisposeAsync, so entries do not leak.
const _controllers = new Map();

// Why: Chrome 149 shipped the WebMCP origin trial as navigator.modelContext; the spec moved the
// API to document.modelContext (tools belong to a page, not a browser) and Chrome 150 deprecated
// the navigator form while the origin trial still serves it. Preferring document and accepting
// navigator keeps one build working across 149-156 instead of breaking on whichever the visitor
// happens to run. This is a browser-API compatibility shim, not a configuration default.
function resolveModelContext() {
    if (typeof document !== 'undefined' && document.modelContext) {
        return document.modelContext;
    }
    if (typeof navigator !== 'undefined' && navigator.modelContext) {
        return navigator.modelContext;
    }
    return null;
}

// isSupported()
// True when the running browser exposes a WebMCP model context. Callers use this to distinguish
// "no agent surface in this browser" (the normal case today) from "registration failed".
export function isSupported() {
    return resolveModelContext() !== null;
}

// register(handle, dotNetRef, toolsJson)
// Registers every tool in toolsJson against the model context. Returns
//   { supported: bool, registered: int, failed: [{ name, error }] }
// so the .NET side can log a structured outcome instead of guessing.
export async function register(handle, dotNetRef, toolsJson) {
    const modelContext = resolveModelContext();
    if (!modelContext) {
        return { supported: false, registered: 0, failed: [] };
    }

    // Why: a re-register for the same handle (tools changed after first render) must not leave the
    // previous generation registered — abort the old controller before installing the new one.
    unregister(handle);

    const controller = new AbortController();
    _controllers.set(handle, controller);

    const tools = JSON.parse(toolsJson);
    const failed = [];
    let registered = 0;

    for (const tool of tools) {
        const descriptor = {
            name: tool.name,
            description: tool.description,
            inputSchema: tool.inputSchema,
            // Why: the agent passes arguments as an object; the .NET side owns all parsing and
            // validation, so forward the raw JSON text and return whatever string it produces.
            // The shipping Chrome API expects execute() to resolve to a string.
            execute: async (args) => {
                return await dotNetRef.invokeMethodAsync(
                    'ExecuteTool',
                    tool.name,
                    JSON.stringify(args ?? {}));
            },
        };

        if (tool.annotations) {
            descriptor.annotations = tool.annotations;
        }

        try {
            await modelContext.registerTool(descriptor, { signal: controller.signal });
            registered++;
        } catch (e) {
            // Why: one malformed tool must not abort the whole page's registration — collect the
            // failure and carry on, then hand the list back so .NET logs each one at Error.
            failed.push({ name: tool.name, error: String(e && e.message ? e.message : e) });
        }
    }

    return { supported: true, registered: registered, failed: failed };
}

// unregister(handle)
// Aborts the AbortController for handle, which unregisters every tool registered under it.
// Safe to call when the handle is unknown (never registered, or already torn down).
export function unregister(handle) {
    const controller = _controllers.get(handle);
    if (controller) {
        controller.abort();
        _controllers.delete(handle);
    }
}
