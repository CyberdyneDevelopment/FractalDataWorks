# UI Domain Map

This page maps every domain to its full FDW package stack — from API client contracts down to routed pages — and lists the pages each `*.UI.Pages` package contains.

For the generic object-type anatomy at each layer, see [13-05 UI Layer Anatomy](13-05-UI-Layer-Anatomy.md).

---

## Quick Reference: UI.Pages Packages

| Domain | `*.UI.Pages` Package | Routes |
|--------|----------------------|--------|
| Authentication | `Fdw.Services.Authentication.UI.Pages` | `/api-keys` |
| Authorization | `Fdw.Services.Authorization.UI.Pages` | `/roles`, `/roles/{id}`, `/users` |
| Calculations | `Fdw.Calculations.UI.Pages` | `/calculations`, `/calculations/{id}`, `/datasets/calculated/new` |
| Catalog | `Fdw.Services.Catalog.UI.Pages` | `/catalog`, `/glossary` |
| Configuration | `Fdw.Configuration.UI.Pages` | `/configuration`, `/configuration/issues` |
| Connections | `Fdw.Services.Connections.UI.Pages` | `/connections`, `/connections/new`, `/connections/{id}` |
| Data | `Fdw.Data.UI.Pages` | `/datastores`, `/datastores/{id}`, `/datastores/{id}/edit`, `/datasets`, `/datasets/{id}`, `/datasets/new`, `/data-preview`, `/mapper` |
| Messaging | `Fdw.Services.Messaging.UI.Pages` | `/messages`, `/messages/{id}`, `/access-requests`, `/access-requests/new`, `/settings/notifications` |
| Operations | `Fdw.Operations.UI.Pages` | `/audit`, `/dataflow`, `/health`, `/lineage`, `/promotions`, `/promotions/{id}/review` |
| Pipelines | `Fdw.Services.Pipelines.UI.Pages` | `/pipelines`, `/pipelines/builder`, `/pipelines/{id}/execution` |
| Quality | `Fdw.Services.Quality.UI.Pages` | `/quality`, `/quality/rules` |
| Schema | `Fdw.Schema.UI.Pages` | `/schema`, `/schema/tables/new` |
| Scheduling | `Fdw.Services.Scheduling.UI.Pages` | `/schedules`, `/schedules/new` |
| Secret Managers | `Fdw.Services.SecretManagers.UI.Pages` | `/secret-managers` |
| Terminal | `Fdw.Services.Terminal.UI.Pages` | `/terminal` |
| Agents | `Fdw.Agents.UI.Pages` | (see package contents) |
| ETL Projects | `Fdw.Services.Etl.Projects.UI.Pages` | (see package contents) |

---

## Per-Domain Package Stacks

### Authentication

| Layer | Package |
|-------|---------|
| Client contracts | `Fdw.Services.Authentication.Clients` |
| Headless providers | _(login flow handled by hosting extensions; no Components pkg)_ |
| UI components | _(login form in skin project)_ |
| **Routed pages** | **`Fdw.Services.Authentication.UI.Pages`** |

**Pages:**
- `ApiKeys.razor` — `@page "/api-keys"` — personal API key management

---

### Authorization

| Layer | Package |
|-------|---------|
| Client contracts | `Fdw.Services.Authorization.Clients` |
| Headless providers | `Fdw.UI.Components.Blazor` (RoleProvider, UserProvider) |
| UI components | _(inline in pages)_ |
| **Routed pages** | **`Fdw.Services.Authorization.UI.Pages`** |

**Pages:**
- `Roles.razor` — `@page "/roles"` — role listing
- `RoleDetail.razor` — `@page "/roles/{id:guid}"` — permission matrix editor
- `Users.razor` — `@page "/users"` — user management, role assignment

---

### Calculations

| Layer | Package |
|-------|---------|
| Client contracts | `Fdw.Web.Calculations.Clients` |
| Headless providers | `Fdw.Calculations.Components` (CalculationProvider) |
| UI components | _(inline in pages)_ |
| **Routed pages** | **`Fdw.Calculations.UI.Pages`** |

**Pages:**
- `Calculations.razor` — `@page "/calculations"` — formula library
- `CalculationEditor.razor` — `@page "/calculations/{id:guid}"` — formula editor
- `CalculatedDesigner.razor` — `@page "/datasets/calculated/new"` — calculated dataset designer

---

### Catalog

