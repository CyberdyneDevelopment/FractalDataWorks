# 12-16 Real-Time Hub Path

This guide covers the FDW real-time building block: the path for adding a SignalR hub that is
discovered, registered, mapped, authorized, and logged the same way across every domain. A new hub
is a single `[TypeOption]` in its owning assembly — no per-application wiring.

## Architecture

```
RealTimeHubBase<TClient>                  ← base hub: lifecycle logging, NullLogger fallback,
   ▲                               uniform Subscribe/Unsubscribe(scopeKey), OnJoin/CanJoin seams
   │ inherits
PipelineStatusHub / CalculationHub / SchemaDiscoveryHub / MessageHub

SignalRBroadcaster<THub,TClient> ← base broadcaster: BroadcastToGroup(s)/BroadcastToAll
   ▲
PipelineStatusBroadcaster / CalculationNotifier / SchemaDiscoveryNotifier

IRealTimeHub  (RealTimeHubOptionBase)   ← one [TypeOption] per hub: Route, HubType,
   │                                       AuthorizationPolicy, RegisterServices, Map
   ▼
RealTimeHubs  [TypeCollection] + [PlatformServiceProvider]
   ├─ RealTimeHubs.Register(services, loggerFactory)       ← before Build(): AddSignalR + each broadcaster
   │                                                          (run by the PlatformServices sweep)
   └─ app.MapRealTimeHubs(loggerFactory)                   ← after Build(): map each hub at its Route (manual)
```

Three moving parts:

- **`RealTimeHubBase<TClient>`** (in `Fdw.SignalR`) — the base every hub derives from. It owns the
  connect/disconnect logging, the `logger ?? NullLogger<T>.Instance` fallback, and the single
  client-facing subscribe contract `Subscribe(scopeKey)` / `Unsubscribe(scopeKey)`. It never
  auto-joins a group by default; a hub opts into an automatic join by overriding `OnJoin`.
- **`SignalRBroadcaster<THub, TClient>`** (in `Fdw.SignalR`) — the publish-side base. Domain
  broadcasters derive from it and call `BroadcastToGroup(s)` / `BroadcastToAll`.
- **`RealTimeHubs`** (in `Fdw.SignalR`) — a `[TypeCollection]` of `IRealTimeHub` descriptors, also
  marked `[PlatformServiceProvider(ServiceCategory = "RealTimeHubs")]` so its three-phase methods join
  the `PlatformServices` sweep. Each hub registers a `[TypeOption(typeof(RealTimeHubs), "...")]`
  in its own assembly; the entry-point app's `Registration.SourceGenerators` module initializer
  registers them at load.

## Hosting wiring

No callbacks, no hard-coded hub list. `RealTimeHubs.Register` (Phase 1) is run for you by the single
`PlatformServices` sweep — its `Configure` and `Initialize` are no-ops declared only to satisfy
the `[PlatformServiceProvider]` shape. The one call each host still makes by hand is the post-Build
endpoint mapping:

```csharp
// Phase 1 — before Build(): PlatformServices sweep runs RealTimeHubs.Register (AddSignalR()
// once + every hub's broadcaster) alongside every other domain.
PlatformServices.Register(builder.Services, loggerFactory);

var app = builder.Build();

// After Build(): map every hub at the route its option declares. NOT part of the three-phase shape,
// so each host calls it directly.
app.MapRealTimeHubs(loggerFactory);
```

`RealTimeHubs.Register` iterates the collection and calls each option's `RegisterServices`;
`MapRealTimeHubs` calls each option's `Map`, applying the option's authorization policy when one is
declared. New hubs in referenced assemblies are picked up automatically.

## Adding a hub

A complete hub is three small types in the owning domain assembly.

**1. The typed client interface** — the server-to-client contract:

```csharp
public interface IInventoryHubClient
{
    Task StockChanged(StockChangedEvent evt);
}
```

**2. The hub** — derive from `RealTimeHubBase<TClient>`:

```csharp
public sealed class InventoryHub : RealTimeHubBase<IInventoryHubClient>
{
    protected override string HubName => "Inventory";

    public InventoryHub(ILogger<InventoryHub> logger) : base(logger) { }

    // Optional: auto-join a per-user group on connect (skips + logs when unauthenticated).
    protected override Task OnJoin() => JoinAuthenticatedUserScope();

    // Domain verbs are thin key-builders over the inherited Subscribe/Unsubscribe contract.
    public Task SubscribeToItem(string sku) => Subscribe($"item:{sku}");
    public Task UnsubscribeFromItem(string sku) => Unsubscribe($"item:{sku}");
}
```

**3. The hub option** — register it against `RealTimeHubs`:

