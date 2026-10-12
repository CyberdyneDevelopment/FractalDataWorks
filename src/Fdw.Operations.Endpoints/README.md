# Fdw.Operations.Endpoints

Endpoint bases for lineage, impact analysis and configuration metadata.

Endpoint base classes for this domain's HTTP surface. A host closes over a base with a sealed endpoint that supplies its route; the base supplies the validate → service → map → send shape.

An endpoint is an HTTP orchestrator. It does not open a gateway, and it does not carry business logic — anything a background job would also need belongs in the service.

## Endpoint bases

| Type | Kind | Purpose |
|---|---|---|
| `ApproveAgentActionEndpointBase` | class | Abstract endpoint that approves a pending agent action. |
| `CancelExecutionEndpoint` | class | Endpoint to cancel an execution. |
| `CreateEscalationPolicyEndpointBase` | class | Abstract endpoint that creates an escalation policy. |
| `DeleteEscalationPolicyEndpointBase` | class | Abstract endpoint that deletes an escalation policy. |
| `DenyAgentActionEndpointBase` | class | Abstract endpoint that denies a pending agent action. |
| `ExpandLineageNodeEndpointBase` | class | Abstract endpoint that expands a single lineage node, returning its direct (one-hop) upstream and… |
| `GetAgentActionEndpointBase` | class | Abstract endpoint that gets a single agent action by ID. |
| `GetAnalyticsEndpointBase` | class | Base endpoint for getting analytics summary for a time period. Route: GET /analytics |
| `GetChildConfigurationTypesEndpoint` | class | Tier 2 default endpoint to list child configuration types for a parent. |
| `GetConfigurationCategoriesEndpoint` | class | Tier 2 default endpoint to list all available configuration categories. |
| `GetConfigurationTypeDetailEndpoint` | class | Tier 2 default endpoint to get detailed configuration type information. |
| `GetConfigurationTypesByCategoryEndpoint` | class | Tier 2 default endpoint to list configuration types by category. |
| `GetDataSetLineageEndpoint` | class | Endpoint to get lineage for a specific DataSet. |
| `GetDataflowGraphEndpoint` | class | Endpoint to get the full dataflow graph. |
| `GetEscalationPolicyEndpointBase` | class | Abstract endpoint that gets an escalation policy by ID. |
| `GetExecutionChildrenEndpoint` | class | Endpoint to get children of an execution. |

## Request and response models

Endpoint-layer models are named `Request` / `Response`; the client layer names its equivalents `Payload`. The two layers are deliberately separate.

| Type | Kind | Purpose |
|---|---|---|
| `AgentActionIdRequest` | class | Request for getting, approving, or denying an agent action by ID. |
| `ConfigurationPropertyTypeBaseResponse` | class | Base class for configuration property type options. Uses CRTP pattern with TypeLookup on DataType for… |
| `DataSetLineageRequest` | class | Request for lineage of a specific DataSet. |
| `DataSetLineageResponse` | class | Lineage information for a DataSet. |
| `DataflowGraphResponse` | class | Response containing the full dataflow graph. |
| `EscalationLevelResponse` | class | DTO for an escalation level within a policy. |
| `EscalationPolicyIdRequest` | class | Request to identify an escalation policy by ID. |
| `EscalationPolicyRequest` | class | Request for creating an escalation policy. |
| `EscalationPolicyResponse` | class | DTO for an escalation policy. |
| `ExecutionIdRequest` | class | Request for getting an execution by ID. |
| `ExecutionStateRequest` | class | Request for state transition operations (cancel, pause, resume). |
| `ExecutionSummaryResponse` | class | Summary execution information for listing. |
| `ExpandLineageNodeRequest` | class | Request to expand a single lineage node, returning its direct upstream and downstream neighbors. Bound… |
| `FieldConsumerResponse` | class | A downstream consumer of a field: another dataset's field, a pipeline step, or a calculation that reads… |

## Installation

```bash
dotnet add package Fdw.Operations.Endpoints --prerelease
```

## Dependencies

`Fdw.Collections` · `Fdw.Commands.Data.Extensions` · `Fdw.Configuration` · `Fdw.Data.Abstractions` · `Fdw.Data.Lineage` · `Fdw.MessageLogging.Abstractions` · `Fdw.Operations.Abstractions` · `Fdw.Services.Abstractions` · `Fdw.Services.Agents.Abstractions` · `Fdw.Services.Audit.Abstractions` · `Fdw.Services.Data.Abstractions` · `Fdw.Services.Etl` · `Fdw.Services.Etl.Abstractions` · `Fdw.Services.Pipelines` · `Fdw.Web.Analytics.Clients.Abstractions` · `Fdw.Web.Endpoints` · `Fdw.Web.RestEndpoints`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.Data.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators` · `Fdw.Registration.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
