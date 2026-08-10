# FDW Hosts — Final Design (docs/FDW-HOSTS-DESIGN.md)

**Status:** APPROVED FOR IMPLEMENTATION (synthesized from the design tournament: *builder* won 2 of 3 judge lenses — consumer experience + migration reality, and mechanism soundness — and its doctrine gaps are closed here by grafts from *typecollection* and *minimalist*. Every judge criticism of the winning design is resolved in §9; one policy decision requires explicit user sign-off, §10.)

**Branch context:** extends the in-flight `feature/platform-services-generator` work (`Fdw.Services.Registration` / `PlatformServices`).

---

## 1. Direct answers

**(a) Do we need an FDW host abstraction handling startup + registration? Yes.** The 3-way Program.cs diff is the proof: ~80% of each file is copy-pasted boot/middleware ceremony that has already drifted 15 documented ways (bounded vs unbounded Serilog flush, raw `UseFastEndpoints` vs `UseFdwFastEndpoints` — which silently loses `PermissionClaimsPreProcessor` on bare endpoints, empty `AddRateLimiter(_ => { })` stubs, `ProgramLog` vs `StartupLog`, contradictory `AddAuthorization()` comments, three ForwardedHeaders variants). Boot sequencing and middleware total order are *mechanism*; per doctrine #6 mechanism belongs to the framework. Today no type owns the boot sequence; four apps each own a divergent copy. That is a case-(a) system inadequacy — the fix lands in the mechanism.

**(b) Do we need a single/central app? No.** Three hard reasons, convergent across all three designs and all three judges:
1. **A central app is all values, no mechanism.** It would bundle deployment topology, gateway cache posture (api = cached, etl/scheduler = cacheless *by construction* — `architecture_gateway_cache.md`; one process collapses cross-process freshness-by-construction), and domain package sets into one artifact. Doctrine #6 forbids the framework owning values.
2. **Failure/scale/key-custody isolation.** ETL saturating its executor must not take down token issuance; the scheduler tick must survive API load; issuer key custody is a distinct security domain. Preview slots, blue/green, and per-service pinning all assume independent binaries.
3. **Module initializers sweep everything referenced** — a central app forces every domain collection into every deployment.

The centralization the instinct actually wants is a single central **mechanism**: one shared boot spine + archetype packages + `PlatformServices`.

**(c) Should there be Fdw.Hosts with api/ui/scheduler/auth flavors? Yes — as archetype builder packages, with one correction: the archetype is `Worker`, not `Scheduler`.** Scheduler and ETL are the same host *shape* (hosted services + control-plane FastEndpoints API + outbound machine identity + post-Initialize fail-loud gate); they differ only in which domain packages they reference, and domain arrival is already `PlatformServices`' job via package references. A scheduler-named host package would bake domain values into a hosting package (doctrine #6 violation). Final archetypes: **`Fdw.Hosting.Api`**, **`Fdw.Hosting.Worker`** (etl *and* scheduler), **`Fdw.Hosting.Ui`**, **`Fdw.Hosting.AuthServer`**. Archetype = **compile-time package boundary** (an Api host pulls FastEndpoints/SignalR/Scalar; a Ui host pulls Blazor; neither pulls the other's graph) — which is why archetypes are packages, not a `HostTypes` TypeCollection (§8-R2).

---

## 2. The chosen architecture

### 2.1 Package layout (exact)

