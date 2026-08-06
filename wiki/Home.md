# FractalDataWorks Wiki

Welcome to the FractalDataWorks documentation. This wiki guides you through building data-intensive applications using the FractalDataWorks framework.

## Getting Started

1. [Prerequisites](01-01-Prerequisites.md) - What you need to get started
2. [Quick Start](01-02-Quick-Start.md) - Build your first FractalDataWorks application
3. [What You Built](01-03-What-You-Built.md) - Understanding the generated solution
4. [Local Package Development](01-04-Local-Package-Development.md) - Working with local NuGet packages

## Solution Structure

5. [Project Layout](02-01-Project-Layout.md) - How projects are organized
6. [Directory.Build.props](02-02-Directory-Build-Props.md) - Shared build configuration
7. [Package Management](02-03-Package-Management.md) - Central package version management
8. [Naming Conventions](02-04-Naming-Conventions.md) - Project and namespace conventions

## Configuration

9. [ManagedConfiguration](03-01-ManagedConfiguration.md) - Database-persisted, source-generated configuration
10. [Configuration Writers](03-02-ConfigurationWriters.md) - CQRS write-side for configuration persistence
11. [Per-Category Reload](03-03-Per-Category-Configuration-Reload.md) - Targeted reload: only the affected ServiceCategory IOptionsMonitor fires
12. [Cache-Backed Providers](03-04-Cache-Backed-Providers.md) - DataSet/DataStore/Pipeline on-demand loading with per-entry invalidation
13. [JSON-Driven Configuration Startup](03-05-JSON-Driven-Configuration.md) - `configurationSchema.json` and the ConfigurationGateway (1.2.0)
14. [Configuration Provider Registration](03-05-Configuration-Provider-Registration-Pattern.md) - Three-phase Configure / Register / Initialize lifecycle
15. [Configuration Guide](03-06-Configuration-Guide.md) - Startup vs runtime vs app config — what goes where
16. [Polymorphic Configuration Pattern](03-07-Polymorphic-Configuration-Pattern.md) - Parent header + typed-body per variant

## TypeCollections

11. [Overview](04-01-Overview.md) - Source-generated enhanced enums
12. [Base Classes](04-02-Base-Classes.md) - Defining base classes
13. [Concrete Options](04-03-Concrete-Options.md) - Creating individual options
14. [Collection Declaration](04-04-Collection-Declaration.md) - The TypeCollection attribute
15. [Generated Lookups](04-05-Generated-Lookups.md) - O(1) access methods
16. [Dispatcher Pattern](04-06-Dispatcher-Pattern.md) - Type-safe dispatch without reflection
17. [Components Guide](04-07-Components-Guide.md) - All 6 TypeCollection variants explained
18. [Testing TypeCollections](04-08-Testing-TypeCollections.md) - Test option logic, not registration

## Data Access

19. [DataGateway Pattern](05-01-DataGateway-Pattern.md) - Unified data access layer
20. [DataSets](05-02-DataSets.md) - Logical data definitions
21. [DataNode Core Split](05-03-DataNode-Core-Split.md) - `Fdw.Data.Configuration` + `Fdw.Data.DataNodes` extraction; the connection-agnostic `IDataStore` tree the UI reuses (FDW-572)
22. [The Self-Similar Command Pipeline](05-04-Self-Similar-Command-Pipeline.md) - **Start here for the architecture thesis.** Commands → translators → connections → connectors, one shape at every layer
23. [Formats and Physical Addressing](05-05-Formats-And-Physical-Addressing.md) - JSON/XML/CSV are FORMATS, not connections; `CanonicalFileExtension`, container `Path`, POCO mapping
24. [A Config Source Is Just a Connection](05-06-Configuration-Source-Is-A-Connection.md) - The same provider read over MsSql and over a JSON folder (FDW-578) + the designed Composed Connection

## Service Domains

21. [Service Domains Overview](06-01-Service-Domains-Overview.md) - Plugin architecture for services
22. [Creating a Service Domain](06-02-Creating-Service-Domain.md) - Step-by-step implementation guide
23. [Connections Service Domain](06-03-Connections-Service-Domain.md) - Example: Connections pattern
24. [Transformations Service Domain](06-04-Transformations-Service-Domain.md) - Calculation, Aggregation, Pivot, Lookup
25. [Notifications Service Domain](06-05-Notifications-Service-Domain.md) - Webhook and Console channels
26. [Connection Configuration Guide](06-06-Connection-Configuration-Guide.md) - Connection configuration end-to-end
27. [DataVault Service Domain](06-07-DataVault-Service-Domain.md) - Restricted verify-only secret storage (passwords, PATs, agent keys)
28. [Domain Stack Pattern](06-10-Domain-Stack-Pattern.md) - Canonical six-layer FDW service domain layout

## Message Logging

24. [Overview](07-01-Overview.md) - Source-generated structured logging
25. [MessageLogging Attribute](07-02-MessageLogging-Attribute.md) - Defining message loggers
26. [Logger Classes](07-03-Logger-Classes.md) - Creating logger classes
27. [IGenericMessage](07-04-IGenericMessage.md) - Message interface
28. [Result Integration](07-05-Result-Integration.md) - Integrating with Results
29. [ResultCodes](07-06-ResultCodes.md) - Structured, type-safe error codes
30. [External Exception Handling](07-07-External-Exception-Handling.md) - TypeCollection-based error dispatch for external systems

## Schema & Database

30. [Schema Abstractions](08-01-Schema-Abstractions.md) - Unified schema definitions
31. [Database Schema](08-02-Database-Schema.md) - ConfigurationDb / AuthDb / OpsDb (one schema per service domain)
32. [Data Lineage](08-03-Data-Lineage.md) - LineageGraph traversal, cycle detection, impact analysis

