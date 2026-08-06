# Real-Time Hub Path — Feature Description

**Issue:** FDW-545
**Package:** `Fdw.SignalR` (building block) + per-domain hub assemblies
**Status:** Implemented

## Summary

FDW now ships a complete, inheritable real-time path. Adding a SignalR hub — in the framework or in
a downstream consumer — is a single `[TypeOption]` in the owning assembly. The host wires every hub
with two collection-driven calls (`RealTimeHubs.Register` before `Build()`, `app.MapRealTimeHubs()`
after) instead of a hand-maintained per-application extension.

## Problem

FDW shipped only the publish half of a realtime path (`SignalRBroadcaster<THub, TClient>` +
`AddBroadcaster`). Everything else was hand-rolled per domain, and the four hubs had drifted:

- **No hub base class.** `PipelineStatusHub`, `CalculationHub`, and `SchemaDiscoveryHub` each
  re-implemented connect/disconnect and logging; `MessageHub` was a bare `Hub` publishing through a
  raw `IHubContext` with stringly-typed `SendAsync("NewMessage", …)` calls.
- **Divergent semantics.** Publish-side roles were named inconsistently ("Broadcaster" vs
  "Notifier"); lifetimes were ad-hoc (Scoped vs Singleton); auth was inconsistent (two of four hubs
  `[Authorize]`, two anonymous); subscribe verbs and group-key prefixes were invented per hub.
- **An anonymous-identity fallback.** Two hubs auto-joined `user:{Context.User?.Identity?.Name ??
  "anonymous"}`, a silent fallback that violates the no-fallbacks rule.
- **Per-app registration.** Each hub needed a bespoke static `RegisterBroadcaster` threaded by hand
  into both an `AddFrameworkSignalR` callback and a `MapFrameworkSignalRHubs` callback in every
  consuming `Program.cs` — not the ServiceTypeCollection three-phase path the rest of FDW uses.

A downstream consumer re-implementing its own hub with divergent semantics is the same failure mode
one level out: with no inheritable path, every new consumer drifts.

## Solution

A building block in `Fdw.SignalR`, plus migration of the four existing hubs onto it.

### New types (`Fdw.SignalR`)

| Type | Role |
|------|------|
| `RealTimeHubBase<TClient>` | Base hub. Connect/disconnect logging, `NullLogger` fallback, uniform `Subscribe`/`Unsubscribe(scopeKey)`, `OnJoin`/`CanJoin` seams, `JoinAuthenticatedUserScope` (fail-loud, no placeholder identity), `AuthenticatedUserId`. |
| `IRealTimeHub` / `RealTimeHubOptionBase` | The hub descriptor: `Route`, `HubType`, `AuthorizationPolicy`, `RegisterServices`, `Map`. `MapHubAt<THub>` applies the route + authorization policy. Route and hub type are fail-loud (no defaults). |
| `RealTimeHubs` | `[TypeCollection]` of hub options. `Register(services, loggerFactory)` calls `AddSignalR()` once and registers each hub's broadcaster. |
| `RealTimeHubEndpointExtensions.MapRealTimeHubs` | Maps every hub at its declared route, applying its authorization policy. |

### Migrated hubs

`PipelineStatusHub`, `CalculationHub`, `SchemaDiscoveryHub`, and `MessageHub` now derive from
`RealTimeHubBase<TClient>`; each ships a `[TypeOption(typeof(RealTimeHubs), "…")]`. `MessageHub` gained a
typed `IMessageHubClient`, and `MessageService` now publishes through
`IHubContext<MessageHub, IMessageHubClient>` instead of stringly-typed `SendAsync`. The per-domain
`RegisterBroadcaster` statics were folded into the options and deleted, as was the
`Fdw.Hosting` SignalR hosting extension.

### Mandatory authentication

Authentication is **mandatory** for every FDW real-time hub. `RealTimeHubBase<TClient>` carries
`[Authorize]`, and `MapHubAt<THub>` always applies `RequireAuthorization` at the endpoint — the
declared `AuthorizationPolicy` when one is set, otherwise the default policy (an authenticated
principal). Enforcement lives on the **endpoint** (not on attribute-inheritance discovery), so no hub
can be mapped anonymously — there is no "skip authorization" branch. This closed the prior gap where
`PipelineStatusHub` and `MessageHub` were mapped with `authorizationPolicy: null` and served
anonymous connections. Per-verb policies (e.g. `[Authorize(Policy = "system:admin")]` on
`SubscribeToAllCalculations`) still compose on top.

Client side: a hub client supplies the caller's access token. In Blazor Server the connection is a
.NET SignalR client created on the circuit; it sends the token as an `Authorization: Bearer` header
on negotiate/connect (`options.AccessTokenProvider = () => TokenProvider.GetAccessToken()`), which the
hub host validates. No token ⇒ 401, the correct fail-loud behavior.