| Project | TFM | Contents |
|---|---|---|
| `Fdw.Hosting.Core` **(NEW, slim)** | net10.0 | Refs: ASP.NET shared framework, Serilog, `Fdw.MessageLogging.Abstractions`, `Fdw.Services.Registration`. Contains: `internal sealed class FdwHostSpine` (shared bucket-A boot sequence), `HostLog` (**the single** `[MessageLogging]` class for boot — kills `ProgramLog`/`StartupLog` drift), `FdwConfigurationBootstrap`, `FdwDomainRegistrationContext`, `FdwHostCompositionException`, `FdwHostBuilderBase<TSelf>` / `FdwHttpHostBuilderBase<TSelf>` (CRTP shared builder bases), `StartupResult`/`StartupStepResult` (moved), `AddFrameworkSerilog`/`FlushFrameworkSerilog` (moved), `MapFrameworkHealthEndpoint` (moved), `ForwardedHeadersDefaults`. *Why this package exists:* `Fdw.Hosting.Ui` must get shell/Serilog/health without dragging `Fdw.Hosting`'s ~30 domain references. |
| `Fdw.Hosting` (existing, extended) | net10.0 | Refs `Fdw.Hosting.Core`. Keeps `UseFrameworkApplicationPipeline`, CORS/OTel/rate-limit extensions. Gains `AddFdwDownstreamClient` (§2.3). |
| `Fdw.Hosting.Api` **(NEW)** | net10.0 | `FdwApiHost` / `FdwApiHostBuilder`. Refs Fdw.Hosting, Fdw.Web.Api, Fdw.Web.RestEndpoints, FastEndpoints.Swagger, Scalar, Fdw.SignalR, Fdw.Services.Multitenancy. |
| `Fdw.Hosting.Worker` **(NEW)** | net10.0 | `FdwWorkerHost` / `FdwWorkerHostBuilder`. Refs Fdw.Hosting, FastEndpoints.Swagger, Scalar, Fdw.SignalR. |
| `Fdw.Hosting.Ui` **(NEW)** | net10.0 | `FdwUiHost` / `FdwUiHostBuilder`. Refs **`Fdw.Hosting.Core` only** plus `Fdw.Web.Http.Authentication.Blazor`, `Fdw.UI.Navigation`. **No ConfigurationGateway path exists in this package** — the UI-never-touches-ConfigurationDb seam is structural, not a runtime flag. |
| `Fdw.Hosting.AuthServer` **(NEW)** | net10.0 | `AddTokenIssuer(this FdwApiHostBuilder)` (co-hosted issuance, today's topology) and `FdwAuthServerHost` (standalone issuer, future). Refs Fdw.Services.Authentication.OpenIddict. Owns `AuthAndTagDocumentProcessor` (moves out of Reference.Api), TokenSwitch loopback client wiring (base URL from **required** config — the `?? "http://localhost:5020"` chain dies), Scalar demo-credentials banner feature. |
| `Fdw.Hosting.MsSql` (existing) | net10.0 | Unchanged role: backend-flavor meta-package (transitive refs for `MsSqlConnectionFactory` + friends). |
| `Fdw.Hosting.Abstractions` (existing, **shrinks**) | netstandard2.0 | **DELETE** `IFdwHost`, `IFdwHostBuilder`, `IFdwHostBuilderContext`, `IFdwHostLifetime`, `IFdwHostApplicationLifetime`, `FdwHostOptions`, `FeatureOptions`, and the unconsumed `LogLevels`/`Sinks`/`TelemetryExporters` catalogs (all grep-confirmed dead). |
| `Fdw.Services.Registration` (+ `.SourceGenerators`) (in-flight, extended) | netstandard2.0 | `Group` attribute argument, async `Initialize` returning `IGenericResult`, `Verify` member, `InitializeAllAsync`/`VerifyAllAsync`, `Entries()` (§5). `SetGroup` **deleted** — one mechanism. |
| `Fdw.Services.HostRegistry.Abstractions` **(NEW)** | netstandard2.0 | `MonitoredHostTypes` `[ServiceTypeCollection(ServiceCategory = "MonitoredHost")]`, `IHostRegistryClient`, registration payload records. |
| `Fdw.Services.HostRegistry.Client` **(NEW)** | net10.0 | `ApiRegisteredHostType` `[ServiceTypeOption]` + `MonitoredHostRegistrar : IHostedService` (registered by the **option's** `RegisterRequiredServices`, never by the spine). |
| `Fdw.Services.HostRegistry` **(NEW)** | net10.0 | Server side: registration endpoints, storage, `LocalRegisteredHostType` option (in-process registration for the host that hosts the registry), permission seed `hostregistry.register`. |
| D-bucket relocation targets | | `Fdw.Services.Scheduling` gains the scheduler mechanism (`SchedulerBackgroundService`, `EtlDispatchService`, dispatch client wiring, `SchedulerTypes.Verify` gate); `Fdw.Calculations.PreCompute` **(NEW)** gains `PreComputeCalculationsJob` + `CalculationApiClient`; `Fdw.UI.Themes.Scalar` **(NEW)** gains the ~150 lines of Scalar theming/tenants-js/preview-hosts (hosts from config); `Fdw.Web.Http.Authentication.Blazor` gains `AddFdwBlazorTokenClient` (cookie scheme, ROPC/refresh via `IHttpClientFactory` — the raw `new HttpClient` in `OnValidatePrincipal` dies) + `MapFdwBlazorAuthEndpoints` (login/logout minimal APIs); `Fdw.Services.Credentials` gains `OutboundIdentityTypes` (§4). |

`Registration.SourceGenerators` module initializers run only in entry-point apps (doctrine #7): the reference app csproj references the generator; archetype packages are ordinary libraries.

### 2.2 IServiceTypeCollection / PlatformServices mechanism changes (grafted from *typecollection* + *minimalist*)

```csharp
// Fdw.Services.Registration
public interface IServiceTypeCollection
{
    string ServiceCategory { get; }
    Type CollectionType { get; }
    int Group { get; }   // NEW: from [ServiceTypeCollection(Group = n)] — the DOMAIN declares its own
                         // dependency layer on itself; generator emits it. PlatformServices.SetGroup is DELETED.
                         // Canonical layers: SecretManagers=0, Connections=1, DataGateway/DataStores=2,
                         // DataSets=3, DataVault=4, Credentials=5, Users=6, Authentication/Authorization=7,
                         // everything else defaults to 10 — each declared BY that domain, not by a spine table.
    Action<IHostApplicationBuilder, ILoggerFactory?> Configure { get; }
    Action<IServiceCollection, ILoggerFactory?> Register { get; }
    Func<IServiceProvider, ILoggerFactory?, CancellationToken, Task<IGenericResult>> Initialize { get; }
                         // NEW: async + IGenericResult. Generator wraps today's sync Initialize methods.
                         // Absorbs the two awaited-init irregularities (DataStoreProvider.Initialize,
                         // TransformationConfigurationProvider.InitializeDomainServices) into the uniform sweep.
    Func<IServiceProvider, ILoggerFactory?, CancellationToken, Task<IGenericResult>>? Verify { get; }
                         // NEW: post-Initialize fail-loud gate, generator-detected. Domain-owned startup
                         // validation lives HERE (SchedulerTypes.Verify), not in app code.
}

public static class PlatformServices
{
    public static IHostApplicationBuilder Configure(IHostApplicationBuilder builder, ILoggerFactory? lf = null);
    public static void Register(IServiceCollection services, ILoggerFactory? lf = null);
    public static Task<IGenericResult> InitializeAllAsync(IServiceProvider provider, ILoggerFactory? lf = null, CancellationToken ct = default);
    public static Task<IGenericResult> VerifyAllAsync(IServiceProvider provider, ILoggerFactory? lf = null, CancellationToken ct = default);
    public static IReadOnlyList<IServiceTypeCollection> Entries();
    // generated per-category dot-walk properties stay (tests, tooling, diagnostics)
}
```

Both sweeps return `IGenericResult` so there is **one** fail-loud exit path (Initialize failure and Verify failure are both `HostLog.BootStepFailed` + exit 1). Additional uniformity fixes riding on the same branch: `ConnectionTypes.RegisterAdditionalInterfaces` folds into its `Register`; `DataStoreProvider`/`DataSetProvider` get conforming `[ServiceTypeCollection]` descriptors registered by module initializer in their own packages; the three hardcoded root `RegisterDomainServices("ConfigurationDb","sec"/"data"/"notify")` calls finish their migration into the owning domains' ServiceTypeOptions.

### 2.3 Public API — bootstrap, context, builders

```csharp
namespace Fdw.Hosting.Core;

public sealed record FdwConfigurationBootstrap
{
    // The ONLY factory. Caller names the file and the backend types explicitly — no default filename,
    // no default backend. Missing/malformed file fails loud at registration exactly as today.
    public static FdwConfigurationBootstrap File<TConnectionFactory, TSecretManager>(string jsonFilePath)
        where TConnectionFactory : class, IConnectionFactory
        where TSecretManager : class, ISecretManager;

    internal void Apply(IServiceCollection services); // -> AddConfigurationGateway<TCf,TSm>(jsonFilePath)
}

public sealed class FdwDomainRegistrationContext
{
    public IConfiguration Configuration { get; }
    public ILoggerFactory LoggerFactory { get; }
    public IServiceCollection Services { get; }  // MIGRATION-ONLY escape hatch, policed by FDW022 (§6).
                                                 // Removal plan: [Obsolete] after M6, deleted next minor.
    public FdwDomainRegistrationContext Options<TOptions>(string sectionName) where TOptions : class;
    public FdwDomainRegistrationContext OptionsValidator<TOptions, TValidator>()
        where TOptions : class where TValidator : class, IValidateOptions<TOptions>;
}

// CRTP bases kill With*-surface copy-drift INSIDE the framework (shared slots live exactly once).
public abstract class FdwHostBuilderBase<TSelf> where TSelf : FdwHostBuilderBase<TSelf>
{
    // Every With* validates eagerly; duplicate non-repeatable calls throw FdwHostCompositionException.
    public TSelf WithDomainServices(Action<FdwDomainRegistrationContext> register);   // at-most-once
    public TSelf WithStartupGate(string gateName,
        Func<IServiceProvider, ILoggerFactory, CancellationToken, Task<IGenericResult>> gate);
        // APP-OWNED gates only (e.g. validating an app-owned Options section). DOMAIN gates belong on
        // the descriptor's Verify (§2.2). Repeatable, keyed; duplicate key throws.
    public Task<WebApplication> BuildAsync(CancellationToken cancellationToken = default);  // WebApplicationFactory test seam
    public Task<int> RunAsync(CancellationToken cancellationToken = default);
        // owns try/catch/finally + bounded FlushFrameworkSerilog + StartupResult exit code
}

public abstract class FdwHttpHostBuilderBase<TSelf> : FdwHostBuilderBase<TSelf>
    where TSelf : FdwHttpHostBuilderBase<TSelf>
{
    public TSelf WithSwagger(Action<FdwSwaggerOptions> configure);  // Title/Version/Description; framework doc processors always included, fixed order
    public TSelf WithAppMiddleware(Action<IApplicationBuilder> configure);
        // ONE fixed slot (post-auth, pre-endpoints); at-most-once. See §6 for the escape-hatch contract.
}
```

**There is deliberately NO `WithHostedService<T>()`.** Hosted services are domain services; they are registered exclusively by their owning `[ServiceTypeOption].RegisterRequiredServices` and arrive by package reference + config row (doctrine #5). This resolves the tournament's double-registration contradiction in `Verify`'s favor (§9-C1). Likewise there is **no `WithBackgroundExecutor`** — ETL's pipeline background executor is registered by the Orchestration/EtlPipeline domain option with queue capacity from its config row.

```csharp
namespace Fdw.Hosting.Api;

public static class FdwApiHost
{
    public static FdwApiHostBuilder Create(string[] args, string applicationName,
        FdwConfigurationBootstrap bootstrap);
}

public sealed class FdwApiHostBuilder : FdwHttpHostBuilderBase<FdwApiHostBuilder>
{
    public FdwApiHostBuilder WithMultitenancy();
    public FdwApiHostBuilder WithScalar(Action<FdwScalarOptions> configure);
        // theming via Fdw.UI.Themes.Scalar; preview hosts from config section "Scalar:PreviewHosts";
        // scalar.DemoCredentialsFromConfig("Scalar:DemoCredentials") — values in config, never code.
}

namespace Fdw.Hosting.Worker;

public static class FdwWorkerHost
{
    public static FdwWorkerHostBuilder Create(string[] args, string applicationName,
        FdwConfigurationBootstrap bootstrap);
}

public sealed class FdwWorkerHostBuilder : FdwHttpHostBuilderBase<FdwWorkerHostBuilder> { }
    // nothing extra: hubs, executors, hosted jobs, gates all arrive via domain options + config rows

namespace Fdw.Hosting.Ui;

public static class FdwUiHost
{
    public static FdwUiHostBuilder Create(string[] args, string applicationName);
    // NO bootstrap parameter — the archetype physically cannot reach ConfigurationDb.
}

public sealed class FdwUiHostBuilder : FdwHostBuilderBase<FdwUiHostBuilder>
{
    public FdwUiHostBuilder WithRootComponent<TApp>() where TApp : IComponent;
        // MapRazorComponents<TApp>().AddAdditionalAssemblies(PageTypes.All()...).AddInteractiveServerRenderMode()
    public FdwUiHostBuilder WithTokenClient(string configurationSection);
        // cookie scheme "Blazor" + password/refresh grants via AddFdwBlazorTokenClient (IHttpClientFactory).
        // ALL keys required in the named section (ClientId, Scopes, ApiBaseUrl, RefreshSkew) — any missing
        // ⇒ HostLog.RequiredConfigurationMissing + exit 1. Section NAME is the explicit argument; no default.
    // RunAsync throws FdwHostCompositionException (exit 1) if WithRootComponent or WithTokenClient was never called.
}

namespace Fdw.Hosting.AuthServer;

public static class FdwAuthServerHostBuilderExtensions
{
    public static FdwApiHostBuilder AddTokenIssuer(this FdwApiHostBuilder builder); // co-hosted issuance
}

public static class FdwAuthServerHost
{
    public static FdwAuthServerHostBuilder Create(string[] args, string applicationName,
        FdwConfigurationBootstrap bootstrap); // standalone issuer: /connect/*, jwks, account endpoints only
}
```

```csharp
// Fdw.Hosting — downstream typed clients; token source is a config-row-selected TypeOption (§4)
public static class FdwDownstreamClientExtensions
{
    // AddHttpClient<TClient> with BaseAddress from a REQUIRED config section (missing ⇒ MessageLogging
    // error + failed registration), BearerTokenHandler, and IAccessTokenProvider resolved from
    // OutboundIdentityTypes.ByName(identityName) — NotFound sentinel ⇒ fail loud.
    public static IServiceCollection AddFdwDownstreamClient<TInterface, TClient>(
        this IServiceCollection services, string configurationSection, string identityName)
        where TClient : class, TInterface where TInterface : class;
}
```

Consumed by domain options (`ApiClientTypes` rows select client + identity per row), not by app code.

### 2.4 The sealed boot sequence (spine-owned; every step a `StartupResult.TryStep` with `HostLog` MessageLogging)

**Phase 1 (pre-Build):**
1. try/catch/finally shell; `finally` = `FlushFrameworkSerilog()` (bounded FDW-424 flush, everywhere — etl/sched unbounded-flush drift dies structurally).
2. `WebApplication.CreateBuilder(args)`. Version inject: read `FileVersionInfo`; if unresolvable ⇒ `HostLog.AssemblyVersionUnresolvable` **warning and the Serilog property is omitted** — no substitute string (the `?? "unknown"` fallback dies).
3. `AddFrameworkSerilog(applicationName)` → startup `ILoggerFactory`; `HostLog.HostStarting`.
4. `bootstrap.Apply(services)` → `AddConfigurationGateway<,>(path)`; `AddFrameworkOpenTelemetry`; `Lazy<IDataGateway>` singleton. (Ui archetype: this step does not exist.)
5. `PlatformServices.Configure(builder, lf)`; `PlatformServices.Register(builder.Services, lf)` — ordered by attribute-declared `Group`. Replaces all ~25 per-domain Configure/Register pairs.
6. Archetype services, compiled into the package (Api: `AddDistributedMemoryCache` → `AddMultitenancy` iff `WithMultitenancy` → `RealTimeHubs.Register` iff `RealTimeHubs.All()` non-empty (package reference IS the opt-in) → `AddFrameworkRateLimiting` (the real one — the empty-stub drift dies) → `AddFrameworkCors` → `AddFastEndpoints` → `AddHttpContextAccessor` → `SwaggerDocument` with framework doc processors in fixed order `DataSetQuery → ValuesFromSchema → PermissionFilter → app processors`).
7. Issuer wiring iff `AddTokenIssuer` (Api only).
8. Domain slot (`WithDomainServices`) — deliberately last: apps see, but cannot precede, framework registrations.
9. **Composition validation:** the archetype's declared `RequiredServiceCategories` must be ⊆ the categories in `PlatformServices.Entries()` (Api: `SecretManager`, `Connection`, `DataStore`, `AuthenticationService`, `Authorization`). Missing ⇒ `HostLog.RequiredServiceCategoryMissing` + exit 1 — a missing domain package is a loud composition error, not a runtime NRE.
10. `builder.Build()` — internal one-shot phase gate; a second call throws `FdwHostCompositionException` (defense in depth).

**Phase 2 (post-Build):**
11. `await PlatformServices.InitializeAllAsync(app.Services, lf, ct)` — non-success ⇒ exit 1.
12. `await PlatformServices.VerifyAllAsync(app.Services, lf, ct)` — domain-owned gates (e.g. `SchedulerTypes.Verify`: missing row / empty `DataStoreName|PathName|ScheduleContainerName` ⇒ MessageLogging error + failed result). Non-success ⇒ exit 1. The scheduler's duplicate sync-over-async DI-factory throw is **deleted**; this is the single validation.
13. Doc-processor `Initialize(app.Services)`.
14. App-owned `WithStartupGate` gates in registration order — non-success ⇒ exit 1.
15. Pipeline built — fixed order per archetype (§6).
16. `HostLog.HostStarted`; `await app.RunAsync(ct)`; exit code from `StartupResult.Complete(applicationName)`.

### 2.5 Post-migration Program.cs — complete, every line

**`reference-api/public/src/Reference.Api/Program.cs`**

```csharp
using Fdw.Hosting.Api;
using Fdw.Hosting.AuthServer;
using Fdw.Hosting.Core;
using Fdw.Services.Connections.MsSql;
using Fdw.Services.SecretManagers.EnvironmentVariable;
using Reference.Api.Middleware;
using Reference.Api.Options;

return await FdwApiHost
    .Create(args, "Reference.Api",
        FdwConfigurationBootstrap.File<MsSqlConnectionFactory, EnvironmentVariableSecretManager>(
            "configurationSchema.json"))
    .WithMultitenancy()
    .AddTokenIssuer() // Why: this API co-hosts OpenIddict issuance today; a standalone auth host later
                      // = delete this line + flip this app's row to OpenIddictValidation Mode: Remote (§3).
    .WithSwagger(doc =>
    {
        doc.Title = "Reference.Api";
        doc.Version = "v1";
        doc.Description = "FDW Reference API — canonical framework consumer.";
    })
    .WithScalar(scalar =>
    {
        scalar.UseThemeProvider();                                  // Fdw.UI.Themes.Scalar; preview hosts from "Scalar:PreviewHosts"
        scalar.DemoCredentialsFromConfig("Scalar:DemoCredentials"); // dev-env values live in appsettings, not code
    })
    .WithAppMiddleware(static app => app.UseMiddleware<EmptyBodyBadRequestMiddleware>())
    .WithDomainServices(ctx => ctx
        .Options<SupportOptions>("Support"))
    .RunAsync();
```

Everything else in today's 677 lines is accounted for: buckets A/B → spine/archetype; C → `AddTokenIssuer`; the `PipelineJobHttpClient`/`ScheduleHttpClient` + token providers → `ApiClientTypes` config rows with identity per row (`ForwardedUser`); theme/catalog one-off singletons → their owning domains' `RegisterRequiredServices` (theme load becomes async init — the `GetAwaiter().GetResult()` dies); NFL endpoints + RowSources → package reference + FastEndpoints scan + module initializers, zero lines, already correct.

**`reference-scheduler/public/src/Reference.Scheduler.Server/Program.cs`**

```csharp
using Fdw.Hosting.Core;
using Fdw.Hosting.Worker;
using Fdw.Services.Connections.MsSql;
using Fdw.Services.SecretManagers.EnvironmentVariable;

return await FdwWorkerHost
    .Create(args, "Reference.Scheduler.Server",
        FdwConfigurationBootstrap.File<MsSqlConnectionFactory, EnvironmentVariableSecretManager>(
            "configurationSchema.json"))
    .WithSwagger(doc =>
    {
        doc.Title = "Reference.Scheduler.Server";
        doc.Version = "v1";
        doc.Description = "Fdw Scheduler Server - Schedule management and evaluation.";
    })
    .RunAsync();
```

That is the whole file. `SchedulerBackgroundService`, `EtlDispatchService`, `IFrameworkSchedulingService` wiring, dispatch clients, and config binds live in `Fdw.Services.Scheduling`'s `[ServiceTypeOption].RegisterRequiredServices`/`Configure` (they were doctrine-#3 bugs in app code); the fail-loud gate is `SchedulerTypes.Verify`. `PreComputeCalculationsJob` + `CalculationApiClient` arrive from `Fdw.Calculations.PreCompute` by package reference + its config rows (`ClientCredentials` identity). `ICalculationUsageRepository` is renamed and re-homed during the move (no Repository pattern, ever). ETL's Program.cs is byte-identical in shape (name + swagger text differ); hubs and the background executor arrive via its package graph and config rows.

**`reference-ui/public/src/Reference.Ui/Program.cs`**

```csharp
using Fdw.Hosting.Core;
using Fdw.Hosting.Ui;
using Reference.Ui.Components;

return await FdwUiHost
    .Create(args, "Reference.Ui")
    .WithRootComponent<App>()
    .WithTokenClient("AuthClient")
    .RunAsync();
```

The UI stops being a hosting orphan: it gains `AddFrameworkSerilog`, the try/catch/exit-code/bounded-flush shell, `PlatformServices` sweep for its collections (`ApiClientTypes`, `HealthMonitorTypes`, `SessionStateTypes` — the raw `AddScoped<ISessionStateService, HttpSessionStateService>` deviation dies; a config row selects the Http option), `MapFrameworkHealthEndpoint`, and MonitoredHost participation. The 90-line cookie/ROPC block and login/logout endpoints now live in `Fdw.Web.Http.Authentication.Blazor`. The vestigial `public/configurationSchema.json` is deleted — structurally unreachable anyway.

---

## 3. Where the auth server (OpenIddict issuer) lives

**The mechanism splits now; the deployment stays in reference-api for now.**

Prerequisite: split the fused `OpenIddict` `[ServiceTypeOption]` into two options of `AuthenticationServiceTypes`:

- **`OpenIddictServer`** — issuance: `AddOpenIddict().AddCore(...).AddServer(...)`, `/connect/token` + jwks + revoke/introspect, `ConnectTokenEndpoint`, `OpenIddictSigningKeyLoader`/`Configurator`, `OpenIddictClientSecretProvisioner`, `ProcessSignInClaimsHandler`.
- **`OpenIddictValidation`** — resource side, with a **required, explicit `Mode`** on its config row: `LocalServer` | `Remote` (Remote additionally **requires** `Authority`). Missing `Mode`, or `Remote` without `Authority` ⇒ MessageLogging error + exit 1. **There is no auto-detection of a co-hosted server** — "validation silently chooses `UseLocalServer()` when the server option happens to be present" is exactly the banned "I'll figure out which" guesser (this replaces the marker-guard auto-detect idea from the losing design; see §9-G1).

Topologies, all config-selected via `AuthenticationServices` rows:
- **Today:** reference-api declares both rows (`OpenIddictServer`, `OpenIddictValidation` with `Mode: LocalServer`) + calls `.AddTokenIssuer()` → byte-for-byte current behavior. **etl and scheduler flip immediately to `OpenIddictValidation` with `Mode: Remote` + `Authority`** — they should never have been co-hosted issuers; this is a standalone security win that also live-proves the split, and it lands before any host-abstraction work (§7-P2).
- **Later, standalone issuer:** a new tiny app on `FdwAuthServerHost` (minimal surface: `/connect/*`, jwks, account endpoints, TokenSwitch, Scalar login-form processor); reference-api deletes `.AddTokenIssuer()` and flips its row to `OpenIddictValidation Mode: Remote`. Zero Program.cs surgery anywhere else.

**Row-placement decision (resolved, not papered over):** `AuthenticationServices` rows are bootstrap-critical yet live in appsettings.json. **Blessed:** appsettings.json is the documented surface for *process-topology rows* (`AuthenticationServices`, `MonitoredHosts`, `ApiClients`) — they select which options are active in **this process**, must be readable before ConfigurationDb is reachable, and vary per deployment slot. `configurationSchema.json` remains strictly the bootstrap-connectivity declaration (Connections/SecretManagers/DataStores needed to reach ConfigurationDb). This lands in the `configuration-overview` skill in M6.

---

## 4. Host self-registration (MonitoredHost) at startup

**Client side — a swept ServiceTypeCollection, zero spine special-cases, zero Program.cs lines** (grafted from *minimalist*; more doctrine-#5-pure than spine registration):

- `MonitoredHostTypes` `[ServiceTypeCollection(ServiceCategory = "MonitoredHost", Group = 10)]` with two options:
  - **`ApiRegistered`** (`Fdw.Services.HostRegistry.Client`) — HTTP POST to the central registry. Its `RegisterRequiredServices` adds `AddHostedService<MonitoredHostRegistrar>` + `AddHttpClient("HostRegistry")` (Microsoft infra inside the option — the established precedent).
  - **`LocalRegistered`** (`Fdw.Services.HostRegistry` server package) — direct in-process call, used by the host that hosts the registry itself. **This mechanically kills the API-registers-to-itself loopback bootstrap race** — no retry-hope needed.
- **Config row (appsettings, per §3's blessed surface) — the spoke declares WHAT it registers, WITH whom, AS whom:** `MonitoredHosts: [{ "Name": "EtlPrimary", "ServiceOptionType": "ApiRegistered", "RegistryBaseUrl": "<hub>", "IdentityName": "EtlMachine", "PublicBaseUrl": "<this spoke's reachable URL>", "HealthPath": "/health", "HealthCheckOnStartup": false, "HealthCheckIntervalSeconds": 300 }]`. PublicBaseUrl/HealthPath/cadence are the advertisement the hub persists as its `settings.MonitoredHost` row and probes on; a host cannot reliably self-derive its public URL behind proxies, so it is DECLARED. All keys required for the selected option; any missing ⇒ MessageLogging error + failed `Initialize` ⇒ exit 1. **No rows ⇒ the collection legitimately has zero instances** — no registrar is registered, the sweep logs Info. That is standard empty-collection semantics (nothing is substituted), not a fallback. Defaulting to any registry URL is explicitly rejected (§8-R10).
- **Identity — ONE seam, two consumers:** `OutboundIdentityTypes` `[ServiceTypeCollection]` in `Fdw.Services.Credentials`, options today: `ClientCredentials` (wraps the existing `IOutboundCredentialService`; secret via `SecretManagerName`) and `ForwardedUser` (user-delegated, for API→ETL/Scheduler call-through). Future options, each a new `[TypeOption]` in a new backend package with **zero changes to any host or the registrar**: `AzureManagedIdentity`, `AwsIrsa`, `GcpWorkloadIdentity`. The registrar resolves `OutboundIdentityTypes.ByName(row.IdentityName)` — compare against the `NotFound` sentinel, fail loud. The **same collection** feeds `AddFdwDownstreamClient` (§2.3) — the *builder* design's separate `AccessTokenProviderTypes` is collapsed into this single seam.
- **Timing:** `MonitoredHostRegistrar : IHostedService` starts on `IHostApplicationLifetime.ApplicationStarted` — after `InitializeAllAsync`, `VerifyAllAsync`, and gates, exactly when connections are resolvable, tokens obtainable, and `/health` is servable.
- **Payload:** `{ host, hostArchetype, instanceId/slot, checks[] }` where `hostArchetype` is a compile-time constant shipped by the archetype package ("Api" | "Worker" | "Ui" | "AuthServer") — machine-readable host-shape metadata in the registry. `checks[]` = per-connection health-check descriptors derived from the configured `ConnectionTypes` instances + `HealthMonitorTypes` rows.
- **Server side — a real package:** `Fdw.Services.HostRegistry` ships `POST api/v1/host-registry/register` and `POST api/v1/host-registry/deregister` FastEndpoints + storage, guarded by permission **`hostregistry.register`** (a seeded authz permission row granted to the service-host role; the databases-seed repo gains the permission + grant in the same cycle). Authorization is the token's baked perms — no host-side special-casing.
- **Failure policy:** registration failure is **not boot-fatal**. Rationale (documented in the package so nobody "hardens" it later): a fatal gate is circular — the registry's own host must boot, and every host including the API self-registers; and fail-loud governs missing *configuration*, not transient remote unavailability. Behavior: Error-level MessageLogging + bounded backoff retry + `"registered": false` surfaced in the `/health` payload; best-effort deregister on `ApplicationStopping`. **The keep-running policy itself requires user sign-off — see §10.**

---

## 5. Relationship to PlatformServices: driven, not subsumed

`PlatformServices` remains the aggregate three-phase engine and the **census** — its module-initializer discovery is the sole statement of "this host has domain X" (package reference = intent). The archetype spine is the **sequencer** — the only production caller, invoking it at fixed boot steps (§2.4 steps 5, 9, 11, 12):

- Spine step 5 drives `Configure` + `Register` (group-ordered from attribute-declared `Group` — the framework owns the *ordering mechanism*, each domain owns its *layer value*; no spine-side table, `SetGroup` deleted).
- Spine step 9 validates archetype `RequiredServiceCategories` against `PlatformServices.Entries()`.
- Spine steps 11–12 drive `InitializeAllAsync` then `VerifyAllAsync`, both returning `IGenericResult`, both exit-1 on failure.

Neither owns the other: `PlatformServices` knows *what* is present; the archetype knows *when* each phase runs and what the pipeline looks like. The generated per-category dot-walk properties stay for tests/tooling/diagnostics. The two visible `PlatformServices` phases from the *minimalist* design are intentionally **not** visible in Program.cs here — the spine is framework code with its own integration tests; hiding the sweep inside a *tested, versioned package* is not the "hidden magic" risk that hiding it inside copy-pasted app code was.

---

## 6. Middleware ordering ownership + the escape-hatch story

**Ownership: the archetype package owns the total order, compiled in. Apps physically cannot reorder** — no hook ever receives `WebApplication`; the one middleware slot receives `IApplicationBuilder` at exactly one position. Each archetype's sequence is covered by an integration test asserting the middleware order.

**Api/Worker pipeline (fixed):**
`UseForwardedHeaders(ForwardedHeadersDefaults.PreviewChain)` → `UseStatusCodePages` (401/403 `{errorCode, messages[]}` envelope) → `UseFrameworkApplicationPipeline(multitenancy)` → **[WithAppMiddleware slot]** → `UseFdwFastEndpoints` — extended to carry `RoutePrefix "api/v1"`, `RoleClaimType "roles"`, the flattened-error `ResponseBuilder`, and `PermissionClaimsPreProcessor` as framework constants, so Reference.Api's raw-`UseFastEndpoints` deviation is *impossible to reproduce* → `UseSwaggerGen` → `MapRealTimeHubs` iff `RealTimeHubs.All()` non-empty → Scalar map + `"/" → /scalar` iff `WithScalar` → `MapFrameworkHealthEndpoint(applicationName)`. (Worker = same spine, minus multitenancy/CORS/doc-processor extras/Scalar theming. The api-vs-etl hub-map-order disagreement is resolved once, in the package.)

**Ui pipeline (fixed):** `UseExceptionHandler("/Error")` (non-dev) → `UseSerilogRequestLogging` → `MapStaticAssets` → `UseForwardedHeaders` → `UseAuthentication` → `UseAuthorization` → `UseAntiforgery` → `MapRazorComponents<TApp>().AddAdditionalAssemblies(PageTypes.All()…).AddInteractiveServerRenderMode()` → `MapFdwBlazorAuthEndpoints()` → `MapFrameworkHealthEndpoint`.

**Escape hatches — explicit, fail-loud, no fallback-shaped defaults:**

| Vector | Contract |
|---|---|
| App middleware | Exactly **one** named slot (post-auth, pre-endpoints), at-most-once, `IApplicationBuilder` only. A pre-auth need has **deliberately no legal app-side position**: pre-auth ordering is security-relevant mechanism (forwarded headers, scheme, auth), so by definition a framework concern — the escalation path is a reviewed framework hook per the governance rule below, never a local hatch. |
| App registrations | `WithDomainServices(FdwDomainRegistrationContext)` — Kestrel/config-sources/logging structurally unreachable; typed `Options<T>`/`OptionsValidator<,>` cover legitimate app values. `ctx.Services` exists **only** for mid-migration residue. |
| **Analyzer FDW022** | Flags `AddSingleton`/`AddScoped`/`AddTransient`/`AddHostedService` of non-`Microsoft.Extensions.Options` types in entry-point app code (including inside the domain slot). Every hit is a doctrine-#3 bug surfaced at build. **Prerequisite — lands before any app ports (§7-P6).** |
| **Analyzer FDW023** | Enforces exactly one `Fdw.Hosting.{Api,Worker,Ui,AuthServer}` package reference per entry-point app. |
| Composition errors | Every `With*` validates eagerly; duplicate non-repeatable calls throw `FdwHostCompositionException`; `RunAsync` verifies required calls (Ui: root component + token client); required-category validation exits 1; every boot step is a `StartupResult.TryStep` with `HostLog` MessageLogging. |
| Feature knobs | Capabilities are **code presence** (a builder call or a package reference) — never config booleans that can silently remove auth or rate limiting (the deleted `FeatureOptions` failure mode stays dead). Values (titles, URLs, client ids, rows) are config. |
| **Hook governance rule** (codified in the `Fdw.Hosting.*` package docs) | A new `With*`/slot exists only when its **pipeline position matters** AND **≥2 consumers need it**. Single-app quirks use the domain slot or the middleware slot. Hook additions are reviewed, versioned FDW API changes — anything inexpressible is a missing FDW feature: file the issue, add the option. |

There are **no** `Action<WebApplicationBuilder>` / `Action<WebApplication>` hooks, ever (§8-R5) — that lambda is the vector by which 677-line Program.cs regrows.

---

## 7. Migration path (ordering only)

**Prerequisites (FDW repo, branch off `feature/platform-services-generator`; worktrees with the same branch name across every affected repo, per protocol):**
- **P1** — `Fdw.Services.Registration` mechanism: `Group` attribute arg (generator-emitted; delete `SetGroup`), async `Initialize` → `Task<IGenericResult>`, `Verify` member, `InitializeAllAsync`/`VerifyAllAsync`, `Entries()`; conforming descriptors for `DataStoreProvider`/`DataSetProvider`; fold `ConnectionTypes.RegisterAdditionalInterfaces` into `Register`.
- **P2** — OpenIddict option split with required explicit `Mode`; then **flip etl + scheduler `AuthenticationServices` rows to `OpenIddictValidation Mode: Remote`** — standalone security win, deployable ahead of all host work, live-proves the split.
- **P3** — `OutboundIdentityTypes` (`ClientCredentials`, `ForwardedUser`) in `Fdw.Services.Credentials`; `AddFdwDownstreamClient`; migrate `PipelineJobHttpClient`/`ScheduleHttpClient`/`CalculationApiClient` to `ApiClientTypes` config rows.
- **P4** — D-bucket relocations: scheduler mechanism → `Fdw.Services.Scheduling` (incl. `SchedulerTypes.Verify`, delete the DI-factory throw); PreCompute job + client → `Fdw.Calculations.PreCompute`; Scalar theming → `Fdw.UI.Themes.Scalar` (preview hosts → config); UI cookie/ROPC + login/logout → `Fdw.Web.Http.Authentication.Blazor`; theme/catalog one-off singletons → owning options; finish sec/data/notify `RegisterDomainServices` migration; unify `AddFrameworkOperations` → `OperationsTypes`.
- **P5** — `Fdw.Hosting.Core` split (shell/Serilog/health/forwarded-headers/StartupResult/`HostLog`); delete dead surface (`IFdwHost*`, `FdwHostOptions`, `FeatureOptions`, unconsumed catalogs).
- **P6** — Analyzers **FDW022** + **FDW023** (FDW022 gates every app port).
- **P7** — Archetype packages (`Fdw.Hosting.Api`/`Worker`/`Ui`/`AuthServer`) + spine; fold api's FastEndpoints config into `UseFdwFastEndpoints`; per-archetype middleware-sequence integration tests.

**Host rollout — smallest-risk-first (scheduler-first, replacing the winning design's api-first order). Each port: replace Program.cs, delete stale appsettings sections, run the parity test, then verify via the `preview-slot-deploy` skill:**
- **M1** — **reference-scheduler** → `FdwWorkerHost`. Proves spine, `Verify` gate, group-ordered sweep. **Parity test:** assert DI-container descriptor-set equality and middleware-order equality against the pre-migration app in a startup test — mechanical proof the archetype reproduces today's composition before drift fixes are layered on. Prove the USGS scheduled pipeline still fires end-to-end.
- **M2** — **reference-etl** → `FdwWorkerHost`. Proves hub auto-sweep + background-executor-by-domain-option on the same archetype. Parity test.
- **M3** — **reference-api** → `FdwApiHost` + `.AddTokenIssuer()`. Richest surface: multitenancy, doc processors, Scalar feature, issuance. Parity test; Newman suite green against a preview slot.
- **M4** — **reference-ui** → `FdwUiHost`. Gains shell/exit-codes/bounded-flush/health; delete vestigial `public/configurationSchema.json`, dead `isDevelopment`, raw `HttpClient`, raw `AddScoped<ISessionStateService,…>`. bUnit + Playwright green.
- **M5** — `Fdw.Services.HostRegistry` (server + client + `LocalRegistered`), `MonitoredHosts` rows on all four hosts, `hostregistry.register` permission + grant seeded (databases-seed, same branch name); verify registrations appear in the registry. *(Blocked on §10 sign-off.)*
- **M6** — Delete-list sweep: stale appsettings sections (`ControlDb`, `Authentication:Jwt`, `InternalApi:ApiKey`), dead usings, app copies of relocated code (`AuthAndTagDocumentProcessor`, both stale API-62 comments); mark `FdwDomainRegistrationContext.Services` `[Obsolete]`. **Docs land on the same branches:** `configuration-overview` (row-placement blessing, §3), `service-domain-patterns`, `auth-patterns`, `create-fdw-package`, showcase, dev-guide hosting chapter, and a new memory topic `architecture_hosting.md`.

---

## 8. Explicitly REJECTED

- **R1 — Single/central app (incl. modular monolith and one-binary role-from-config).** All values no mechanism; kills failure/scale/key-custody isolation and the per-process cached/cacheless gateway posture; module-init sweep forces every domain into every process; role-from-config is runtime dispatch on host type — the exact branching FDW bans above the connection layer.
- **R2 — `HostTypes` TypeCollection of archetypes** (`FdwHost.RunAsync(ApiHost.Instance, …)`). Archetypes differ by compile-time package graph; a collection package would force Blazor refs onto workers and OpenIddict onto UIs. Doctrine #4 governs *runtime* extensibility; host shape is build-time. Archetype = package. (The collection buys only metadata; the archetype-name constant in the registration payload delivers that payoff without the ceremony.)
- **R3 — Reviving `IFdwHost`/`IFdwHostBuilder`.** Interface-first hosting with zero second implementation earns nothing and invites app-side host implementations. Deleted; builders are sealed concretes.
- **R4 — Config-driven feature flags** (`FeatureOptions.Authorization = false`). A config file must never be able to silently remove security middleware — the fallback pattern applied to the pipeline. Deleted with the dead abstractions.
- **R5 — Generic `Action<WebApplicationBuilder>` / `Action<WebApplication>` escape hooks.** Program.cs with extra steps; the soup, verbatim. Only named, typed, position-fixed slots.
- **R6 — Auto-detecting co-hosted issuance** (validation choosing `UseLocalServer()` iff the server option is present — including via the marker guard). A banned "I'll figure out which" guesser. `Mode` is explicit required config; missing ⇒ exit 1.
- **R7 — `Fdw.Hosting.Scheduler` (or distinct Etl/Scheduler archetypes).** Worker is the shape; scheduler/etl are domains arriving via package refs + rows. A domain-named host package inevitably accretes domain values; archetype-per-app recreates the drift problem one level up.
- **R8 — `WithHostedService<T>()` / `WithBackgroundExecutor` on the builders.** Hosted services and executors are domain services owned by `[ServiceTypeOption].RegisterRequiredServices` (doctrine #5). An app-side registration path would be a second, competing mechanism — the tournament's own double-registration contradiction proved the hazard.
- **R9 — `dotnet new` templates as the fix.** Templates copy code; copies drift — precisely today's disease (the copy-paste fossils prove the lineage). Templates may emit the ~10-line Program.cs; mechanism lives in packages.
- **R10 — Auto-enabling self-registration / defaulting a registry URL when no row exists.** No rows = feature off with an Info log; substituting a default endpoint is a textbook forbidden fallback.
- **R11 — Fatal self-registration boot gate.** Circular (the registry's own host must boot; every host self-registers) and conflates transient remote unavailability with missing configuration. Documented in the package so nobody "hardens" it later.
- **R12 — Priority-integer middleware registries / TypeOptions contributing pipeline middleware.** The pipeline is a closed total order with adjacency constraints, not an open extensible set; priority-soup is how the hub-map disagreement happened. Collections may own *sweeps* (`MapRealTimeHubs`); total order has one compiled owner.
- **R13 — Lint-the-four-Program.cs approach.** Polices the symptom; the drift class survives. Case-(a): fix the mechanism.

---

## 9. Judge criticisms — explicit resolutions

**Criticisms of the winning (builder) design:**

| # | Criticism (judge) | Resolution in this document |
|---|---|---|
| C1 | §5 contradiction: `WithHostedService<SchedulerBackgroundService>()` in Program.cs while the domain option also registers it — double-registration; app-side hosted services violate doctrine #5 (J1, J2, J3) | **Resolved in the domain option's favor: `WithHostedService` is deleted from the API entirely (§2.3, R8).** Hosted services/executors are registered only by `RegisterRequiredServices`; scheduler/etl/PreCompute Program.cs lines are gone (§2.5). |
| C2 | Spine hardcodes core-chain group values via `SetGroup` — framework sequencer owning per-domain ordering values (J1, J2, J3) | **Grafted from typecollection/minimalist: `Group` is a `[ServiceTypeCollection(Group = n)]` attribute argument, generator-emitted, declared by each domain on itself; `SetGroup` deleted** (§2.2) — one mechanism, no override side-channel. |
| C3 | Parallel app-facing registration surface (WithDomainServices + ctx.Services, WithStartupGate, WithAppMiddleware) competes with three-phase doctrine (J1) | Surface shrunk to the minimum that migration honesty requires: hosted services removed (C1); domain gates moved to descriptor `Verify` (C4); `ctx.Services` is migration-only, FDW022-policed, `[Obsolete]` after M6 with a scheduled deletion; the middleware slot is one fixed position that cannot reorder anything. What remains is not a *registration* mechanism — it is typed Options binding plus one middleware position. |
| C4 | Startup gates as app-side lambdas keep domain validation in app code (J1, J3) | **Grafted from typecollection: `Verify` on `IServiceTypeCollection`, swept by `PlatformServices.VerifyAllAsync`** — `SchedulerTypes.Verify` is the single scheduler gate; the DI-factory throw is deleted. `WithStartupGate` survives only for genuinely app-owned gates (app-owned Options sections), expected rare, reviewable. |
| C5 | Exactly one `WithAppMiddleware` slot; pre-auth needs have no legal position (J3) | **Deliberate and now documented** (§6): pre-auth position is security-relevant mechanism = framework concern by definition; escalation path is a reviewed framework hook under the governance rule (position matters + ≥2 consumers). No second slot until real consumers demonstrate need. This is the no-fallbacks discipline applied to pipeline shape. |
| C6 | Fluent `With*` knobs are code toggles beside the doctrinal seams (J1) | Accepted with stated rationale (§6): capabilities are code presence (builder call or package ref), values are config — the inverse (config booleans) is R4's silently-disable-auth failure mode. Where package-reference intent suffices (hubs, executors, jobs), there is **no** builder call — presence is the reference. Builder-call surface is governed, eagerly validated, and non-repeatable. |
| C7 | Four archetype builders with overlapping `With*` surfaces invite framework-internal copy-drift (J2) | **`FdwHostBuilderBase<TSelf>` / `FdwHttpHostBuilderBase<TSelf>` CRTP bases** carry every shared slot exactly once (§2.3); archetype builders add only their archetype-specific methods. |
| C8 | `Fdw.Hosting.Ui` "refs Fdw.Hosting (Serilog/health only)" hand-waves ~30 transitive domain refs (J2) | **Grafted from minimalist: the `Fdw.Hosting.Core` split** (§2.1). `Fdw.Hosting.Ui` references Core only; the claim is now structurally true. |
| C9 | M1 ports reference-api first — highest-risk-first (J2) | **Grafted from typecollection: scheduler-first order** (M1 scheduler → M2 etl → M3 api → M4 ui, §7) — proves Worker + Verify + sweep cheaply before the richest surface. |
| C10 | FDW022 is load-bearing but doesn't exist yet (J3) | **FDW022 (and FDW023) are prerequisite P6, gating every app port** (§7). |
| C11 | More packages/surface than minimalist for the same middleware drift-kill (J3) | Accepted: the extra packages are exactly where the ordering compiles and where drift becomes a framework diff instead of a four-app diff; Core split keeps the dependency cost contained. The minimalist alternative leaves phase ordering as app ceremony — the documented drift breeding ground (its own §9 defense fails for the init-gate lines). |

**Criticized flaws of the losing designs, avoided by construction in the grafts:**

| # | Flaw | How this design avoids importing it |
|---|---|---|
| G1 | typecollection's `UseLocalServer()` marker-guard auto-detect (a banned guesser — J1, J2, J3) | Not grafted. Explicit required `Mode` config (§3, R6). |
| G2 | typecollection's framework-constant `"configurationSchema.json"` filename (J1) | Not grafted. `FdwConfigurationBootstrap.File<,>(path)` makes the filename an explicit required argument (§2.3). |
| G3 | typecollection's kernel-registered `HostRegistrationService` (spine special-case — J1) | Not grafted. `MonitoredHostTypes` swept option registers the registrar (§4). |
| G4 | typecollection's `UiHost.Instance.WithRoot<App>()` state-on-a-TypeOption strain (J3) | Moot — no TypeOption archetypes; `WithRootComponent<TApp>()` is builder state (§2.3). |
| G5 | typecollection's sealed RunAsync-only kernel (least testable — J1, J3) | `BuildAsync` is a first-class `WebApplicationFactory` seam on every builder (§2.3). |
| G6 | minimalist's app-owned phase ordering / open `WebApplicationBuilder` (drift survives — J2, J3) | Spine owns phasing; no hook receives the raw builder or app (§2.4, §6). |
| G7 | minimalist's permanently app-owned services (`PreComputeCalculationsJob`, doc processor, Scalar docs — J1) | All relocated to FDW packages in P4 (§2.1, §7). |

Other grafts adopted without a corresponding criticism: single `HostLog` MessageLogging class (kills `ProgramLog`/`StartupLog` drift); `RequiredServiceCategories` validation against `PlatformServices.Entries()`; one-shot throwing `Build()` phase gate; version-inject = warning + omit property; `Fdw.Services.HostRegistry` server package + `hostregistry.register` permission; archetype name in the registration payload; one `OutboundIdentityTypes` seam for both downstream clients and registration; DI-descriptor/middleware-order parity test; AuthenticationServices row-placement blessing; documented non-fatal-registration rationale.

---

## 10. RESOLVED — Host self-registration failure policy (signed off 2026-07-02)

**Decision: keep running, fail loud.** When a spoke boots healthy but cannot register with the
hub's registry (hub unreachable, permission denied): Error-level MessageLogging per attempt,
bounded backoff retry, `"registered": false` surfaced in the spoke's own `/health` payload,
best-effort deregister on shutdown. Boot stays up. Missing/malformed `MonitoredHosts`
CONFIGURATION remains fatal at Initialize (exit 1) — the policy covers transient remote
unavailability only, never absent configuration.

**Topology (owner decision): registration follows the DEPENDENCY GRAPH; a host never
registers with itself.** A host registers with each service that directly connects to it and
needs to monitor it — one `MonitoredHosts` row per target (the row shape in §4). Reference
deployment: etl registers with the API and the scheduler (both are its direct consumers);
scheduler and ui register with the API; nothing registers with the ui (no service connects
to it directly). Correspondingly, any service that RECEIVES registrations hosts the registry
(server package + `settings.MonitoredHost` container + `hostregistry.register` enforcement):
the API and the scheduler carry it; the ui carries only the client. Self-monitoring is each
host's own `/health` + health monitor — never a self-registration. This DELETES the
`LocalRegistered` option from §4: the loopback case it existed to solve cannot occur by
construction. `MonitoredHostTypes` ships with the single `ApiRegistered` option; future
transports are new options.

---
*Implementers: prerequisites P1–P7 land on branches off `feature/platform-services-generator` in the FractalDataWorks repo; app ports M1–M6 use worktrees with the same branch name across fractaldataworks, reference-scheduler, reference-etl, reference-api, reference-ui, databases-seed, and claude-tools (docs/skills), merged and pruned together per the multi-project worktree protocol. Every deploy verification goes through the `preview-slot-deploy` skill. All work requires a YouTrack issue (FDW project) before starting.*
