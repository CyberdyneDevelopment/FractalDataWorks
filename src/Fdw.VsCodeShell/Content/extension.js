// FractalDataWorks VS Code Shell — bootstrap
// Generic loader: spawns the .NET host shipped alongside this file, reads its manifest,
// and dynamically registers each command + webview with VS Code. No domain-specific code.
//
// The .NET host is expected at: <extensionPath>/bundle/<EntryDll>
// EntryDll is read from package.json "fdw.entryDll".
//
// Manifest contract (GET /vscode/manifest):
//   { extensionId, displayName, commands: [{id,title,category,contextKind}],
//     webviews: [{viewType,title,openCommandId,path,retainContextWhenHidden}] }
//
// Command invocation (POST /vscode/commands/{id}):
//   body = EditorContext { documentUri,languageId,cursorLine,cursorCharacter,
//                          selectionText,wordUnderCursor }
//
// Webviews are opened in reaction to their declared openCommandId firing.

const vscode = require("vscode");
const { spawn } = require("child_process");
const net = require("net");
const path = require("path");
const http = require("http");

let hostProcess = null;
let hostBaseUrl = null;
let panels = new Map(); // viewType -> WebviewPanel

async function activate(context) {
    const pkg = require(path.join(context.extensionPath, "package.json"));
    const entryDll = (pkg.fdw && pkg.fdw.entryDll) || "";
    if (!entryDll) {
        vscode.window.showErrorMessage("FDW shell: package.json is missing fdw.entryDll");
        return;
    }

    const externalUrl = vscode.workspace.getConfiguration().get("fdw.externalHostUrl", "");
    if (externalUrl) {
        hostBaseUrl = externalUrl.replace(/\/+$/, "");
    } else {
        const port = await pickEphemeralPort();
        hostBaseUrl = `http://localhost:${port}`;
        await startHost(context, entryDll, port);
    }

    const manifest = await fetchManifest(hostBaseUrl);
    registerCommands(context, manifest);
}

function deactivate() {
    for (const panel of panels.values()) {
        try { panel.dispose(); } catch (_) {}
    }
    panels.clear();
    if (hostProcess) {
        try { hostProcess.kill(); } catch (_) {}
        hostProcess = null;
    }
}

async function pickEphemeralPort() {
    return await new Promise((resolve, reject) => {
        const server = net.createServer();
        server.unref();
        server.on("error", reject);
        server.listen(0, () => {
            const port = server.address().port;
            server.close(() => resolve(port));
        });
    });
}

async function startHost(context, entryDll, port) {
    const dllPath = path.join(context.extensionPath, "bundle", entryDll);
    const env = Object.assign({}, process.env, {
        ASPNETCORE_URLS: `http://localhost:${port}`,
        DOTNET_ENVIRONMENT: process.env.DOTNET_ENVIRONMENT || "Production",
    });
    hostProcess = spawn("dotnet", [dllPath], { env, stdio: ["ignore", "pipe", "pipe"] });

    hostProcess.stdout.on("data", (b) => console.log(`[fdw-host] ${b.toString().trimEnd()}`));
    hostProcess.stderr.on("data", (b) => console.error(`[fdw-host] ${b.toString().trimEnd()}`));
    hostProcess.on("exit", (code) => {
        console.log(`[fdw-host] exited code=${code}`);
        hostProcess = null;
    });

    await waitForHealth(`http://localhost:${port}/vscode/health`, 15000);
}

async function waitForHealth(url, timeoutMs) {
    const deadline = Date.now() + timeoutMs;
    while (Date.now() < deadline) {
        try {
            const ok = await new Promise((resolve) => {
                const req = http.get(url, (res) => { resolve(res.statusCode === 200); res.resume(); });
                req.on("error", () => resolve(false));
                req.setTimeout(500, () => { req.destroy(); resolve(false); });
            });
            if (ok) return;
        } catch (_) {}
        await new Promise((r) => setTimeout(r, 200));
    }
    throw new Error(`Host did not become healthy at ${url}`);
}

async function fetchManifest(baseUrl) {
    return await new Promise((resolve, reject) => {
        http.get(`${baseUrl}/vscode/manifest`, (res) => {
            let body = "";
            res.on("data", (chunk) => (body += chunk));
            res.on("end", () => {
                try { resolve(JSON.parse(body)); } catch (e) { reject(e); }
            });
        }).on("error", reject);
    });
}