## Connectors (FDW-403)

33. [Connectors](09-01-Connectors.md) - File / HTTP / Roslyn connector adapters with DataStore-backed addressing

## Advanced Patterns

34. [TypeCollection Patterns](10-TypeCollection-Patterns.md) - ServiceTypeCollection vs MutableTypeCollection

## Reference Solutions

34. [Reference Apps Architecture](10-01-ReferenceSolutions-Architecture.md) - Multi-solution enterprise pattern
35. [Building Authentication Service](10-03-Building-Authentication-Service.md) - SQL-backed authentication implementation

## Management UI

36. [Management UI Overview](11-01-Management-UI-Overview.md) - Reference UI implementations
37. [Blazor Hosting Models](11-02-Blazor-Hosting-Models.md) - InteractiveServer vs InteractiveAuto

The reference Management UI implementations live in separate repositories:

- `reference-ui` — Tailwind + InteractiveServer skin
- `reference-aui` — MudBlazor + InteractiveAuto skin

## Hosting & Deployment

38. [Creating a Server](12-01-Creating-A-Server.md) - Build an FDW server in ~20 lines with hosting extensions
39. [Deployment Guide](12-02-Deployment-Guide.md) - Docker, production checklist, environment variables
40. [Service Communication](12-03-Service-Communication.md) - API gateway, proxies, webhooks, resiliency
41. [Security Hardening](12-04-Security-Hardening.md) - OWASP, headers, CORS, DB isolation, JWT
42. [Authorization](12-05-Authorization.md) - RBAC permissions, roles, endpoint policies
43. [Validation](12-06-Validation.md) - FluentValidation, DataAnnotations, and configuration validation
44. [Scalar Theming](12-09-Scalar-Theming.md) - ToScalarCssBlock() bridge for tenant themes in API docs
45. [Secret Management](12-10-Secret-Management.md) - Secret managers, Azure Key Vault, environment variables, JWT key resolution
46. [Authentication Architecture](12-11-JWT-Authentication-Architecture.md) - OpenIddict token issuance, vault-backed credential flow, provider-swap pattern, client-side refresh coordination
47. [WebMCP](12-12-WebMCP.md) - Expose endpoints as AI agent tools via the W3C WebMCP standard
48. [OpenAPI Document Processors](12-14-OpenAPI-Document-Processors.md) - ValuesFrom enum dropdowns, per-dataset cloning, cascading parameters in Scalar
49. [OpsDb Configuration](12-13-OpsDb-Configuration.md) - Operations database setup
50. [Claim Types](12-15-Claim-Types.md) - The ClaimDefinitions TypeCollection; adding a new JWT claim
51. [Real-Time Hub Path](12-16-Realtime-Hub-Path.md) - RealTimeHubBase + RealTimeHubs collection; add a SignalR hub as one [TypeOption]

## API Endpoints

44. [API Endpoints](12-07-API-Endpoints.md) - Per-domain endpoint architecture (generic bases + thin closures)
45. [Customizing Endpoints](12-08-Customizing-Endpoints.md) - Thin closure pattern with concrete examples

## Web UI Architecture

46. [Headless UI Pattern](13-01-Headless-UI-Pattern.md) - Protocol components, API clients, and the headless rendering architecture
47. [Creating Consumer Packages](13-02-Creating-Consumer-Packages.md) - Building consumer endpoints from per-domain endpoint packages
48. [UI Layer Anatomy](13-05-UI-Layer-Anatomy.md) - Object types at each layer (*.Clients.Abstractions → *.Clients → *.Components → *.UI.Components → *.UI.Pages)
49. [CancellationToken Propagation](13-04-CancellationToken-Propagation.md) - Rules for forwarding cancellation through async chains
50. [UI Skin Assembly Discovery](13-06-UI-Skin-Assembly-Discovery.md) - How skin assemblies are discovered

## Code Conventions

48. [Code Conventions](13-03-Code-Conventions.md) - FDW analyzer conventions, naming rules, suppression guidelines
49. [Analyzer Catalog](13-07-Analyzer-Catalog.md) - Every FDW0xx / TC00x analyzer (id, what it enforces, severity)

## Building Servers (Tutorials)

49. [Building an API Server](14-01-Building-An-API-Server.md) - Step-by-step API server with hosting extensions
50. [Building an ETL Server](14-02-Building-An-ETL-Server.md) - Pipeline execution server tutorial
51. [Building a Scheduler Server](14-03-Building-A-Scheduler-Server.md) - Job scheduling server tutorial

## Building UI Projects (Tutorials)

52. [Building a Blazor Server UI](15-01-Building-A-Blazor-Server-UI.md) - InteractiveServer with Tailwind CSS
53. [Building a Blazor Auto UI](15-02-Building-A-Blazor-Auto-UI.md) - InteractiveAuto with MudBlazor

## API Clients & Protocols (Reference)

55. [API Clients Reference](16-01-API-Clients-Reference.md) - Complete reference for 16 per-domain .Clients packages
57. [UI Domain Map](16-03-UI-Domain-Map.md) - Per-domain package stacks and page inventory (all 15 domains)

## Testing

59. [Test Classification](18-01-Test-Classification.md) - Priority traits (P0-P3) and category traits for CI gating

## Build & Infrastructure

57. [Build Pipeline Guide](17-01-Build-Pipeline-Guide.md) - Framework build/pack pipeline
58. [Docker & Database Setup](17-02-Docker-And-Database-Setup.md) - SQL Server, Seq, dacpac deployment, schema logins

## Configuration Reference

60. [Configuration Prerequisites](20-01-Configuration-Prerequisites.md)
61. [Service Startup Order](20-02-Service-Startup-Order.md)
62. [Data Flow](20-03-Data-Flow.md)

---

