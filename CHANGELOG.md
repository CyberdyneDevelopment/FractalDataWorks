# Changelog

All notable changes to the FractalDataWorks framework will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Breaking

- The tag owners list is a service, `ITagOwners` (`Fdw.Services.Tags`): `TagOwners` where a host carries the tenant context and the organization and workspace providers, `UnavailableTagOwners` (result `TagOwnersUnavailable`, 61312) where it does not, so the ETL host and the scheduler start with the tag endpoints mapped. `ListTagOwnersEndpointBase` takes `(logger, ITagOwners)` in place of the tenant context and the two providers, and a host registers `ITagOwners` scoped (the tenant context is scoped), as `TagEndpoints` in reference-servicetypes does (FDW-4100).
- `TagOwnerKinds` (`Fdw.Services.Tags`) is no longer a static class of string constants: it is a TypeCollection in `Fdw.Services.Tags.Abstractions` (`Tenant`, `Organization`, `Workspace`; the names are unchanged, as `CK_Tag_OwnerKind` holds them), so `GET /type-collections/TagOwnerKinds/values` lists them. Code that compared `TagOwnerKinds.Tenant` to a string compares `TagOwnerKinds.Tenant.Name`. New: `GET /tags/owners` (`ListTagOwnersEndpointBase`, read policy `tags:read`) lists the owners a caller may give a tag to as `{ kind, id, name }`: the caller's tenant, then the organizations and workspaces the caller can read. A host names it with an endpoint option (FDW-4100, RUI-1199).
- A workflow trigger (`TriggerEndpointBase<TRequest>`, the `TriggerWorkflowEndpointBase` route) answers 400 `TriggerValidationFailed` when `CorrelationId` or `TriggerSource` is missing or blank (FDW-3990, part of FDW-3580). Before, it generated a GUID and `API:TriggerEndpoint`. A caller states its own. `StreamingRequest.ShouldCompress` and the error-code texts are unchanged for callers; `ValuesFromSchemaDocumentProcessor` throws when a type option has no value for the display property it was asked to list, where it listed the option's `Name`.
- The terminal UI states its theme and its icons (FDW-3978, part of FDW-3580). `TUIComponent<,>.Theme` and `TUIPropertyComponent<,>.Theme` are `required`. `TextPromptHelper.Prompt`, `NumericPromptHelper.Prompt`, `CollectionPromptHelper.PromptCollection`, `TypeCollectionPromptHelper.Prompt`, `PanelRenderer.RenderPanel` and `TableRenderer.RenderTable` take a non-null `TUIThemeConfiguration` as their last argument with no default (the text and numeric helpers also take `defaultValue` and `metadata` without defaults, and `RenderPanel` takes `title`), and read colours and borders from it. `TUIComponent.RenderHeader` takes the title. `SpectreRenderContext` throws on a null console instead of using `AnsiConsole.Console`. `IIconSet` has a new `NodeIcon(string nodeType)`; an `IconSetBase` subclass overrides it (`UnicodeIconSet` and `AsciiIconSet` do). `ThemeConfigurationMapper.ToDto` throws when a stored theme has no `TextOnPrimary` or `TextOnSecondary`. A page renderer shows a component's `Label` and a section's `Title` as stated; it no longer shows the `Id` in their place.
- Four option bases take fewer constructor parameters; an implementer overrides an abstract property instead:
  - `CommandTypeBase` (`Fdw.Commands.Abstractions`) no longer takes `category`. Override `CommandCategory` (`IGenericCommandCategory`).
  - `TranslatorTypeBase` (`Fdw.Commands.Abstractions`) no longer takes `sourceFormat`, `targetFormat` or `capabilities`. Override `SourceFormat` and `TargetFormat` (`IDataFormat`) and `Capabilities` (`TranslationCapabilities`).
  - `DevelopmentCommandBase` (`Fdw.Commands.Development.Abstractions`) is now `(string name, string description)`. Override `CommandCategory` (`IDevelopmentCommandCategory`).
  - `DataMapperBase<TSource, TTarget>` (`Fdw.Data.Abstractions`) is now `(string id, string name)`. Override `SourceConverter` (`TSource`) and `TargetConverter` (`TTarget`). `DefaultDataMapper` already does.

  Why: each collection's generated `NotFound` sentinel derives from the option base and calls its constructor with default arguments, in the collection's static initializer. These parameters could only be passed null. `DevelopmentCommandBase` rejected the null, so the first use of `DevelopmentCommands` threw at startup. An abstract property is stubbed by the sentinel instead.
