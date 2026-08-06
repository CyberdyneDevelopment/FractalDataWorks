# Blazor Hosting Models

FractalDataWorks supports two Blazor hosting models for Management UI implementations. The reference repos demonstrate one of each. Both connect to the same `reference-api` backend and surface the same domain pages from FDW's `*.UI.Pages` packages.

## The Two Hosting Models

| | InteractiveServer | InteractiveAuto |
|---|---|---|
| **Reference repo** | `reference-ui` | `reference-aui` |
| **CSS Framework** | Tailwind CSS v4 | MudBlazor + custom theme |
| **Rendering** | Server-side via SignalR | Server first, then WASM |
| **Auth Storage** | sessionStorage | sessionStorage (both sides) |

## InteractiveServer (`reference-ui`)

All UI logic runs on the server. The browser receives a lightweight page and communicates with the server over a persistent SignalR WebSocket connection. Every button click, form submission, and navigation event is a round-trip to the server. The server computes the DOM diff and sends it back to the browser.

### When to Use

- **Internal tools** with a small number of concurrent users (tens, not thousands)
- **Admin dashboards** behind a corporate VPN where latency to the server is low
- Applications that need **direct access to server resources** (databases, file systems, internal services) without an API layer
- When you need **fast initial load** and don't want users to download a WASM runtime
- When **offline support is not required**

### Architecture

```
Browser                          Server
┌──────────────────┐            ┌──────────────────────────────┐
│                  │  SignalR   │  Blazor Circuit              │
│  Rendered HTML   │◄──────────►│  - Component tree in memory  │
│  + blazor.web.js │  WebSocket │  - Event handlers            │
│                  │            │  - Scoped DI container        │
│                  │            │  - Token in sessionStorage    │
└──────────────────┘            │  - HTTP calls to API          │
                                └──────────────────────────────┘
```

### Auth Pattern

JWT auth uses sessionStorage via Blazored.SessionStorage. The login page posts credentials to the API and stores the resulting tokens in sessionStorage from the server circuit (via JS interop). A `DelegatingHandler` attaches the access token to every outgoing API call. Refer to `reference-ui/public/Program.cs` and the `Services/Auth/` folder for the canonical wiring.

Tokens are not visible to other browser tabs (sessionStorage is tab-scoped) and clear when the tab closes.

**Security note:** HTTP calls originate from the server, not the browser. The API does not receive cross-origin requests from this model — no CORS configuration is needed for the UI's own traffic.

## InteractiveAuto (`reference-aui`)

The application starts with server-side rendering (InteractiveServer), then transparently transitions to client-side WebAssembly rendering once the WASM runtime has been downloaded and cached. Subsequent visits start in WASM immediately.

### When to Use

- **Public-facing applications** where first-load performance matters but you also want client-side execution
- Applications that benefit from **reduced server load** after the initial visit
- When you want the **smoothest possible user experience** (fast first paint, then full client-side interactivity)
- When your team can handle the **added complexity** of two execution environments

### Architecture

```
First Visit (Server Mode):
Browser                          Server
┌──────────────────┐  SignalR   ┌──────────────────────────┐
│  Rendered HTML   │◄──────────►│  Blazor Circuit          │
│  + blazor.web.js │            │  Auth state serialized   │
│  (downloads WASM │            │  to WASM client via      │
│   runtime in bg) │            │  AddAuthentication-      │
└──────────────────┘            │  StateSerialization      │
                                └──────────────────────────┘

Subsequent Visits (WASM Mode):
Browser
┌──────────────────────────────────────┐
│  WASM Runtime (cached)               │
│  ┌─────────────────────────────────┐ │       ┌──────────┐
│  │  Blazor Components              │ │  HTTP  │          │
│  │  Client-side auth + clients     │─┼───────►│  API     │
│  └─────────────────────────────────┘ │ (CORS) │          │
└──────────────────────────────────────┘       └──────────┘
```

### Auth Pattern: Serialized State with Dual Providers

This is the most complex auth pattern because services must work in two execution environments:

- **Server mode (first visit):** login goes through the server, auth state is serialized to the WASM client via `AddAuthenticationStateSerialization(options => options.SerializeAllClaims = true)`.
- **WASM mode (after transition):** subsequent operations talk directly from the browser to the API (cross-origin, CORS-gated).

**Two separate DI containers exist:** the server project has its own `Program.cs`; the client project has its own `Program.cs` with WASM-compatible service registrations. Both must register compatible implementations of the same interfaces.

Refer to the `reference-aui` repository for the canonical startup of both halves.

## Tradeoffs Matrix

| Aspect | InteractiveServer | InteractiveAuto |
|--------|-------------------|-----------------|
| **Initial Load** | Fast (small payload) | Fast (Server first) |
| **Subsequent Loads** | Same as first | Fast (WASM cached) |
| **Latency** | Every interaction is a round-trip | Round-trips initially, then local |
| **Server Resources** | High (circuit per user) | Moderate (circuits for first visits) |
| **Scalability** | Limited by server memory | Better than Server alone |
| **Offline Capable** | No | Partially (in WASM mode) |
| **SignalR Required** | Yes (always) | Yes (first visit only) |
| **Download Size** | ~50 KB (JS only) | ~50 KB first, then several MB WASM |
| **Auth Complexity** | Low (single environment) | High (dual environment) |
| **API CORS Needed** | No (server-to-server) | Yes (in WASM mode) |
| **Project Count** | 1 project | 2 projects (Server + Client) |

## Decision Flowchart

- **Internal tool, small team?** InteractiveServer (`reference-ui` shape). Simplest to build and debug.
- **Public app, need fast first paint AND scale?** InteractiveAuto (`reference-aui` shape). Accept the dual-environment complexity.
- **Not sure?** Start with InteractiveServer. You can always migrate to InteractiveAuto later by adding a Client project.

## Further Reading

- [ASP.NET Core Blazor render modes](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/render-modes) — official documentation on render modes
- [Blazor Server overview](https://learn.microsoft.com/en-us/aspnet/core/blazor/hosting-models#blazor-server) — server-side hosting model
- [ASP.NET Core Blazor authentication and authorization](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/) — auth patterns for Blazor
- [Call a web API from Blazor](https://learn.microsoft.com/en-us/aspnet/core/blazor/call-web-api) — HTTP client patterns