function registerCommands(context, manifest) {
    const declaredIds = new Set((manifest.commands || []).map((c) => c.id));
    for (const wv of manifest.webviews || []) {
        if (!declaredIds.has(wv.openCommandId)) {
            // Why: fail loud. A webview whose openCommandId matches no declared command can never
            // open, and silently doing nothing is exactly what hid this defect until now.
            vscode.window.showErrorMessage(
                `FDW shell: webview '${wv.viewType}' declares openCommandId '${wv.openCommandId}', but no command with that id exists.`);
        }
    }

    for (const cmd of manifest.commands || []) {
        const id = cmd.id;
        const contextKind = cmd.contextKind || "none";
        // Why: the manifest contract is "a webview opens when its OpenCommandId fires", so the panel
        // must open from this same handler. Registering a separate "<id>.open" command satisfied the
        // contract on paper but nothing ever invoked it, so no panel was ever created.
        const webviews = (manifest.webviews || []).filter((wv) => wv.openCommandId === id);
        const handler = async () => {
            const ctx = captureEditorContext(contextKind);
            try {
                const result = await postJson(`${hostBaseUrl}/vscode/commands/${encodeURIComponent(id)}`, ctx);
                if (!result || !result.ok) {
                    const msg = (result && result.message) || "command failed";
                    vscode.window.showErrorMessage(`${cmd.title}: ${msg}`);
                    return;
                }
                // Why: open only after the host acknowledges — the panel iframes the host, so opening
                // on a failed dispatch would render a blank webview pointed at a dead endpoint.
                for (const wv of webviews) {
                    openWebview(context, wv);
                }
            } catch (e) {
                vscode.window.showErrorMessage(`${cmd.title}: ${e.message || e}`);
            }
        };
        context.subscriptions.push(vscode.commands.registerCommand(id, handler));
    }
}

function openWebview(context, descriptor) {
    const existing = panels.get(descriptor.viewType);
    if (existing) {
        existing.reveal(vscode.ViewColumn.Beside);
        return existing;
    }
    const panel = vscode.window.createWebviewPanel(
        descriptor.viewType,
        descriptor.title,
        vscode.ViewColumn.Beside,
        {
            enableScripts: true,
            retainContextWhenHidden: !!descriptor.retainContextWhenHidden,
        }
    );
    panel.webview.html = buildWebviewHtml(hostBaseUrl + descriptor.path);
    panel.onDidDispose(() => panels.delete(descriptor.viewType));
    panels.set(descriptor.viewType, panel);
    return panel;
}

function buildWebviewHtml(targetUrl) {
    // Why: VS Code webview CSP defaults to same-origin only; the localhost host serves
    // scripts/styles from http://localhost:<port> so frame-src, script-src, connect-src,
    // and style-src must allow localhost:* or the iframe loads blank.
    const csp = [
        "default-src 'none'",
        "frame-src http://localhost:*",
        "script-src http://localhost:* 'unsafe-inline'",
        "connect-src http://localhost:* ws://localhost:*",
        "style-src http://localhost:* 'unsafe-inline'",
    ].join("; ");
    return `<!DOCTYPE html>
<html><head>
<meta http-equiv="Content-Security-Policy" content="${csp}">
<style>html,body,iframe{margin:0;padding:0;border:0;width:100%;height:100%;}</style>
</head><body><iframe src="${targetUrl}"></iframe></body></html>`;
}

function captureEditorContext(kind) {
    const editor = vscode.window.activeTextEditor;
    if (!editor || kind === "none") {
        return {
            DocumentUri: null, LanguageId: null,
            CursorLine: null, CursorCharacter: null,
            SelectionText: null, WordUnderCursor: null,
        };
    }
    const doc = editor.document;
    const pos = editor.selection.active;
    const sel = editor.selection.isEmpty ? null : doc.getText(editor.selection);
    let word = null;
    if (kind === "cursor" || kind === "document") {
        const range = doc.getWordRangeAtPosition(pos);
        if (range) word = doc.getText(range);
    }
    return {
        DocumentUri: doc.uri.toString(),
        LanguageId: doc.languageId,
        CursorLine: pos.line,
        CursorCharacter: pos.character,
        SelectionText: sel,
        WordUnderCursor: word,
    };
}

async function postJson(url, body) {
    const data = JSON.stringify(body);
    const u = new URL(url);
    const opts = {
        hostname: u.hostname, port: u.port, path: u.pathname + u.search, method: "POST",
        headers: { "Content-Type": "application/json", "Content-Length": Buffer.byteLength(data) },
    };
    return await new Promise((resolve, reject) => {
        const req = http.request(opts, (res) => {
            let raw = "";
            res.on("data", (c) => (raw += c));
            res.on("end", () => {
                try { resolve(JSON.parse(raw || "{}")); } catch (e) { reject(e); }
            });
        });
        req.on("error", reject);
        req.write(data); req.end();
    });
}

module.exports = { activate, deactivate };