```csharp
[TypeOption(typeof(RealTimeHubs), "Inventory")]
public sealed class InventoryHubOption : RealTimeHubOptionBase
{
    public InventoryHubOption()
        : base(5, "Inventory", "/hubs/inventory", typeof(InventoryHub), authorizationPolicy: null) { }

    public override void RegisterServices(IServiceCollection services, ILoggerFactory? loggerFactory)
        => services.AddBroadcaster<IInventoryBroadcaster, InventoryBroadcaster, InventoryHub, IInventoryHubClient>(loggerFactory);

    public override void Map(IEndpointRouteBuilder endpoints) => MapHubAt<InventoryHub>(endpoints);
}
```

The owning project needs the Collections source generator analyzer reference so the `[TypeOption]`
is emitted:

```xml
<ProjectReference Include="..\Fdw.Collections.SourceGenerators\Fdw.Collections.SourceGenerators.csproj"
                  OutputItemType="Analyzer" ReferenceOutputAssembly="false" PrivateAssets="all" />
```

That is the whole onboarding. No `AddXxx` extension, no `Program.cs` edit — the option is discovered
through the collection.

## The subscribe contract

`RealTimeHubBase<TClient>` exposes one uniform pair of verbs:

| Member | Behavior |
|--------|----------|
| `Subscribe(scopeKey)` | Validates the key is non-empty, checks `CanJoin(scopeKey)`, joins the group, logs the join. Empty or unauthorized requests are logged and ignored — never a silent malformed join. |
| `Unsubscribe(scopeKey)` | Validates non-empty, leaves the group, logs the leave. |
| `OnJoin()` (override) | Auto-join on connect. Default is a no-op — the base never joins a global firehose by default. |
| `CanJoin(scopeKey)` (override) | Authorize a subscribe against the caller. Default permits any non-empty key; override to add tenant/org scoping. |
| `JoinAuthenticatedUserScope()` | Joins `user:{userId}` from the authenticated identity; when there is no identity it logs `HubIdentityMissing` and **skips** — it never substitutes a placeholder identity (no `?? "anonymous"`). |

Group-key conventions are `{domain}:{id}` (e.g. `execution:{id}`, `pipeline:{name}`, `calc:{id}`,
`discovery:{id}`), `user:{userId}`, and `all-{domain}` for admin fan-out.

## Authorization

Authentication is **mandatory** — there is no anonymous FDW real-time hub:

- The base `RealTimeHubBase<TClient>` carries `[Authorize]`, and `MapHubAt` **always** calls
  `RequireAuthorization` on the mapped endpoint — the declared `authorizationPolicy` when one is
  passed, otherwise the default policy (an authenticated principal). Enforcement lives on the
  endpoint (not on attribute-inheritance discovery), so a `null` policy still requires auth — it is
  never a path to anonymous access.
- Narrower per-verb rules compose on top with `[Authorize(Policy = "system:admin")]` on a specific
  hub method (e.g. an admin-only "subscribe to all" verb).

**Clients** must send the caller's access token. A .NET SignalR client (including the one a Blazor
Server circuit creates) sets `options.AccessTokenProvider` so the token rides as an
`Authorization: Bearer` header on negotiate/connect; the hub host validates it. No token ⇒ 401.

**Per-org firehose.** The old global `pipeline-updates` group was an unconditional cross-org leak; it
is replaced by `org:{orgId}:pipeline-updates`, scoped to a pipeline's owning org
(`pipe.Pipeline.OrgId` → `PipelineConfiguration.OrgId`). `PipelineStatusHub.OnJoin` joins the group
for the caller's `org_id` claim (no claim ⇒ no firehose, logged); the background executor resolves the
pipeline's owning `OrgId` and the broadcaster targets that group (null org ⇒ no firehose — there is no
global group). Task/edge/pause/resume stay execution-scoped (`execution:{id}`). `CanJoin` still permits
any non-empty scope key — org-authorized subscription (per-execution/pipeline) is a follow-up.

## Logging

Every path is logged through `SignalRLog` (TypeCode `SIGNALR2`): hub connect/disconnect (including
the error-disconnect variant), each group join/leave, each subscribe rejection (empty scope /
unauthorized), the missing-identity skip, and each registration and mapping step. There is no silent
path — a rejected subscribe, a skipped auto-join, and an empty hub set all emit a log entry.

## Related

- [12-01 Creating a Server](12-01-Creating-A-Server.md) — where `Register` / `MapRealTimeHubs` slot into startup
- [12-05 Authorization](12-05-Authorization.md) — the `system:admin` and `resource:action` policies hubs reuse
- [04-01 TypeCollections Overview](04-01-Overview.md) — the `[TypeCollection]` / `[TypeOption]` mechanism `RealTimeHubs` is built on
