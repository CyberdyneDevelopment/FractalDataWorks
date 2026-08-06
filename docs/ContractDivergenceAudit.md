# Client/Server Contract Divergence Audit

The 38 request/response names declared in both `Fdw.Web.Api` and a client package whose
**field sets genuinely differ**. Per decision D1 these were audited, not changed: the two-layer
design is deliberate and `Fdw.ContractParity.Tests` documents the reasons.

`JUSTIFIED` = the difference is explained by route-binding or a client-side routing discriminator.
`REVIEW` = no such explanation found in the shape alone; worth a human look for accidental drift.

**9 justified / 29 to review.**

| Type | Client package | Server-only fields | Client-only fields | Verdict | Basis |
|---|---|---|---|---|---|
| `AddDataStoreContainerRequest` | Services.Data.Clients.Abstractions | `Fields`, `Format`, `Name` | — | **REVIEW** | — |
| `AssignRoleRequest` | Services.Authorization.Abstractions | `UserId` | — | **JUSTIFIED** | server-only fields are route-bound identifiers |
| `CatalogSearchRequest` | Services.Catalog.Clients.Abstractions | `EntityTypes`, `Tags` | `EntityType` | **REVIEW** | — |
| `CreateDataSetRequest` | Services.Data.Clients.Abstractions | `Aggregates`, `FederationStrategy`, `KeyFields`, `SourceDataSetName`, `TransformExpression` | — | **REVIEW** | — |
| `CreateDataSetSourceRequest` | Services.Data.Clients.Abstractions | `SourceDataSetId`, `SourceKind` | `FieldMappings` | **REVIEW** | — |
| `CreatePromotionRequest` | Web.Analytics.Clients.Abstractions | `Items`, `Notes`, `RequestedBy` | — | **REVIEW** | — |
| `CreateQualityRuleRequest` | Services.Quality.Abstractions | `FieldName`, `IsEnabled`, `MaxValue`, `MinValue`, `Pattern`, `Severity` | — | **REVIEW** | — |
| `DataPreviewRequest` | Services.Data.Clients.Abstractions | `Columns` | `Filters`, `Page`, `PageSize` | **REVIEW** | — |
| `DataPreviewResponse` | Services.Data.Clients.Abstractions | `Messages`, `RowCount`, `Source` | `TotalRowCount` | **REVIEW** | — |
| `DataSetDetailDto` | Services.Data.Clients.Abstractions | `Aggregates`, `FederationStrategy`, `SourceDataSetName`, `TransformExpression` | `Id`, `KeyFields`, `Name` | **REVIEW** | — |
| `DataSetFieldDto` | Services.Data.Clients.Abstractions | `IsNullable`, `TypeName` | `DefaultValue`, `IsIndexed` | **REVIEW** | — |
| `DataSetSourceDto` | Services.Data.Clients.Abstractions | `ContainerId`, `IsActive`, `PathName`, `SourceDataSetId`, `SourceDataSetName`, `SourceKind` | `FieldMappings`, `FileFormat`, `HttpMethod`, `MapperTypeName`, `Path`, `SupportsPredicatePushdown` | **REVIEW** | — |
| `DataSetSourceRecord` | Operations.Endpoints | — | `SourceDataSetId`, `SourceDataSetName` | **REVIEW** | — |
| `DataSetSummaryDto` | Services.Data.Clients.Abstractions | — | `Name`, `ServiceOptionType` | **JUSTIFIED** | client-only field is a routing discriminator |
| `DataStoreContainerDto` | Services.Data.Clients.Abstractions | — | `PhysicalName`, `SupportedOperations` | **REVIEW** | — |
| `DataStoreDetailDto` | Services.Data.Clients.Abstractions | `ConnectionId` | `ETag`, `Id`, `Name` | **REVIEW** | — |
| `DataStoreFieldDto` | Services.Data.Clients.Abstractions | `Description`, `FrameworkDataType` | `MaxLength`, `Precision`, `Scale` | **REVIEW** | — |
| `DataStorePathDto` | Services.Data.Clients.Abstractions | `Path` | `PhysicalPath` | **REVIEW** | — |
| `DataStoreSummaryDto` | Services.Data.Clients.Abstractions | `ConnectionId`, `ContainerCount`, `StoreType` | `Id`, `Name` | **REVIEW** | — |
| `EnvironmentDto` | Operations.Abstractions | `Approvers`, `ConnectionName`, `Order`, `RequiresApproval` | — | **REVIEW** | — |
| `ExecuteDdlRequest` | Schema.Clients.Abstractions | `Columns`, `Name`, `SchemaName`, `TableName` | `ConnectionName`, `Ddl` | **REVIEW** | — |
| `GenerateDdlRequest` | Services.Connections.Abstractions | `Name` | — | **JUSTIFIED** | server-only fields are route-bound identifiers |
| `GetMeResponse` | Services.Authentication.Abstractions | `AvailableTenants`, `Id`, `TenantId` | `UserId` | **REVIEW** | — |
| `GlossaryTermDto` | Services.Catalog.Clients.Abstractions | `Name` | — | **JUSTIFIED** | server-only fields are route-bound identifiers |
| `ImportSchemaRequest` | Schema.Clients.Abstractions | `ConnectionName` | — | **JUSTIFIED** | server-only fields are route-bound identifiers |
| `PersonalAccessTokenSummary` | Services.Authentication.Abstractions | — | `IsRevoked` | **REVIEW** | — |
| `PipelineDetailDto` | UI.Pipelines.Clients.Abstractions | `DestinationConnectionName`, `DestinationDataSet`, `IsEnabled`, `PipelineType`, `SourceConnectionName`, `SourceDataSet`, `Transforms`, `UpdatedAt` | `Connections`, `Id`, `ModifiedAt`, `Name`, `Status`, `Tasks` | **REVIEW** | — |
| `PipelineSummaryDto` | UI.Pipelines.Clients.Abstractions | `PipelineType` | `CreatedAt`, `Description`, `ModifiedAt`, `Name`, `Status` | **REVIEW** | — |
| `PromotionDto` | Web.Analytics.Clients.Abstractions | `ApprovedAt`, `ApprovedBy`, `CompletedAt`, `CreatedAt`, `Items`, `Notes`, `RequestedBy` | — | **REVIEW** | — |
| `QualityCheckResultDto` | Services.Quality.Abstractions | `ErrorMessage`, `ExecutedAt`, `FailureCount`, `Status`, `TotalCount` | `Message`, `Passed`, `RuleName` | **REVIEW** | — |
| `SaveSourceMappingsRequest` | Schema.Clients.Abstractions | `Name` | `DataSetName` | **JUSTIFIED** | server-only fields are route-bound identifiers |
| `SchemaFieldDto` | Data.Components | `Ordinal` | `MaxLength`, `Precision`, `Scale` | **REVIEW** | — |
| `SetRolePermissionsRequest` | Services.Authorization.Abstractions | `Name` | — | **JUSTIFIED** | server-only fields are route-bound identifiers |
| `UpdateDataSetRequest` | Services.Data.Clients.Abstractions | `Aggregates`, `FederationStrategy`, `KeyFields`, `Name`, `SourceDataSetName`, `TransformExpression` | — | **REVIEW** | — |
| `UpdateQualityRuleRequest` | Services.Quality.Abstractions | `Id` | — | **JUSTIFIED** | server-only fields are route-bound identifiers |
| `UpdateRoleRequest` | Services.Authorization.Abstractions | `Name`, `SortOrder` | — | **REVIEW** | — |
| `UpdateUserPreferencesRequest` | Services.Notifications.Abstractions | `DarkMode`, `Language`, `ThemeName`, `Timezone` | `Preferences`, `UserId` | **REVIEW** | — |
| `UpdateUserRequest` | Services.Users.Abstractions | `Name` | — | **JUSTIFIED** | server-only fields are route-bound identifiers |

---

## Separate finding: EventId collisions inside TypeCode `ENDPOINTS3`

`Fdw.Operations.Endpoints` declares two MessageLogging classes that both carry
`[MessageLoggingTypeCode("ENDPOINTS3")]`, so they share one code namespace — and five EventIds
are used twice for unrelated events. The emitted `Code` is identical in each pair:

| Code | `ApiEndpointLog` | `OperationsEndpointLog` |
|---|---|---|
| `ENDPOINTS3-11016` | `PipelineAggregateComposed` | `ListingAgentActions` |
| `ENDPOINTS3-11017` | `PipelineLinkageExtracted` | `AgentActionsListed` |
| `ENDPOINTS3-11018` | `LineageEdgesCreated` | `GettingAgentAction` |
| `ENDPOINTS3-11019` | `PipelinesProjectedForLineage` | `ReviewingAgentAction` |
| `ENDPOINTS3-31003` | `PipelineAggregateComposeFailed` | `AgentActionNotFound` |

This **pre-dates** the contract work and was not introduced by it. It is left as-is deliberately:
resolving it means renumbering EventIds, which changes codes that dashboards or alerts may key on,
so it wants its own change with explicit sign-off rather than being folded into a DTO refactor.