- TC009 is reported by every collection kind: TypeCollection, MutableTypeCollection, TypeInstanceCollection, ServiceTypeCollection, MutableServiceTypeCollection and ServiceTypeInstanceCollection. Before, only TypeCollection reported it, and only for a collection with options in the same assembly; it now fires for a collection with no options too. TC009 is a warning, so a Release build that treats warnings as errors fails on it. It names a constructor parameter of the option base that the `NotFound` sentinel can only pass null to. The fix is a type the sentinel can build (a string, number, array, read-only collection, enum or struct), a nullable type, or an abstract member. A read-only collection interface (`IReadOnlyList<T>`, `IReadOnlyCollection<T>`, `IEnumerable<T>`, `IReadOnlyDictionary<K,V>`), an enum and a struct are no longer reported: the sentinel always built them non-null.
- `CreateGraphEndpointBase`, `UpdateGraphEndpointBase` and `ValidateGraphEndpointBase` (`Fdw.Services.Pipelines.Endpoints`) take an `IDataSetConfigurationProvider` as a new last constructor parameter. A class that derives from one passes it on: the host registers the data set provider already (`DescribeGraphEndpointBase` takes it). Why: saving or validating a graph now reads the destination's data set fields, to refuse a `UuidV7` value source on a column that is not a Guid field, naming the column.
- `IExecutionTracker` has a new member `Close(executionItemId, finalState, resultCode, resultMessage, cancellationToken)`, which ends an execution item in a terminal state other than Completed or Failed. A class that implements `IExecutionTracker` implements it (`Complete` can only end an item Completed or Failed). A pipeline run the ETL host stopped or lost now ends **Interrupted**, with the stage and action records under it, where it ended Failed with result code `ServerStopped` and its child records stayed Running; code that reads a cut-off run's state as Failed reads Interrupted.
- `IFlowReporter` (`Fdw.Services.Etl.Abstractions`) has a new member `DestinationCommitted(stage, commit, cancellationToken)`, which records what a load destination has committed to its target. A class that implements `IFlowReporter` implements it too; `ExecutionFlowReporter` writes the event `destination.committed`. `ComponentTracking` (`Fdw.Services.Etl.Tracking`) has three new last members, `RowsCommitted`, `TargetEmptied` and `RepeatsToSameResult`, all null for a component that is not a load destination or whose records hold no figure; a caller that constructs one passes them.
- `ILoadStepKind` (`Fdw.Services.Etl.Abstractions`) has a new member `RepeatsToSameResult`: whether running the step again over the same rows leaves the target as the first run did (true for Upsert, Update, Delete and Truncate; false for Insert, and for any kind that does not override `LoadStepKindBase.RepeatsToSameResult`). `IPipelineRunRequestStore` has a new member `CommittedLoads(run, cancellationToken)`, which reads what the destinations of a cut-off run had committed; a class that implements it implements it too. `DestinationCommit` has a fifth member, `RepeatsToSameResult`.
- `IFlowPauseGate` (`Fdw.Services.Etl.Abstractions`) has a new member `StoppedWithHost`: true when the run's token ended because the host is stopping and no person cancelled the run. A class that implements the gate implements it.
- `IScheduleImplementationConfiguration` (`Fdw.Services.Scheduling.Abstractions`) has a new required member `MissedFireRule`: the name of a `MissedFireRules` option, `Skip` or `FireOnce`. A schedule saved without it is refused naming the field, so every client that saves a schedule (the console's schedule form, scripts) sends it. `sched.ScheduleConfiguration.MissedFireRule` is `NOT NULL` with no default; existing rows are migrated to `Skip`, which is what they already did (`databases` `scripts/migrations/FDW-3788.md`). `IFrameworkSchedulingService` has a new member `TriggerCatchup(scheduleId, dueTime, cancellationToken)`, and `TriggerScheduleCommand` a constructor that takes the due time and a property `CatchupDueTime`; a class that implements the service implements it too.

### Changed

- The create and update schedule validators of FDW (`CreateScheduleRequestValidator`, `UpdateScheduleRequestValidator`) carry the `MissedFireRule` rule: required on create, one of the `MissedFireRules` names, refused naming the field; on update it is checked only when stated.
- The domain providers of the data store, data set, category and transformation domains (`DataStoreServiceTypes`) and the schedule domain (`SchedulerTypes`) attach their implementation providers after Build, in the collection's Initialization (FDW-3966), and return the first failed `Register` as a failed result. Before, the `Register` result was dropped inside a singleton factory, so a provider that could not be attached left the host running without it. The threshold types (FDW-3936) do the same.
- A cancelled run records itself as cancelled all the way down. The stage record of a flow the caller cancelled, and the action or round records under it, end `Cancelled` (through `IExecutionTracker.Close`) and not `Failed`; before, `ExecutionFlowReporter.StepCancelled` completed them as failed. `FlowExecutor` decides the caller's cancel first: the outcome is the cancelled failure and `FlowRunReport.FailedComponent` is null, so a component that failed because of the cancel is not named as the cause. `FlowStageRecords.Finish` takes a new `cancelled` parameter (internal). The `component.finished` event has a new key `cancelled`: true for a component that neither succeeded nor caused the failure. When a person cancels a run the ETL host closes every record still open under it as `Cancelled`, writes the run's row in `etl.PipelineExecution` as `Cancelled` with its end time, and states in the run's message the rows read, written and rejected and the stages and components that were stopped; a figure the run's records do not hold is left out of the message, not written as zero.
- A cut-off run is not started again when that would load rows twice. Before the ETL host starts a run again after a stop, it reads what the run's destinations had committed. A destination that committed rows and whose load neither empties its target first nor repeats to the same result (an Insert) leaves the run Interrupted, and the run's message names the destination and the count. Records that cannot be read leave the run Interrupted too. The setting `MaxPipelineRunRestarts` is applied first.
- A schedule's `TimeoutSeconds` is applied to the run it starts. `TriggerPipelineRequest` and `PipelineExecutionRequest` have a new member `TimeoutSeconds` (seconds, null when the caller states none, at most `PipelineExecutionRequest.MaxTimeoutSeconds`), kept with the stored request of a run. The ETL host starts a timer when a run starts running (the wait in the queue is not counted); when the time has passed it cancels the run through the same path as a person's cancel, and the run ends `Cancelled` with the result code `TimedOut` and the message "The run exceeded the N second timeout of schedule 'X'", with the rows moved and the stages stopped. A scheduled run whose schedule states no timeout is logged as having no time limit. Before this nothing read the value.
- A run that fails records the kind of its failure: one `run.failed` event on the run with `classified`, `category`, `code` and `transient`, read from the category the failure message's code names (`RunFailureClassification`). Transient is the category's own retryable flag (Dependency, Transient and GatewayTimeout are). A failure with no coded failure message is recorded as unclassified, not as transient. A run that ends in an unhandled exception, or whose run identity is refused, or whose watermark cannot be read or written, records one as well. Nothing reads the failure's text.
- A host shutdown closes every record under the run Interrupted. The flow reads `IFlowPauseGate.StoppedWithHost`: on a host stop it writes its events and leaves its stage, action and round records open, and the host's `InterruptRun` closes them Interrupted with the run. Before, a stage the flow had closed Cancelled stayed Cancelled under an Interrupted run. A person's cancel and a timeout still close the records Cancelled, and win over a host stop that follows: a run a person cancelled ends Cancelled and is not left Interrupted to be started again.

### Added
- A request refused because of the state of something it uses has its own result category (FDW-4005): `Refused`, category 12 (codes 120000-129999), 409, not retryable, and `IResultCategory.CountsTowardFailureThresholds` is false for it (and for Success); `ResultCategoryBase` takes the member as a new constructor argument after `isRetryable` (the sentinel constructor says true). `RunFailureClassification` now reads the category with `FailureCategory.Of` (the result's own code first) and has a new last member `Refused`, the negation of that member; `run.failed` carries `refused`. `PipelineRunOutcomeReader` does not count a refused run, and `PipelineFailureCounter` skips a run that does not count without ending the streak, so a refused run (a disabled connection) is neither counted nor a reset. A conflict raised during a run stays in category 4 and stays counted.
- A threshold field states the comparisons it accepts (FDW-4001): `ThresholdFieldOperators.For(valueType)` decides once, from the type of the value a field reports (numbers, times and durations: Equal, NotEqual, GreaterThan, GreaterThanOrEqual, LessThan, LessThanOrEqual; a boolean: Equal, NotEqual; a string: Equal, NotEqual, Contains, StartsWith, EndsWith), `ThresholdField.Operators` reads it, and `ThresholdDefinitions` refuses a stored condition whose operator its field does not accept (result code 20005, naming the field and the comparison; IsNull and IsNotNull are no longer accepted in a threshold rule).
- The quality threshold types take their own measure (FDW-3935): `QualityThresholdTypeBase.Measure(QualityMeasureInput)` returns the measure by field name over the rows of a data set (`QualityMeasures`: NotNull, Unique, InRange, MatchesPattern, InReferenceSet, DistinctCount, RowCount, NullPercentage; the rule logic of the quality rule evaluator, with the pattern match timeout stated by the caller). A measure that cannot be taken (no FieldName, no numeric limit, an invalid pattern, no rows to take a share of, and CustomExpression, which has no evaluator) fails with result code 20011 and is reported not run, never a pass.
- The quality rule kinds as threshold types (FDW-3935, `Fdw.Services.Quality.Thresholds`): `QualityNotNull`, `QualityUnique`, `QualityInRange`, `QualityMatchesPattern`, `QualityInReferenceSet`, `QualityDistinctCountInRange`, `QualityRowCountInRange`, `QualityNullPercentageBelow` and `QualityCustomExpression`, each on a data set, reporting the measure the rule takes (`rowCount`, `violations`, `distinctCount`, `nullPercentage`) and declaring the parameters that say how it is taken (`FieldName`, `MinValue`, `MaxValue`, `Pattern`, `AllowedValues`, `Expression`). A rule's limits are conditions on the measure. `Fdw.Services.Quality` references `Fdw.Operations.Abstractions`. The quality executor, the migration of stored rules and the retirement of the old tables follow.
- Threshold parameters (FDW-3935): a threshold type declares the parameters it takes (`IThreshold.Parameters`, `ThresholdParameterDeclaration`: name, value type, required, list), a stored threshold states them as `ThresholdParameter` child rows (`ThresholdParameterConfiguration`), and `ThresholdParameterReader` checks them against the declaration (an undeclared, missing required, mistyped, valueless or twice-stated single-valued parameter is refused with result code 20013) into `IThresholdDefinition.Parameters`. `ThresholdTypeBase.Parameters` is empty by default, so existing types need no change. `Threshold<T>` takes the parameters as a new constructor argument, before the rules. The hosts' `configurationSchema.json` must declare `workflow.ThresholdParameter`.
- The threshold base (`Fdw.Operations.Abstractions.Thresholds`, evaluator in `Fdw.Operations.Thresholds`). A threshold type (`ThresholdTypes`, `IThreshold`) names the subject it is attached to and the values that subject reports. `Threshold<T>` is an ordered list of `ThresholdRule`s: a criteria in the existing filter model (judged by the data layer's `InMemoryQuery.Matches` over the reported values, no new comparison code) and a consequence from `EscalationTypes` (`Alert` on an escalation policy, `Notify`, `StopRun`, `DisablePipeline`, `DisableConnection`), each an option with its handler; a handler acts through `IEscalationAlerts`, `IThresholdNotifier`, `IRunStopper`, `IPipelineDisabler` or `IConnectionDisabler`, and fails with a result code when the host registered none. `IThresholdEvaluator.Report(subject, values)` loads the subject's thresholds (`IThresholdSource`), fires the consequence of every matching rule in order, and answers which fired and which rules were not measured; a subject with no threshold is not judged, and a value that was not reported is never read as zero. `Fdw.Operations.Abstractions` now references `Fdw.Data.Abstractions` and `Fdw.Operations` references `Fdw.Services.Data`. Storage: a threshold is `ThresholdImplementationConfiguration` (domain row `workflow.Threshold`, whose Implementation is the threshold type's name, rules in `workflow.ThresholdRule`) read through `IThresholdConfigurationProvider`; it is attached to a subject by a row in the link table of the subject's kind (`ThresholdSubjectKinds`: Pipeline, Component, Connection, DataSet, Schedule), all with a `SubjectId` column. `ThresholdSubject` now carries the subject's `SubjectId`. `ThresholdSource` reads the links and the thresholds and refuses one that cannot be judged (`ThresholdDefinitions`: unknown type or field or consequence, an Alert without a policy, a Notify without rules, filter rows that do not make a criteria; a rule's criteria are `ThresholdFilterGroup` and `ThresholdFilterCondition` child rows read into the filter model by `ThresholdCriteriaReader`, and a Notify rule's notification rules are `ThresholdRuleNotification` rows, with no JSON column). `ThresholdNotifier` sends the Notify rules through the run-notification delivery. `DefaultOperationsServiceType` registers the providers, `IThresholdSource`, `IThresholdNotifier` and `IThresholdEvaluator`. New result codes 20003-20009. `IThresholdEvaluator` also judges a set the caller holds (`Judge`, for a layered resolution such as wait thresholds) and answers which rules of a consequence hold without firing (`Holds`, for a gate). A stored `FiresOnEntry` setting (no default) makes a threshold fire when its criteria start to hold and not again until they have stopped (`ThresholdEntryTracker`, one per host process). New consequence `HoldNewRuns` (a state the gate reads; firing it does nothing) and subject kind `Server` (links in `workflow.ServerThreshold`, SubjectId is the tenant). The hosts' `configurationSchema.json` must declare the nine new containers.
- The escalation alerts routes (base classes in `Fdw.Operations.Endpoints.Escalation`): `GET /escalation/alerts` (permission `escalation:read`; optional `state`, `open`, `policy`, `subjectKind`, `subjectName`, `skip`, `take`; newest first; answers `items`, `total`, `skip`, `take` and `refreshSeconds`, the last from the setting `EscalationAlertsRefreshSeconds`, null when not set), `POST /escalation/alerts/{id}/acknowledge` and `POST /escalation/alerts/{id}/resolve` (permission `escalation:acknowledge`; no body; the caller is who acted; 404 when the alert is not found for the caller's tenant, 409 when its state does not allow the action). The permissions are seeded for Admin in `databases-seed`.
- `EscalationAlerts` implements `IEscalationAlerts` over `ops.EscalationAlert`: `Open` finds the policy by name (a missing, disabled or level-less policy is a failure naming it), opens one alert for a threshold, answers the open one when the threshold fires again, and answers cooling down inside the policy's cooldown; `Advance` sends each level that has fallen due through the notification service the level names (recipients, subject, and the level's message template with the tokens `{Subject}`, `{Message}`, `{Level}`, `{Policy}`, `{SubjectKind}`, `{SubjectName}`, `{Threshold}`); `Acknowledge` and `Resolve` check the alert's state and the caller's tenant. A level is sent with the priority the new setting `EscalationNotificationPriority` states (seeded `High`); a level whose priority is not set is not sent, is not recorded as sent and is tried on the next pass. `EscalationBackgroundService`, run by the ETL host, advances open alerts every `EscalationAdvanceIntervalSeconds` (seeded 30); a host that does not state a positive number does not start the timer and says so. `Fdw.Operations` now references `Fdw.Services.Settings`.
- `IEscalationAlerts` (`Fdw.Operations.Abstractions.Escalation`): the one entry for the alerts escalation policies walk. A threshold that fires calls `Open` with the policy it names and the subject and body its notifications carry (`EscalationAlertRequest`); `Advance` sends the levels that have fallen due; a person calls `Acknowledge` or `Resolve`; `List` lists the alerts. `Open` answers `EscalationOpened` (opened, already open, or cooling down). Every kind of threshold uses it: a wait threshold, a pipeline's failure threshold, a component's input and output thresholds. The contract is written; its implementation follows.
- The timing rules of an escalation policy: `EscalationTimeline` says which level an open alert is due to reach (the previous level's time plus the level's own delay, levels in order, never past the policy's maximum, none for a policy that is not enabled) and whether a cooldown has passed. `ops.EscalationAlert` (OpsDb) is the table that keeps each alert's state. The evaluator, the service that advances alerts and the endpoints follow in the same story.
- A load destination reports what it has committed while it loads: `FlowLoadDestination` tells the run's records when it has emptied its target (a Replace load) and, after each committed batch, the rows committed so far, as `destination.committed` events (`destination`, `rowsCommitted`, `targetEmptied`, `wholeSet`) on its stage's record. A run that is stopped or lost still says how far its load got. `RunTrackingReader` puts the figures on the destination's `ComponentTracking`, taking the greatest total, and lists a destination that has events but no component event.
- The execution state `Interrupted` (`ExecutionStateTypes`): terminal, neither a success nor a failure, reachable from every open state. `MaxPipelineRunRestarts` is seeded to 1 in `databases-seed` (0 leaves a cut-off run Interrupted; the setting is still never assumed in code). A retry run records `ContinuesRun`, the execution id of the run it continues.
- The `MissedFireRules` collection with `Skip` and `FireOnce`: what the scheduler does with a cron fire time that passed while it was down. `Skip` drops the missed times and logs how many; `FireOnce` runs the schedule once when it starts, however many were missed, and the run is recorded as a Catchup that was due at the latest missed time. It applies to Cron schedules only. A schedule that never ran has missed nothing, and the scheduler logs that.

- `UuidV7` value source (`ValueSourceKinds`, `ValueSourceFactories`): a destination column takes a new UUIDv7 for each row written. An Insert load mints one for every row; an Upsert load fills it on inserted rows only, leaves updated rows' value alone and keeps the column out of the compare list; an Update load refuses it. Saving a graph that puts it on a column whose data set field is not a Guid is refused, naming the column. New seams: `IValueSourceFactory.RowValueProducer` (replaces `RowValue`; the load calls the producer once for each row), `IValueSourceFactory.InsertOnly`, `IValueSourceKind.RequiresGuidColumn`, `UuidV7ValueSource` and the T-SQL renderer `MsSqlUuidV7ValueRenderer`.

## [1.4.5] - 2026-11-01

### Breaking: source layout only

Every project folder moved into a domain folder. For a NuGet consumer only the version changes: package ids, assembly names, namespaces, public APIs and package dependencies are the same as 1.4.4. No source file changed, and an unmoved build of the same commit gave the same package ids and the same dependency list in every nuspec. Every package is re-released at 1.4.5 because `Directory.Build.props` and 430 of the 445 project files changed.

How the move was performed, so a consumer of the source can apply it to their own paths:

- Projects moved from `public/src/<Project>/` to `public/src/[<group>/]<domain>/<Project>/`, and test projects from `public/tests/<Project>/` to `public/tests/[<group>/]<domain>/<Project>/`. Test projects that span domains are in `public/tests/_cross/`. The 445 projects sit in 42 domains named by the services the code uses (`connections`, `pipelines`, `secretmanagers`, ...); small related domains share a group folder (`foundation`, `identity`, `secrets`, `observability`, `ai`, `tooling`). No project was renamed, merged or split.
- The move is a script, `public/tools/reorg/reorg_apply.py`, with its rules in `public/tools/reorg/domains.py`. Folders were moved with `git mv`, so every file that is not a build file is a pure rename with unchanged content: `git log --follow`, blame and line numbers carry across.
- Files whose content changed: relative paths in `.csproj`, `.props`, `.targets`, `.slnx` and `.proj` files and in the two scripts under `public/scripts`; the solution folders of `public/Fdw.DeveloperKit.slnx`, regenerated by `public/tools/reorg/regenerate_slnx.py`; two convention tests (`ConnectionSecretWritersGuardTests`, `PlatformConnectionLiteralTests`) that found `public/src` through one project's old path and now find it as the folder holding `Directory.Build.props` and `src/`; and project paths in the docs.
- The full map ships in the repo: `public/tools/reorg/moves.json` maps each project's old folder to its new one, and `public/tools/reorg/moves/fractaldataworks-files.tsv` lists every file with its change, old path, new path and project (`-projects.tsv` lists every project with its domain and group). Rewrite your own paths from it: a ProjectReference into this repo, a script, a CI path filter, a doc link, a `file:line` citation.
- A branch or fork with changes from before the move merges across it with git's rename detection, which `git merge` does by default.
- PDB source paths carry the new folders.
- Six dead `<None Include>` embeds of `Fdw.Collections.SourceGenerators.Registration`, a project that no longer exists, were removed. The published packages never held that file, so nothing changes.
- Each domain folder holds a `CLAUDE.md` describing the domain (its projects, seams, rules, tests and dependencies), in no package. They are written by `public/tools/reorg/gen_domain_guides.py`, and `public/.gitignore` un-ignores exactly those 42 files.

## [1.4.4] - 2026-10-30

Released with reference-servicetypes 1.4.4 and the reference servers and console 1.4.4. An application with its own configuration database needs the schema changes listed under Breaking applied before it takes this version. Test-only fixes are not listed, and each entry under Fixed, and each entry that begins Fixed:, describes the fault as it was before 1.4.4; 1.4.4 fixes it.

### Breaking

- The promotion API is removed: every `/promotion` route (environments, requests, approve, reject, compare and execute) answers 404, and the promotion tables in the quality schema, their configuration classes and providers, and the hosts' declarations for them are dropped.
- `PATCH /secret-managers/{name}` takes `{"configuration": {...}}`, the type's own settings, merges them over the stored ones and saves them. A setting the type does not take is refused by name. `Description` and `Environment` are gone from the request; they were never saved.
- `ConditionOperator` is no longer part of the notification rule create API. A rule's severity is one of those `GET /notifications/rules/severities` serves, checked when the rule is saved, with no default.
- `AuiMiddleware` builds the manifest for the signed-in user and answers 401 when nobody is signed in. The reference-aui demo host wires the middleware without authentication, so its manifests answer 401 once it moves to this build.
- The OpenAPI, OData and SQL Server schema importers now register in `SchemaImporters`. The OpenAPI importer refuses a field with no type or no converter (`OpenApiFieldTypeUnknown`) and imports a plain integer as Int64, where it used to import both as string. The OData importer refuses `$metadata` with no `EntityType`.
- `GET /secret-manager-types` requires the platform administrator and names each type's settings container. The reference API references all six secret manager Registration packages, including the new `FdwReferenceSecretManagers.HashiCorpVault.Registration`.
- `GET /search` and `POST /find` are removed from the API.
- Fixed: Four DELETE routes answer 204 for an id that does not exist (annotation by id, glossary term, quality rule, escalation policy).
- Pipeline runs are no longer one at a time across pipelines. The ETL server runs the runs of different pipelines at once, up to `MaxConcurrentPipelineRunsPerServer` and `MaxConcurrentPipelineRunsPerTenant`; runs that write the same table take turns, and a run whose writes cannot be told runs alone. Neither setting has a value in code: an environment whose configuration lacks either row refuses every pipeline run, naming the setting, so every environment needs both before it takes 1.4.4.
- Each pipeline states how many of its runs may run at once (`MaxParallelRuns`, at least 1) and what happens to a run triggered while that many are active (`OverflowBehavior`: `Queue`, the run waits its turn, or `Refuse`, the run ends as failed with reason `RunRefused`.
- A calculation whose DataSet input names a view (a data set made from another data set plus a saved filter, such as AfcQuarterbacks, AfcPlayers filtered to Position = QB) reads that view's rows with its saved filter, as the caller, up to the row cap (`MaxCalculatedFilterRows`), by the same path the `/data` routes use, with rows cut to the data set's declared fields.
- `POST /calculation-entities` and `PATCH /calculation-entities/{id}` answer 400 (ENDPOINTS-21539) for a configuration member the calculation kind does not declare. Formula declares `formulaBody`, `formulaLanguage` and `timeoutSeconds`; Windowed declares `targetField` and `windowFunction`.
- The ETL host's trigger queue size, 100 before, is now the server setting `MaxQueuedPipelineTriggers` (Int32, positive), read once when the host starts. It has no default in code: any host that runs the pipeline trigger queue and whose server settings lack the row does not start, and its log names the setting.
- A calculation whose input is a data set reads that data set's rows as the caller: the data store applies the caller's row-level security through the request's gateway, up to the calculated-filter row cap (the `MaxCalculatedFilterRows` setting for the caller's tenant and role, as `/data` resolves it).
- A pipeline that belongs to the global tenant now reads every tenant's rows, read only, except the system tenant's and rows marked Restricted. Before, the database's row filter left the global tenant out of a run, so such a pipeline over another tenant's data set was accepted and then failed at run time with "Dataset not found"; that reason now names the tenant cause.
- Fixed: PATCH /calculation-entities/{id} with part of a configuration resets the members left out to their defaults.
- `IDomainConfigurationProvider<T>` (Fdw.Services.Abstractions) gains `Update`, typed and untyped: the same write as `Save` except that the record must name an existing member, and it fails as not found (event 31061, UpdateTargetNotFound) when the member's row is gone, or when the record carries an Id that names no member.
- How often a host checks each configuration database for a reset is now the server setting `ResetWatchIntervalSeconds` (Int32, positive), read once when the host starts. It has no value in code: a host that registers settings and finds no positive row does not start, and its log names the setting (event 20988). databases-seed carries it with 10, so a reseeded environment has it.
- Removed with it: `POST /calculation-entities/validate-formula`, `ICalculationApiClient.ValidateFormula` and `GetFunctions`, the Formula step executor, the Fdw.Expressions package, the Formula editor client types and the console's FormulaTokenizer; CALCULATIONS-21024 and CALCULATIONS-21025 are retired.
- Pipeline wait thresholds are new configuration, and every configuration database a host of this version reads must carry it before the host starts: three tables, `pipe.WaitThresholdLevel` (one row per owner that states a level: the server, a pipeline or a schedule, with its mode Inherit, Own or None), `pipe.WaitThreshold` and `pipe.WaitThresholdRule`, declared in every host's `configurationSchema.json`, and the server's level row, which databases-seed writes with mode None (no alert, today's behaviour).
- A pipeline run that has not started (held for a delayed start, waiting its turn, or waiting for approval) is kept in the new operational table `etl.PipelineRunRequest` and survives an ETL host restart; its run values are stored as they are, because a run value is never a secret.
- Each kind of calculation input says which members it takes, and the type-collection values route lists them as its parameters: a DataSet input takes `dataSetName`; a Scalar input takes `scalarValueType` and `scalarSerializedValue`; a Container input takes `connectionName` and `containerPath`.
- `POST /calculations/execute` finds a data set name the way `/data` does, so the name of a view is found; an unknown name still answers 404. `ExecuteCalculationEndpointBase`'s protected constructor takes `IDataSetResolver` in place of `IConfigurationGateway`, so an endpoint that derives from it must pass the resolver (the reference `ExecuteCalculationEndpoint` does).
- A calculation create or update refuses an input that leaves out a member its kind requires, with 400 ENDPOINTS-21548 naming the input's alias, its kind and the member: a DataSet input needs `dataSetName`, a Container input `connectionName` and `containerPath`, a Scalar input `scalarValueType` and `scalarSerializedValue`, each named on its own.
- A Scalar input's value is checked against its type when a calculation is created or updated: a value that is not a value of its type is refused with 400 ENDPOINTS-21549, "Calculation input '<alias>' has scalar value '<value>', which is not a value of type '<type>'".
- A pipeline destination states what it writes, as a source does: `targetKind` File writes one file and Files writes one file per value of the destination's split field, each holding only that value's rows, with the required `truncateFirst` deciding whether an existing file is replaced or added to.
- A connection or secret manager that states no command timeout no longer gets 30 seconds from the code or the column: it takes the required server setting `DefaultCommandTimeoutSeconds`, and is not built where neither is present (SETTINGS-62030 to 62033 name it). The SQL Server, PostgreSQL and SQLite connection columns and the SQL Server and SQLite secret manager columns lose their `DEFAULT 30`. A host's bootstrap connections state their own `CommandTimeoutSeconds` in `configurationSchema.json`.
- A create that used to overwrite a member of the same name now refuses it with a conflict (SERVICES-40001, CreateTargetExists): CreateRole, CreateDataSetAnnotation, CreateQualityRule, CreateEscalationPolicy and BindExternalIdentity. A caller that relied on a create replacing the old member must use the update.

### Added

- The path setting of a container source and a container destination is declared as a data store path, so a client can offer the store's own paths.
- A person who belongs to more than one organization marks one as default, and a sign-in issues a token already acting in it.
- GET /secret-manager-types tells the console which settings can be emptied (canBeEmpty), by the same rule the PATCH null refusal uses.
- A person's dark mode is kept as light, dark or follow-my-computer, and a person who never chose reads as not chosen.
- A for-each step works through its list in batches: a batch size that is a pipeline default with a per-run override.
- A pipeline load target is picked from a type collection: a container, a data set, a file, or an array of files.
- A further run of the same pipeline waits its turn rather than being refused, up to the pipeline's own limit on parallel runs (`MaxParallelRuns`).
- The console shows which pipeline runs are running and which are queued, and why a queued run waits.
- A pipeline can delay a run triggered past its limit: start after a set delay, or at a set time.
- The list of file names of the Files kind may come from a variable in scope, such as the batch of a For each.

### Fixed

- Cancel, pause and resume only change a run's record: the running pipeline never sees them, so a cancelled run keeps running.
- OrchestrationExecutor reports a step as succeeded when no step executor is registered and the step did nothing.
- GET /configuration/types/detail answers no settings for any type, so a secret manager's settings form cannot be built.
- Secret managers: a failed list read answers an empty list, a description is taken and dropped, and no route reads a manager's settings back.
- Pipeline run history answers only success, so a cancelled run or one still going reads as failed.
- Which configuration members are secret is decided by a name pattern, so a name is masked and a Token is not.
- ETL project and node reads answer 500: container 'OrchestrationNode' declares no foreign key to 'OrchestrationNodeConfiguration'.
- POST /identities/{name}/verify with an audience answers a bare gateway 502 instead of 200 with succeeded:false.
- Calculation execute answers 200 for an unknown calculation type and for a windowed calculation that failed.
- The request validators of the fdw endpoint base assemblies are never registered, so their rules are inert (calculation execute accepts an unknown type).
- PATCH /secret-managers/{name} answers 200 for a setting sent as null and keeps the old value.
- A data set's note cannot be deleted from what the API answers (delete takes an id no read gives), and "resolve" saves the note unchanged.
- POST /catalog/annotations/{AnnotationId}/resolve saves the note unchanged and answers it as resolved.
- Endpoints tell a not-found failure apart by searching its message for "not found".
- Deleting a secret manager is still allowed while notifications, authentication flows, data vaults, client credentials, token managers, hosts and Aegis commands name it.
- After a stage is renamed, the project read lists the stage twice: the old version and the new one.
- A save of a record carrying member A's Id under member B's name is written into B without a word.
- GET /users/me/preferences answers the empty shape for a person who has stored preferences.
- A calculation entity never receives the rows of its DataSet input, so Windowed and Formula entities run over zero rows and answer success.
- POST /calculation-entities/get answers no inputs, outputDataSetName or resultFieldName, and a failed read as 404.
- Creating or updating a calculation drops a Scalar input's value and accepts an unknown input kind.
- Two overlapping runs of a pipeline that has no watermark yet: the second to finish fails with WatermarkWriteFailed after its load committed.
- POST /calculation-entities/get does not return the calculation's configuration, so what create or PATCH set cannot be read back.
- POST /calculations/windowed answers partitionCount 0 and rowCount 0 beside a resultJson that holds the real counts.
- ProjectExecutionQueue and OrchestrationNodeExecutionQueue drop a trigger past their capacity and report it queued.
- The refusal ENDPOINTS-21532 for a type change without a configuration answers the generic "The request was invalid".
- The unreadable-configuration refusal ENDPOINTS-21531 shows System.Text.Json internals to the caller.
- PATCH /calculation-entities/{id} with a different calculationEntityType answers 200 and changes nothing.
- Through the API, a trigger a wait threshold stops answers a bare 503 and a refused pipeline delete a generic 409, without the reason, the threshold or its owner.
- GET /type-collections/ComparisonOperators/values and /AlertSeverities/values answer 404, so no wait threshold can be given a comparison or severity from the console.
- The 503 a stopped trigger answers through the API carries no executionId, so the stopped run's record cannot be read from it.
- A pipeline run cut off by its connection's command timeout fails with "SQL query timeout", not naming the connection or the seconds; a failure with no coded message now says so and lists what it carried.
- A pipeline run that was running when the ETL host stopped is still marked Running after the host starts again.
- deleting a pipeline or schedule that has its own wait thresholds, or that another owner's threshold counts, makes the queue refuse every run.
- The no-input-rows failure tells a person to give a calculation a Scalar input, which cannot help.
- A stop-new-runs threshold saved while its condition already holds lets one more run through.
- A pipeline Update load into SQL Server or PostgreSQL finds no field to update, because the update translators read a record's properties and the load sends a dictionary.
- An Update with no filter whose record does not carry the primary key builds UPDATE with no WHERE clause in the SQL Server, PostgreSQL and SQLite translators.
- Fixed: The SQL Server secret manager's CommandTimeoutSeconds limits none of its lookups: its commands run through the gateway with the data store connection's timeout.
- A data command that carries its own command timeout and expands into simpler commands runs its steps without that timeout, and is not refused.

## [1.0.0-rc.1] - 2026-07-02 — Initial release

This is the first public release of FractalDataWorks (FDW): a .NET framework for building
data-centric services (APIs, ETL pipelines, schedulers, and UIs) on a small set of consistent
mechanisms — a fail-loud result type, extensible-enum TypeCollections, a single data-access
gateway, database-backed configuration, and a render-agnostic UI model. The sections below
describe the framework **as shipped**, not as a diff from an internal pre-release.

### Added

**Results & messaging**
- `IGenericResult` / `IGenericResult<T>` — the universal return type for anything that can fail.
  Methods never return `null` and never throw for expected failure; they return a result carrying
  a `ResultCode`, a message chain, and root-cause tracking (`IsSuccess`/`Value`/`Status`/`CodeChain`/`RootCause`/`IsEmpty`)
- `IGenericMessage` / `GenericMessage` — structured message type (`Message`/`Code`/`Source`/severity),
  produced by the MessageLogging generator rather than hand-built
- Categorized `ResultCode` catalog — every code's number doubles as its `Id` and `EventId`; the
  leading digit selects one of 11 handling categories via the closed `ResultCategories`
  TypeCollection, including `ForbiddenCategory` (403) and `GatewayTimeoutCategory` (504) alongside
  Success/Validation/Auth/Conflict/Dependency/Transient/Configuration/Missing/Internal
- `[MessageLogging]` source generator — `[MessageLoggingTypeCode("PREFIX")]` partial classes with
  `[MessageLogging]` partial methods that both log and return an `IGenericMessage`; raw `ILogger`
  calls are disallowed by analyzer (FDW003)
- `logger ?? NullLogger<T>.Instance` as the one sanctioned fallback pattern in the codebase

**TypeCollections, source generators & analyzers**
- TypeCollections — the framework's extensible-enum mechanism: `[TypeCollection]` families of
  `[TypeOption]` members looked up by `ById`/`ByName`/`All`, auto-registered at module load.
  Failed lookups return a `NotFound` sentinel, never `null`
- ServiceTypeCollections — `[ServiceTypeCollection]` plugin families driving a three-phase DI
  lifecycle (`Configure → Register → Initialize` on the collection; `RegisterRequiredServices` /
  `RegisterFactory` per option). This is the only service-registration mechanism in FDW — no
  ad-hoc `AddXxx` extensions
- Source generators covering configuration DDL + validators, POCO row↔object mappers
  (`[GenerateMapper]`), MessageLogging, TypeCollection/ServiceTypeCollection scaffolding, and
  cross-assembly registration
- FDW001–FDW023 Roslyn analyzer family enforcing the no-fallback/no-raw-logging/TypeCollection/
  ServiceType conventions at build time

**Data access (DataGateway)**
- `IDataGateway` — the only data-access path in the framework. Addressing is supplied per call via
  `DataStoreTarget(DataStore, Path?, Container)` or `DataSetTarget(DataSet)`; commands never carry
  raw connection strings or schema/table strings
- Command set: Query / Insert / BulkInsert / Update / Delete / Find / Truncate, plus DDL commands
  (CreateTable/AlterTable/Index/Schema/View and drops), all reached through `IDataGateway.Execute`
- Gateway-native transactions (`BeginTransaction`) — a scope's calls run on one physical connection
  in one native transaction, with fail-loud behavior when a backend doesn't support transactions
- Streaming cursor (`OpenRecordSource`) for backends that support it, with fallback to materialized
  execution when they don't
- Caching built into the gateway itself — a tag-invalidated result cache gated by configuration,
  with a per-call `useCache:false` escape hatch; no cache decorators or per-provider caches
- Per-connection limit enforcement (rate, concurrency, result size, and query/byte budgets)
- `IConfigurationGateway` — the same gateway model specialized for reading/writing ConfigurationDb

**Connections, SQL dialects & data motion**
- Six connection backends: MsSql, PostgreSql, Sqlite, Http, FileSystem, and RoslynWorkspace, each
  exposing capability marker interfaces so callers never branch on connection type
- A shared SQL translator (`SqlDataCommandTranslatorBase`) plus a per-backend `ISqlDialect`
  (TSql / PlPgSql / Sqlite) — adding a new SQL backend means adding a dialect and a driver, not a
  new translator
- DataSets — a `DataSetTypes` strategy dispatch (Simple / Compound / Federated) so a named DataSet
  resolves to the right execution strategy at runtime
- Pipeline and transform infrastructure: ETL pipeline steps, field-level data transformers, and
  DI-managed transformation services, plus pipeline/task type collections for Extract/Transform/
  Load/Validate/Branch/Notify style orchestration
- Data lineage node/edge/graph model with execution integration

**Configuration**
- `[ManagedConfiguration]` — a class-level attribute that drives DDL generation, validation, and
  UI form metadata for configuration POCOs
- Parent-header + typed-body pattern for polymorphic configuration (e.g. a Connection header with
  one typed body per connection kind), so runtime fields land on the correct table
- One configuration-provider mechanism (`DefaultConfigurationProvider<TConfig,TCommand>`) composing
  reads (`Get(name)/Get(id)/Get()`) and cascading writes (`Save`) with version-on-write and
  tag-based cache invalidation
- App-shipped `configurationSchema.json` — each entry-point app declares the connections,
  secret managers, and data stores it needs to reach its configuration store

**Service domains**
- Connections and SecretManagers (MsSql, Sqlite, Azure Key Vault, environment variable, user secrets)
- Authentication via OpenIddict (OAuth2/OIDC authorization server, RS256 tokens, permission claims
  baked at issuance, client-credentials support)
- Authorization — org-scoped role/permission model enforced through a permission requirement/handler
  pair, with an effective-permission resolver
- Multitenancy — two-level Tenant→Org isolation with tenant-aware connection routing and cache keys
- Health monitoring — a `HealthMonitorTypes` collection with a local health monitor implementation
  and provider
- Scheduling (Cron/Interval/Once/Manual triggers) driving ETL dispatch from a background loop
- Execution/Operations tracking with a validated state machine and escalation policies
- Users/Credentials, DataVault, Quality, Workflows, Notifications (Console/Email/Webhook/System),
  Messaging, Session state, Settings, Audit, Agents, and Resiliency (Polly-backed retry/failover)

**Hosting & UI**
- Hosting middleware: security headers, an internal API key gate for machine-to-machine endpoints,
  forwarded-headers support, fail-loud bootstrap validation, and bounded Serilog flush on shutdown
- Render-agnostic UI — `UI.Abstractions` contracts and an `IUIRenderer` seam, with a Spectre console
  renderer as the reference implementation
- Headless Blazor components — a Context/Provider/ProviderLog triple that keeps logic in
  render-agnostic providers so skins stay presentation-only
- Realtime hubs — a `RealTimeHubBase<TClient>` base plus a `RealTimeHubs` TypeCollection so each
  SignalR hub is registered as a single type option instead of app-specific wiring
- `Fdw.DevSession.Abstractions` — worktree/branch isolation contracts (`IWorktreeEngine`,
  `IIsolationLevel`) for tooling that needs to stage isolated copies of a working tree

[1.0.0-rc.1]: https://github.com/CyberdyneDevelopment/FractalDataWorks/releases/tag/v1.0.0-rc.1