| Layer | Package |
|-------|---------|
| Client contracts | `Fdw.Services.Catalog.Clients` |
| Headless providers | `Fdw.Services.Catalog.Components` |
| UI components | _(inline in pages)_ |
| **Routed pages** | **`Fdw.Services.Catalog.UI.Pages`** |

**Pages:**
- `Catalog.razor` — `@page "/catalog"` — data asset catalog browser
- `Glossary/Index.razor` — `@page "/glossary"` — business glossary

---

### Configuration

| Layer | Package |
|-------|---------|
| Client contracts | _(config read via `ConfigurationApiClient` in Web.Clients.Abstractions)_ |
| Headless providers | `Fdw.UI.Components.Blazor` (ConfigurationProvider) |
| UI components | **`Fdw.Configuration.UI.Components`** (ConfigurationPageProvider, ConfigurationPageContext) |
| **Routed pages** | **`Fdw.Configuration.UI.Pages`** |

**Pages:**
- `Configuration.razor` — `@page "/configuration"` — system configuration viewer/editor
- `ConfigurationIssues.razor` — `@page "/configuration/issues"` — configuration validation issues

---

### Connections

| Layer | Package |
|-------|---------|
| Client contracts | `Fdw.Services.Connections.Clients` |
| Headless providers | `Fdw.UI.Components.Blazor` (ConnectionProvider) |
| UI components | **`Fdw.Services.Connections.UI.Components`** (ConnectionList) |
| **Routed pages** | **`Fdw.Services.Connections.UI.Pages`** |

**Pages:**
- `Connections.razor` — `@page "/connections"` — connection listing and status
- `ConnectionWizard.razor` — `@page "/connections/new"` — new connection setup wizard
- `ConnectionEditor.razor` — `@page "/connections/{id:guid}"` — connection edit form

---

### Data (DataStores + DataSets + Preview + Mapper)

| Layer | Package |
|-------|---------|
| Client contracts | `Fdw.Services.Data.Clients` |
| Headless providers | `Fdw.Data.Components` (DataSetWizardProvider, DataMapperProvider, DataPreviewProvider) |
| UI components | **`Fdw.Data.UI.Components`** (DataPreviewPageProvider, DataPreviewPageContext, QueryPanel, PreviewPanel) |
| **Routed pages** | **`Fdw.Data.UI.Pages`** |

**Pages:**
- `DataStores.razor` — `@page "/datastores"` — physical storage locations
- `DataStoreDetail.razor` — `@page "/datastores/{id:guid}"` — datastore detail/introspection
- `DataStoreEditor.razor` — `@page "/datastores/{id:guid}/edit"` — datastore edit form
- `DataSets.razor` — `@page "/datasets"` — logical data definitions
- `DataSetDetail.razor` — `@page "/datasets/{id:guid}"` — dataset detail, fields, sources
- `DataSetWizard.razor` — `@page "/datasets/new"` — new dataset setup wizard
- `DataPreview.razor` — `@page "/data-preview"` — interactive query and preview
- `Mapper.razor` — `@page "/mapper"` — schema field mapping designer

---

### Messaging

| Layer | Package |
|-------|---------|
| Client contracts | `Fdw.Services.Messaging.Clients` |
| Headless providers | `Fdw.UI.Components.Blazor` (MessageProvider) |
| UI components | _(inline in pages)_ |
| **Routed pages** | **`Fdw.Services.Messaging.UI.Pages`** |

**Pages:**
- `Messages.razor` — `@page "/messages"` — message inbox
- `MessageDetail.razor` — `@page "/messages/{id:guid}"` — message thread
- `AccessRequests.razor` — `@page "/access-requests"` — pending access requests
- `NewAccessRequest.razor` — `@page "/access-requests/new"` — create access request
- `NotificationSettings.razor` — `@page "/settings/notifications"` — notification preferences

---

### Operations

| Layer | Package |
|-------|---------|
| Client contracts | `Fdw.Web.Analytics.Clients`, `Fdw.Operations.Clients` |
| Headless providers | `Fdw.Operations.Components` (AuditProvider, ExecutionProvider, DataflowProvider, LineageProvider) |
| UI components | `Fdw.Web.Analytics.Components` (Health.Dashboard, Health.Gauge, Health.Sparkline, Health.Throughput, Promotions, PromotionReview) |
| **Routed pages** | **`Fdw.Operations.UI.Pages`** |

