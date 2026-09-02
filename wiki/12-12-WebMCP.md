# 12-12 WebMCP

WebMCP is a hosting extension that offers FDW API endpoints to AI agents as structured tools, via
the W3C [WebMCP](https://webmcp.dev/) browser standard. When a host maps it, the server generates a
JavaScript file at `/.well-known/webmcp.js` that registers each tool through
`document.modelContext.registerTool()`.

An agent that lands on the page discovers the file at that well-known URL, reads the tool
definitions, and calls the endpoints directly with structured inputs. There is no MCP server process
to install and no per-client configuration — that is the whole point of the standard.

---

## Status: declared, not served

**No application currently maps WebMCP.** Nothing calls `MapWebMcp` or `UseWebMcpApiKeyAuth`, and
`reference-api` has no reference to the WebMcp packages at all.

291 endpoint options carry `[WebMcpTool]`, so `DeclaredWebMcpTools` fills during startup — and
nothing reads it. `/.well-known/webmcp.js` is not served by any host, and no agent can discover
anything.

What remains is a ServiceType plus a `.Registration` package: `AddWebMcp` in the Registration phase,
`MapWebMcp` in Initialization (which receives the built `IHost`), and the middleware placed ahead of
`UseAuthentication`. Read the rest of this page as the shape of the mechanism, not as a description
of a running system.

**Package:** `Fdw.WebMcp.Hosting` · **Attribute:** `Fdw.WebMcp.Abstractions`

---

## Declaring a tool

`[WebMcpTool]` goes on the endpoint **OPTION**, not on the endpoint class:

```csharp
[WebMcpTool("list_connections", "List all configured database connections", ReadOnly = true)]
[TypeOption(typeof(ConnectionEndpoints), "ListConnections")]
public class ListConnectionsOption : ConnectionEndpointBase<ListConnectionsEndpoint>;
```

The option is where an endpoint already declares itself, and `EndpointTypeOptionBase.Register` is the
one place that knows an endpoint is switched on. An endpoint that is never declared is never routed,
so marking the class instead would offer agents tools for routes that return 404.

Tools are **gathered from options that attached themselves**. They are not swept out of an assembly
list — `AddWebMcp` takes no assemblies, and there is no discovery scan. An earlier design did scan,
and it found nothing: the routes it needed live inside FastEndpoints `Configure()` bodies, which no
assembly scan can read.

### Attribute parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `name` | `string` | Yes | Tool name exposed to agents. snake_case, e.g. `list_connections`. |
| `description` | `string` | Yes | What the tool does. This is what an agent uses to choose it, so be specific. |
| `ReadOnly` | `bool` | No | Adds `annotations.readOnlyHint`. Default `false`. |
| `HttpMethod` | `string` | No | Only needed when an endpoint maps more than one verb and the route table offers no single answer. |

The attribute carries **no route**. The route lives in the endpoint's `Configure()` body, and a copy
here would be a second source of truth free to drift from the one the router actually matches. It is
read from the live route table at `MapWebMcp`.

---

## Route and method resolution

Both come from the application's own `EndpointDataSource` at `MapWebMcp`, joined to the declarations
by endpoint type. The endpoint class is read off FastEndpoints' `EndpointDefinition` metadata by
NAME rather than by type, so the package keeps no hard dependency on a FastEndpoints version.

A declaration that resolves to no route is **skipped with a warning** — the option declared it, so a
missing route is a contradiction rather than an ordinary absence. A declaration that resolves to more
than one route/verb pair is skipped as ambiguous; set `HttpMethod` to choose.

---

## Input schema

The schema is derived from the endpoint's request DTO (`ReqDtoType`), and is gated on **whether that
type exists** — not on the HTTP verb. A `GET` that declares a request type gets real inputs: gating
on the verb, as an earlier version did, left every `GET` and `DELETE` with `properties: {}` even when
the endpoint declared a DTO, so a path value had no field to arrive through and no list could be
filtered.

Supported CLR → JSON Schema mappings:

| CLR Type | JSON Type | Format |
|----------|-----------|--------|
| `string` | `string` | — |
| `bool` | `boolean` | — |
| `int`, `long`, `short` | `integer` | — |
| `double`, `float`, `decimal` | `number` | — |
| `Guid` | `string` | `uuid` |
| `DateTime`, `DateTimeOffset` | `string` | `date-time` |
| `DateOnly` | `string` | `date` |
| `TimeOnly` | `string` | `time` |
| `Nullable<T>` | (unwrapped) | — |

Complex types, collections and arrays are omitted, with a warning naming the property — the agent
cannot supply it, and a silent omission looks identical to a field that was never wanted.

**Path parameters are the only entries in `required`.** That is structural, not a validation
preference: the URL cannot be constructed without them. Every other property stays optional, and
required-ness for those belongs to the endpoint's own FluentValidation.

---

## Path parameters

A route parameter is substituted into the URL, not sent as a field:

- Each `{Name}` is replaced with `encodeURIComponent(input["Name"])`. The URL is built by
  concatenation rather than string replacement, so a value containing `/` or `?` cannot reshape the
  path it lands in.
- Constraints and modifiers bind on the name alone: `{id:int}`, `{name?}`, `{page=1}` and `{*rest}`
  all bind to `id`, `name`, `page`, `rest`.
- Path parameters are **stripped from the request body**. An endpoint handed the same value twice can
  disagree with itself about which one binds.
- A route parameter with no matching property on the request DTO means the tool is **skipped with a
  warning** rather than emitted. A tool that always 404s is worse than a missing one, because an
  agent cannot tell a 404 from a genuine empty result.

Properties that are not path parameters, on a verb that carries no body, become a **query string** —
only for values the agent actually supplied, so an omitted filter widens the result rather than
narrowing it to the empty string.

---

## Recovering from a wrong identifier

A bare 404 is a dead end: an agent cannot tell a wrong identifier from an empty result, and nothing
in the response says what a right one would look like.

When a tool has exactly one path parameter and its parent collection route is itself a declared tool,
a failed call returns the collection alongside the error:

```json
{
  "error": "404 Not Found",
  "validValues": [ ... ],
  "hint": "'Name' did not match. failure.validValues lists what is available; 'list_connections' returns the same set."
}
```

The parent is matched against **resolved routes**, never by string prefix. A resource's list route is
frequently computed rather than written — `CrudListEndpointBase` builds `/{ResourceName}` — so
nothing textual is reliable.

**The recovery fetch reuses the caller's headers, so it runs as the same principal and can never
surface values the caller could not have listed itself.** That is a guarantee of the design, not an
incidental detail; preserve it in any change to this path.

A tool with more than one path parameter gets no hint. Which collection a second parameter selects
from is not derivable from the route, and naming the wrong one is worse than naming none.

---

## Generated JavaScript

The script is an IIFE served at `/.well-known/webmcp.js` with `Cache-Control: public, max-age=3600`,
excluded from the API description.

It prefers `document.modelContext` and falls back to `navigator.modelContext`. The spec moved the API
from `navigator` to `document` because tools belong to a page; Chrome 150 deprecated the `navigator`
form while the Chrome 149 origin trial still served it, so one generated script works across the
Chrome 149–156 trial window.

```js
(function() {
  var modelContext =
    (typeof document !== 'undefined' && document.modelContext) ||
    (typeof navigator !== 'undefined' && navigator.modelContext) ||
    null;
  if (!modelContext) return;

  modelContext.registerTool({
    name: "get_connection_health",
    description: "Health for one connection.",
    annotations: { readOnlyHint: true },
    inputSchema: {
      type: "object",
      properties: {
        "Name": { "type": "string" },
        "Deep": { "type": "boolean" }
      },
      required: ["Name"]
    },
    execute: async function(input) {
      let url = "/connections/" + encodeURIComponent(input["Name"]) + "/health";
      const q = query(input, ["Deep"]);
      if (q) url += "?" + q;
      const init = {
        method: "GET",
        headers: { "Accept": "application/json" }
      };
      const r = await fetch(url, init);
      if (!r.ok) {
        const failure = { error: r.status + " " + r.statusText };
        try {
          const alt = await fetch("/connections", { method: "GET", headers: { "Accept": "application/json" } });
          if (alt.ok) {
            failure.validValues = await alt.json();
            failure.hint = "'Name' did not match. ...";
          }
        } catch (e) { }
        return failure;
      }
      return await r.json();
    }
  });
})();
```

The registry is a singleton resolved once at `MapWebMcp`; the script itself is rebuilt per request,
so a change in what is exposed takes effect without a restart.

---

## Agent authentication

`UseWebMcpApiKeyAuth()` authenticates `Authorization: Bearer fdx_*` credentials and hands anything
else to the next handler (JWT bearer, cookies). Both credential kinds are minted by the same
generator; the environment segment separates them, and `agent` is reserved for keys minted by
`IAgentKeyService`.

| Credential | Shape | Validated by | Claims emitted |
|---|---|---|---|
| Agent key | `fdx_agent_…` | `IAgentKeyService.ValidateKey` | `sub`, `agent`, `agentLabel`, `agentKeyId` |
| Personal access token | `fdx_{env}_…` | `IPersonalAccessTokenService.ValidateToken` | `sub` |

They are told apart by prefix **before** validation, not by trying one service and falling back to
the other: a fallback reports an unrecognised agent key as a bad token, and an operator reading the
log cannot tell which credential actually failed. With no `IAgentKeyService` registered the answer is
401, never a retry as a PAT.

The agent claims sit **beside** `sub` and never replace it. An agent acts on behalf of its owner, so
its `sub` IS that person's — every permission check, RLS predicate and ownership test downstream must
keep seeing the person. The claims say who is driving, not who is acting.

`agent` is also the reason those claims exist at all: nothing else in the token distinguishes an
agent from the person it acts for, and several things must — audit rows, message attribution, and any
policy gating what an agent may do unattended.

### Client key

`WebMcpOptions.ClientApiKey` is injected into every `fetch()` in the generated script, under
`ApiKeyHeader` (default `X-Webmcp-Key`). **That value is embedded in a publicly cached JavaScript
file** — use a low-privilege, read-only key, store it in secrets rather than `appsettings.json`, and
never reuse an admin key.

---

## Design decisions

**Opt-in only.** Not every endpoint should be an agent tool. `[WebMcpTool]` is an explicit statement
of intent; an option without it is invisible to agents.

**No route on the attribute.** The router is the single source of truth for where an endpoint lives.

**Refuse rather than emit a broken tool.** An unresolvable route, an ambiguous route, and an
unbindable path parameter all skip the tool with a warning. The agent cannot diagnose a tool that
cannot work, so it should never be offered one.

**Required means structurally required.** Only path parameters.

---

## Known gaps

- Complex request properties (arrays, nested objects) are omitted from the schema.
- Multi-parameter routes get no valid-values recovery.
- The tool list is not filtered by the caller's permissions — every caller is offered every declared
  tool and discovers its own 403s. The OpenAPI document already filters per caller
  (`PermissionFilterDocumentProcessor`); this does not yet.
- `Cache-Control: public` is correct only while the script is identical for every caller. Filtering
  it per principal requires `private`/`no-store` first, or a shared cache will serve one caller's
  tool list to another.