### Per-org firehose (no global cross-org group)

The old global `pipeline-updates` group was an unconditional cross-org firehose — every authenticated
connection received status/progress/completion for **every** organization's pipelines. It is replaced
by a per-**org** firehose: `org:{orgId}:pipeline-updates`.

- **Data model.** A pipeline carries an owning org: `pipe.Pipeline.OrgId` (nullable) →
  `PipelineConfiguration.OrgId`. The seed stamps the sample pipeline's owning org.
- **Connection side.** `PipelineStatusHub.OnJoin` reads the caller's `org_id` claim from the JWT and
  joins `org:{orgId}:pipeline-updates`. No `org_id` claim ⇒ no firehose join (logged
  `HubOrgClaimMissing`); never a global group, never a placeholder org.
- **Broadcast side.** The `PipelineExecutionBackgroundService` resolves the pipeline's owning `OrgId`
  once per execution (from the parent `PipelineConfiguration`) and threads it to every lifecycle
  broadcast. `PipelineStatusBroadcaster` adds `org:{orgId}:pipeline-updates` to the target groups when
  the org is present; a null org targets **no** firehose (there is no global group) — the
  `pipeline:{name}` / `execution:{id}` groups still deliver to explicit subscribers.

Net effect: an "all my pipelines" dashboard joins one group and sees only its own org's pipeline
lifecycle events. A connection in org A never receives org B's stream. Task/edge/pause/resume events
remain execution-scoped only (`execution:{id}`), unchanged. Project-level (orchestration-node) status
does not yet resolve an owning org and is broadcast with a null firehose (execution-scoped only) —
tracked as a follow-up.

## Logging

All paths log through `SignalRLog` (TypeCode `SIGNALR2`): connect, clean disconnect, error
disconnect, group join, group leave, empty-scope subscribe rejection, unauthorized subscribe
rejection, missing-identity skip, and each registration and mapping step. No silent paths.

## Testing

`Fdw.SignalR.Tests` — 31 tests, **100% line and 100% branch** on every hand-written type in the
building block and all four migrated hubs + options (verified with `dotnet-coverage`; the source
generated TypeCollection and LoggerMessage partials are excluded as generated code). No type required
`[ExcludeFromCodeCoverage]` — every hand-written line is exercised.

> Tooling note: the coverlet **collector** under-reports `Fdw.SignalR` / `Fdw.Services.Pipelines` /
> `Fdw.Services.Messaging` here (assemblies loaded by the test assembly's registration module
> initializer before the collector's instrumentation hook). Use `dotnet-coverage collect` for an
> accurate per-assembly number on this suite.

## Follow-ups

**Done (this cycle):**

- **Client dead-protocol fixed.** `Builder.razor` sent a `JoinGroup` verb no hub defines — it never
  joined `execution:{id}`, so live task/edge animations never arrived; it now calls the real
  `SubscribeToExecution(Guid)` verb. `ProjectExecution.razor` connected to a non-existent
  `/pipelinestatushub` route with dead `JoinProjectGroup`/`JoinStageGroup` verbs and events no server
  implements (there is no project-level broadcast path); the dead block was removed in favor of the
  existing status poll.
- **Mandatory authentication.** `[Authorize]` on the base + endpoint `RequireAuthorization` on every
  hub; client token wiring in `Builder.razor`. See *Mandatory authentication*.
- **Per-org firehose.** The global cross-org firehose is replaced by `org:{orgId}:pipeline-updates`,
  scoped to the pipeline's owning org. See *Per-org firehose*. (`pipe.Pipeline.OrgId` +
  `PipelineConfiguration.OrgId`; hub `OnJoin` from the `org_id` claim; broadcaster targets the org
  group; seed stamps the sample pipeline's org.)
- **Reference-app rewiring.** `reference-api` and `reference-etl` `Program.cs` now call
  `RealTimeHubs.Register` + `app.MapRealTimeHubs()`.

**Open:**

- **Org-authorized subscription.** `CanJoin` still permits any non-empty scope key. Authorizing a
  `pipeline:{name}` / `execution:{id}` subscription against the caller's org needs the target's org
  resolved at subscribe time (an async lookup the current sync `CanJoin` seam can't do) — the seam
  would become async.
- **Project-level firehose.** Orchestration-node (ETL project) status is broadcast with a null
  firehose (execution-scoped only); wiring the project's owning org mirrors the pipeline path.
- **Client-side hub abstraction** (`IRealTimeHubClient`) so consumers stop inlining a raw
  `HubConnectionBuilder`; speaks the same `Subscribe(scopeKey)` contract as the server base.
- **Client-side hub abstraction** (`IRealTimeHubClient`) so consumers stop inlining a raw
  `HubConnectionBuilder`; speaks the same `Subscribe(scopeKey)` contract as the server base.