**Pages:**
- `Audit.razor` — `@page "/audit"` — execution audit log
- `Dataflow.razor` — `@page "/dataflow"` — ETL execution monitoring graph
- `HealthDashboard.razor` — `@page "/health"` — system health overview
- `Lineage.razor` — `@page "/lineage"` — data lineage traversal graph
- `Promotions/Index.razor` — `@page "/promotions"` — promotion request queue
- `Promotions/Review.razor` — `@page "/promotions/{id:guid}/review"` — promotion review/approval

---

### Pipelines

| Layer | Package |
|-------|---------|
| Client contracts | `Fdw.Services.Pipelines.Clients`, `Fdw.UI.Pipelines.Clients` |
| Headless providers | `Fdw.Services.Pipelines.Components` |
| UI components | _(inline in pages)_ |
| **Routed pages** | **`Fdw.Services.Pipelines.UI.Pages`** |

**Pages:**
- `Pipelines/Index.razor` — `@page "/pipelines"` — pipeline listing and run history
- `Pipelines/Builder.razor` — `@page "/pipelines/builder"` — visual pipeline designer
- `Pipelines/ExecutionDetail.razor` — `@page "/pipelines/{id:guid}/execution"` — execution detail and logs

---

### Quality

| Layer | Package |
|-------|---------|
| Client contracts | `Fdw.Services.Quality.Clients` |
| Headless providers | `Fdw.Services.Quality.Components` |
| UI components | _(inline in pages)_ |
| **Routed pages** | **`Fdw.Services.Quality.UI.Pages`** |

**Pages:**
- `Quality/Dashboard.razor` — `@page "/quality"` — data quality score dashboard
- `Quality/Rules.razor` — `@page "/quality/rules"` — quality rule management

---

### Schema

| Layer | Package |
|-------|---------|
| Client contracts | `Fdw.Schema.Clients` |
| Headless providers | `Fdw.Schema.Components` (SchemaExplorerProvider, TableWizardProvider) |
| UI components | _(inline in pages)_ |
| **Routed pages** | **`Fdw.Schema.UI.Pages`** |

**Pages:**
- `SchemaExplorer.razor` — `@page "/schema"` — schema browser and discovery
- `TableWizard.razor` — `@page "/schema/tables/new"` — new table creator

---

### Scheduling

| Layer | Package |
|-------|---------|
| Client contracts | `Fdw.Services.Scheduling.Clients` |
| Headless providers | `Fdw.UI.Components.Blazor` (ScheduleProvider) |
| UI components | _(inline in pages)_ |
| **Routed pages** | **`Fdw.Services.Scheduling.UI.Pages`** |

**Pages:**
- `Schedules/Index.razor` — `@page "/schedules"` — schedule listing and status
- `Schedules/New.razor` — `@page "/schedules/new"` — schedule creation form

---

### Secret Managers

| Layer | Package |
|-------|---------|
| Client contracts | `Fdw.Services.SecretManagers.Clients` |
| Headless providers | _(configuration-layer concern)_ |
| UI components | _(inline in pages)_ |
| **Routed pages** | **`Fdw.Services.SecretManagers.UI.Pages`** |

**Pages:**
- `SecretManagers.razor` — `@page "/secret-managers"` — secret manager configuration

---

### Terminal

| Layer | Package |
|-------|---------|
| Client contracts | _(WebSocket / raw HTTP)_ |
| Headless providers | _(browser JS interop, inline)_ |
| UI components | _(inline in page)_ |
| **Routed pages** | **`Fdw.Services.Terminal.UI.Pages`** |

**Pages:**
- `Terminal.razor` — `@page "/terminal"` — web terminal with xterm.js

---

## Pages That Stay in the Skin (Not Extracted)

These pages are application-specific and remain in the skin project (`reference-ui`, etc.):

| Page | Reason |
|------|--------|
| `Home.razor` / Dashboard | Aggregates data from multiple domains; skin-specific layout |
| `Login.razor` | Authentication flow is skin-specific (redirect URIs, cookie policy) |
| `Profile.razor` | User profile is app-specific |
| `Settings.razor` | General settings aggregation page |
| `Appearance.razor` | Theme/appearance is skin-specific |
| `StatCard.razor` | Presentational component with no domain logic |
| `AgentActions*` | Agent-integration feature, reference-ui specific |

---

## See Also

- [13-05 UI Layer Anatomy](13-05-UI-Layer-Anatomy.md) — object types at each layer (generic, non-domain-specific)
- [13-01 Headless UI Pattern](13-01-Headless-UI-Pattern.md) — provider structural contract and hard rules
- [16-01 API Clients Reference](16-01-API-Clients-Reference.md) — all `*.Clients` packages
