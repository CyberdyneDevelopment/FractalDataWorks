# Message code renumbers (FDW-4843)

A code's category is its number divided by 10000. The category chooses the HTTP status a caller is answered, whether the
failure is retried, and whether it counts toward failure thresholds. The codes below were numbered in the wrong category. Each now
carries a number in the right category, free across its whole TypeCode. Where it was free, the number keeps its last four digits.
A consumer that matches on an old code string must match the new one. The message text of every code is unchanged.

Audit scope: every `[MessageLogging]` code that reaches a result, in categories 6, 7, 8 and 9, plus downstream-failure messages
in the 4xx categories (FDW-4832). Log-only and unused codes were not changed.

## A request mistake answered a 5xx status; it now answers its 4xx (46)

| Old code | New code | Status before | Status after | Method | Reason |
|---|---|---|---|---|---|
| SETTINGS-62056 | SETTINGS-22056 | 500 Configuration | 400 Validation | `InstanceDefaultLog.ChoiceNotStated` | a create body leaves out a caller choice; same shape as DATA-71095 |
| ENDPOINTS6-71005 | ENDPOINTS6-21007 | 502 Dependency | 400 Validation | `ConnectionEndpointLog.HealthCheckEnabledWithoutTrigger` | create/update connection body turns health checks on with no trigger |
| CMDDATA-62014 | CMDDATA-22014 | 500 Configuration | 400 Validation | `ConfigurationWriteLog.LogicalKeyValueMissing` | a save carries no value for a field of its Logical key |
| CMDDATA-62019 | CMDDATA-22019 | 500 Configuration | 400 Validation | `ConfigurationWriteLog.MatchValueMissing` | a save/retire carries no value for a field it matches on |
| OPERATIONS-61152 | OPERATIONS-21152 | 500 Configuration | 400 Validation | `OperationsLog.LatestRunsNameBlank` | a pipeline name passed for the latest runs is blank |
| ABSTRACTIONS6-81000 | ABSTRACTIONS6-151000 | 503 Transient | 429 TooManyRequests | `ConnectionLimitLog.RateExceeded` | caller is over the connection's per-second rate cap |
| ABSTRACTIONS6-81002 | ABSTRACTIONS6-21002 | 503 Transient | 400 Validation | `ConnectionLimitLog.MaxResultSizeExceeded` | caller asked for more rows than the connection's cap |
| ABSTRACTIONS6-81004 | ABSTRACTIONS6-151004 | 503 Transient | 429 TooManyRequests | `ConnectionLimitLog.DailyQueryBudgetExhausted` | caller used up the connection's daily query budget |
| CATALOG-91643 | CATALOG-21643 | 500 Internal | 400 Validation | `CatalogSearchLog.WindowPastSource` | catalog search asks for a window past what the source reads |
| DATA-61013 | DATA-21013 | 500 Configuration | 400 Validation | `DataGatewayLogger.VectorizedNotSupported` | vector command sent to a store that holds no vectors |
| DELETESCOPE-90100 | DELETESCOPE-20100 | 500 Internal | 400 Validation | `DeleteScopeLog.Refused` | a delete states no filter, no soft-delete and no AllRows |
| AUTHENTICATION-91262 | AUTHENTICATION-21262 | 500 Internal | 400 Validation | `FlowAdministrationLog.NameMissing` | sign-in flow change names no flow |
| AUTHENTICATION-91263 | AUTHENTICATION-31263 | 500 Internal | 404 Missing | `FlowAdministrationLog.FlowNotConfigured` | sign-in flow change names a flow that does not exist |
| AUTHENTICATION-91268 | AUTHENTICATION-21268 | 500 Internal | 400 Validation | `FlowAdministrationLog.PermissionNotDefined` | sign-in flow gated on a permission that does not exist |
| AUTHENTICATION-91269 | AUTHENTICATION-121269 | 500 Internal | 409 Refused | `FlowAdministrationLog.FlowCannotBeGated` | flow's steps establish nothing a permission gate needs |
| AUTHENTICATION-91300 | AUTHENTICATION-121300 | 500 Internal | 409 Refused | `FlowAdministrationLog.FlowGateAmbiguous` | flow has several gates; change refused |
| AUTHENTICATION-91303 | AUTHENTICATION-121303 | 500 Internal | 409 Refused | `FlowAdministrationLog.PermissionHeldByNoRole` | gate would admit nobody; change refused |
| AUTHENTICATION-91304 | AUTHENTICATION-121304 | 500 Internal | 409 Refused | `FlowAdministrationLog.ChangeLocksOutAdministrator` | change refused: it would lock out the administrator |
| AUTHENTICATION-91305 | AUTHENTICATION-121305 | 500 Internal | 409 Refused | `FlowAdministrationLog.FlowGateNotAdministrable` | flow's gate cannot be changed on this host; change refused |
| AUTHENTICATION-91231 | AUTHENTICATION-31231 | 500 Internal | 404 Missing | `FlowProviderLog.NoSuchFlow` | sign-in names a flow that is not configured |
| AUTHENTICATION-91233 | AUTHENTICATION-21233 | 500 Internal | 400 Validation | `FlowProviderLog.NameMissing` | sign-in names no flow |
| AUTHENTICATION-91121 | AUTHENTICATION-121121 | 500 Internal | 409 Refused | `FlowProviderLog.FlowDisabled` | sign-in names a switched-off flow |
| AUTHENTICATION-91108 | AUTHENTICATION-101108 | 500 Internal | 403 Forbidden | `RunnerLog.NotPermitted` | flow decided not to issue for this subject |
| AUTHENTICATION-91109 | AUTHENTICATION-51109 | 500 Internal | 401 Auth | `RunnerLog.InsufficientAssurance` | subject reached a lower assurance level than the flow needs |
| AUTHENTICATION-91272 | AUTHENTICATION-51272 | 500 Internal | 401 Auth | `DatabaseAuthorizationCodeStoreLog.NotRedeemable` | presented sign-in code redeems nothing |
| AUTHENTICATION-91273 | AUTHENTICATION-21273 | 500 Internal | 400 Validation | `DatabaseAuthorizationCodeStoreLog.CodeMissing` | no sign-in code presented |
| AUTHENTICATION-91132 | AUTHENTICATION-51132 | 500 Internal | 401 Auth | `ExecutionStoreLog.NotResumable` | presented resume token resumes nothing |
| AUTHENTICATION-91153 | AUTHENTICATION-51153 | 500 Internal | 401 Auth | `DatabaseExecutionStoreLog.NotResumable` | presented resume token resumes nothing |
| AUTHENTICATION-91220 | AUTHENTICATION-21220 | 500 Internal | 400 Validation | `RequestStoreLog.StateMissing` | no state value presented |
| AUTHENTICATION-91222 | AUTHENTICATION-41222 | 500 Internal | 409 Conflict | `RequestStoreLog.StateReused` | state value reused |
| AUTHENTICATION-91223 | AUTHENTICATION-21223 | 500 Internal | 400 Validation | `RequestStoreLog.NoSuchRequest` | presented state matches no pending request |
| AUTHENTICATION-91280 | AUTHENTICATION-21280 | 500 Internal | 400 Validation | `SignInAdmissionLog.MethodMissing` | admission question names no sign-in method |
| TOKENMANAGER-91210 | TOKENMANAGER-51210 | 500 Internal | 401 Auth | `IssuerLog.ClaimsAlreadyExpired` | claims presented for a token already stopped being true |
| AUTHVALIDATION-71117 | AUTHVALIDATION-51117 | 502 Dependency | 401 Auth | `AuthenticationValidationLog.TokenRejected` | presented token rejected (category only; ASP.NET answers 401 itself) |
| AUTHVALIDATION-71132 | AUTHVALIDATION-51132 | 502 Dependency | 401 Auth | `AuthenticationValidationLog.TokenRejectedWithoutReason` | presented token rejected (category only; ASP.NET answers 401 itself) |
| DEVSESSION-92012 | DEVSESSION-22012 | 500 Internal | 400 Validation | `WorktreeEngineLog.RepoPathInvalid` | requested repository path is not a git repository |
| DEVSESSION-92030 | DEVSESSION-32030 | 500 Internal | 404 Missing | `DevSessionManagerLog.SessionNotFoundById` | unknown session id |
| DEVSESSION-92031 | DEVSESSION-32031 | 500 Internal | 404 Missing | `DevSessionManagerLog.SessionNotFoundByKey` | unknown session key |
| DEVSESSION-92032 | DEVSESSION-22032 | 500 Internal | 400 Validation | `DevSessionManagerLog.UnknownIsolationLevel` | request names an isolation level that is not an option |
| DEVSESSION-92033 | DEVSESSION-42033 | 500 Internal | 409 Conflict | `DevSessionManagerLog.SessionIsTerminal` | operation on a session already in a terminal state |
| DEVSESSION-92034 | DEVSESSION-42034 | 500 Internal | 409 Conflict | `DevSessionManagerLog.InvalidTransition` | operation not allowed from the session's state |
| DEVSESSION-92050 | DEVSESSION-42050 | 500 Internal | 409 Conflict | `WorkspaceCoordinatorLog.ScopeOverlap` | strand paths overlap another strand's claim |
| DEVSESSION-92051 | DEVSESSION-42051 | 500 Internal | 409 Conflict | `WorkspaceCoordinatorLog.StrandAlreadyFenced` | strand already fenced |
| DEVSESSION-92052 | DEVSESSION-32052 | 500 Internal | 404 Missing | `WorkspaceCoordinatorLog.StrandNotFound` | unknown strand |
| DEVSESSION-92054 | DEVSESSION-42054 | 500 Internal | 409 Conflict | `WorkspaceCoordinatorLog.StrandIsTerminal` | strand already terminal |
| DEVSESSION-92055 | DEVSESSION-22055 | 500 Internal | 400 Validation | `WorkspaceCoordinatorLog.EmptyScope` | scope request claims no path |

## A downstream, host or configuration fault answered a 4xx status; it now answers its 5xx (32)

| Old code | New code | Status before | Status after | Method | Reason |
|---|---|---|---|---|---|
| MSSQL-51004 | MSSQL-71037 | 401 Auth | 502 Dependency | `MsSqlConnectionLogger.SqlLoginFailed` | the platform's own SQL login failed; the client is not unauthenticated |
| PIPELINERUN-20231 | PIPELINERUN-60231 | 400 Validation | 500 Configuration | `PipelineRunLog.WatermarkStoreNotNamed` | host names no operational store |
| PIPELINERUN-20232 | PIPELINERUN-70232 | 400 Validation | 502 Dependency | `PipelineRunLog.WatermarkReadFailed` | reading the watermark failed downstream |
| PIPELINERUN-20803 | PIPELINERUN-70803 | 400 Validation | 502 Dependency | `PipelineRunStartLog.ExecutionNotCreated` | execution record could not be created |
| PIPELINERUN-20804 | PIPELINERUN-70804 | 400 Validation | 502 Dependency | `PipelineRunStartLog.RunRowNotWritten` | run row could not be written |
| PIPELINERUN-20805 | PIPELINERUN-60805 | 400 Validation | 500 Configuration | `PipelineRunStartLog.OperationalStoreNotNamed` | host names no operational connection |
| PIPELINERUN-22116 | PIPELINERUN-72116 | 400 Validation | 502 Dependency | `PipelineRunRecoveryLog.RequestNotWritten` | run request could not be written |
| PIPELINERUN-22118 | PIPELINERUN-72118 | 400 Validation | 502 Dependency | `PipelineRunRecoveryLog.RequestUnreadable` | recorded run request cannot be read |
| PIPELINERUN-22123 | PIPELINERUN-72123 | 400 Validation | 502 Dependency | `PipelineRunRecoveryLog.RecordNotClosed` | run record could not be closed |
| PIPELINERUN-22127 | PIPELINERUN-62127 | 400 Validation | 500 Configuration | `PipelineRunRecoveryLog.StoreNotNamed` | host names no operational store |
| PIPELINERUN-22129 | PIPELINERUN-72129 | 400 Validation | 502 Dependency | `PipelineRunRecoveryLog.RequestNotRemoved` | stored run request could not be removed |
| PIPELINERUN-22133 | PIPELINERUN-62133 | 400 Validation | 500 Configuration | `PipelineRunRecoveryLog.RestartLimitMissing` | restart-limit setting not set |
| PIPELINERUN-22137 | PIPELINERUN-72137 | 400 Validation | 502 Dependency | `PipelineRunSchedulerLog.RefusedRequestNotForgotten` | stored request could not be removed after a full queue |
| PIPELINERUN-22711 | PIPELINERUN-62711 | 400 Validation | 500 Configuration | `PipelineRunRecoveryLog.StopGraceSettingMissing` | shutdown-grace setting not set |
| PIPELINERUN-22716 | PIPELINERUN-72716 | 400 Validation | 502 Dependency | `PipelineRunRecoveryLog.CommittedLoadsNotRead` | committed destination rows could not be read |
| FLOW-20925 | FLOW-70925 | 400 Validation | 502 Dependency | `FlowLog.SourceReadFailed` | flow source could not read rows |
| FLOW-20927 | FLOW-70927 | 400 Validation | 502 Dependency | `FlowLog.DestinationLoadFailed` | flow destination could not load a batch |
| FLOW-20928 | FLOW-70928 | 400 Validation | 502 Dependency | `FlowLog.DestinationTruncateFailed` | flow destination could not truncate its target |
| FLOW-20930 | FLOW-70930 | 400 Validation | 502 Dependency | `FlowScriptLog.ReadFailed` | script source could not read its script |
| FLOW-20932 | FLOW-70932 | 400 Validation | 502 Dependency | `FlowScriptLog.RunFailed` | script step could not run its script |
| COMPUTE-20629 | COMPUTE-70629 | 400 Validation | 502 Dependency | `ComputeSetLog.DataSetUnreadable` | compute stage could not read its data set |
| COMPUTE-20630 | COMPUTE-60630 | 400 Validation | 500 Configuration | `ComputeSetLog.NoGateway` | run has no data gateway |
| COMPUTE-20601 | COMPUTE-60601 | 400 Validation | 500 Configuration | `ComputeLog.TransformationProviderMissing` | host gave the run no Transformation provider |
| COMPUTE-20608 | COMPUTE-60608 | 400 Validation | 500 Configuration | `ComputeLog.LimitSettingMissing` | calculation-limit setting missing |
| COMPUTE-20612 | COMPUTE-60612 | 400 Validation | 500 Configuration | `ComputeLog.SettingsProviderMissing` | host gave the run no settings provider |
| DATA-20988 | DATA-60988 | 400 Validation | 500 Configuration | `DatabaseResetWatcherLog.IntervalSettingMissing` | reset-poll setting missing |
| SERVICES-21067 | SERVICES-61067 | 400 Validation | 500 Configuration | `DefaultConfigurationProviderLog.KeyChunkSizeNotStated` | provider's key chunk size invalid |
| QUALITYENDPOINTS-20003 | QUALITYENDPOINTS-60003 | 400 Validation | 500 Configuration | `QualityCheckEndpointLog.ExecutionStoreNotNamed` | host names no operational connection |
| DATA-41003 | DATA-61022 | 409 Conflict | 500 Configuration | `DataGatewayLogger.DataStoreHasNoConnectionId` | stored data store has no ConnectionId |
| ETLGATEWAY-20988 | ETLGATEWAY-70988 | 400 Validation | 502 Dependency | `EtlGatewayLog.StoreLookupFailed` | whether the host declares the store could not be read |
| SCRIPTS-20975 | SCRIPTS-90975 | 400 Validation | 500 Internal | `ScriptEndpointLog.SavedButUnreadable` | saved and cannot be read back |
| WEBHOOK-20994 | WEBHOOK-90994 | 400 Validation | 500 Internal | `WebhookEndpointLog.SavedButUnreadable` | saved and cannot be read back |

## A configuration, data or internal fault was numbered Dependency (502, retried); it now sits in its own category (30)

| Old code | New code | Status before | Status after | Method | Reason |
|---|---|---|---|---|---|
| AUTHENTICATION-91301 | AUTHENTICATION-61301 | 500 Internal | 500 Configuration | `FlowAdministrationLog.GateStepUnavailable` | host registers not exactly one permission-gate step |
| AUTHVALIDATION-71101 | AUTHVALIDATION-61101 | 502 Dependency | 500 Configuration | `AuthenticationValidationLog.EntryMissingName` | host authentication entry declares no Name |
| AUTHVALIDATION-71102 | AUTHVALIDATION-61102 | 502 Dependency | 500 Configuration | `AuthenticationValidationLog.EntryMissingAuthority` | host authentication entry declares no Authority |
| AUTHVALIDATION-71103 | AUTHVALIDATION-61103 | 502 Dependency | 500 Configuration | `AuthenticationValidationLog.AuthorityNotAbsolute` | host Authority not an absolute URL |
| AUTHVALIDATION-71104 | AUTHVALIDATION-61104 | 502 Dependency | 500 Configuration | `AuthenticationValidationLog.JwtBearerMissingAudience` | host JwtBearer entry declares no Audience |
| AUTHVALIDATION-71105 | AUTHVALIDATION-61105 | 502 Dependency | 500 Configuration | `AuthenticationValidationLog.JwtBearerMissingRoles` | host JwtBearer entry declares no Roles |
| AUTHVALIDATION-71106 | AUTHVALIDATION-61106 | 502 Dependency | 500 Configuration | `AuthenticationValidationLog.NoAuthenticationServicesDeclared` | host declares no authentication services |
| AUTHVALIDATION-71108 | AUTHVALIDATION-61108 | 502 Dependency | 500 Configuration | `AuthenticationValidationLog.NoOptionForImplementation` | entry names an Implementation no option registers |
| AUTHVALIDATION-71110 | AUTHVALIDATION-61110 | 502 Dependency | 500 Configuration | `AuthenticationValidationLog.DeclaredRolesNotResolved` | declared roles do not resolve to permissions |
| AUTHVALIDATION-71113 | AUTHVALIDATION-61113 | 502 Dependency | 500 Configuration | `AuthenticationValidationLog.LocalSigningKeyUnavailable` | no local signing key configured |
| AUTHVALIDATION-71114 | AUTHVALIDATION-61114 | 502 Dependency | 500 Configuration | `AuthenticationValidationLog.LocalKeyMissingAudience` | host LocalKey entry declares no Audience |
| AUTHVALIDATION-71129 | AUTHVALIDATION-61129 | 502 Dependency | 500 Configuration | `AuthenticationValidationLog.JwtBearerMissingAcceptedClaims` | host JwtBearer entry declares no AcceptedClaims |
| AUTHVALIDATION-71130 | AUTHVALIDATION-61130 | 502 Dependency | 500 Configuration | `AuthenticationValidationLog.JwtBearerAcceptedClaimUnknown` | AcceptedClaims names an unknown claim |
| AUTHVALIDATION-71131 | AUTHVALIDATION-61131 | 502 Dependency | 500 Configuration | `AuthenticationValidationLog.JwtBearerAcceptedClaimSessionBound` | AcceptedClaims names a session-bound claim |
| AUTHORIZATION-71006 | AUTHORIZATION-61006 | 502 Dependency | 500 Configuration | `AuthorizationLog.RoleNameUnknown` | a declaration names a role the catalogue does not hold |
| ETL-71328 | ETL-61328 | 502 Dependency | 500 Configuration | `PipelineFailureRuleLog.OperationalConnectionNotNamed` | host names no operational connection |
| ETL-71330 | ETL-61330 | 502 Dependency | 500 Configuration | `PipelineFailureRuleLog.CountedOutcomeUnknown` | threshold row names an outcome that does not exist |
| USERS-72739 | USERS-62739 | 502 Dependency | 500 Configuration | `DefaultWorkspaceFoundingLog.Unavailable` | host does not run the workspaces service |
| USERS-72741 | USERS-62741 | 502 Dependency | 500 Configuration | `UnavailableWritersLog.TenantWriterUnavailable` | host does not run the tenant service |
| USERS-72742 | USERS-62742 | 502 Dependency | 500 Configuration | `UnavailableWritersLog.OrganizationWriterUnavailable` | host does not run the tenant service |
| MESSAGING-71003 | MESSAGING-61003 | 502 Dependency | 500 Configuration | `MessagingLog.LocationNotConfigured` | Messaging DataStoreName/PathName not resolvable |
| DATA-71052 | DATA-61052 | 502 Dependency | 500 Configuration | `DataGatewayLogger.DataStoreProviderUnavailable` | gateway built with no IDataStoreProvider |
| TYPES-71005 | TYPES-61005 | 502 Dependency | 500 Configuration | `MsSqlTypesProviderLog.UnknownCollectionKind` | stored row names a collection kind not declared |
| ENDPOINTS2-71004 | ENDPOINTS2-91004 | 502 Dependency | 500 Internal | `ConfigurationEndpointLog.InstanceRowIncomplete` | stored row lacks a column; data fault, not a downstream outage |
| DATAENDPOINTS-71013 | DATAENDPOINTS-91013 | 502 Dependency | 500 Internal | `StandInEndpointLog.StandInHasNoStrategy` | stored stand-in has no strategy row; data fault |
| ENDPOINTS9-71000 | ENDPOINTS9-91000 | 502 Dependency | 500 Internal | `PipelineEndpointLog.RunRolesExpansionUnexplained` | role expansion failed with no reason; internal fault |
| ENDPOINTS9-71001 | ENDPOINTS9-91001 | 502 Dependency | 500 Internal | `PipelineEndpointLog.RunRolesExpansionEmpty` | role expansion succeeded with nothing; internal fault |
| ETL-71323 | ETL-91323 | 502 Dependency | 500 Internal | `PipelineFailureRuleLog.RunTimeMissing` | ended run row holds no completion time; data fault |
| ETL-71325 | ETL-91325 | 502 Dependency | 500 Internal | `PipelineFailureRuleLog.DisableRecordMissing` | run holds no usable disable record; data fault |
| SCHEDULEPIPELINE-71000 | SCHEDULEPIPELINE-91000 | 502 Dependency | 500 Internal | `SchedulePipelineLog.SchedulePipelineLookupEmpty` | lookup answered success with no pipeline; internal fault |

## User endpoints that threw away the inner failure (FDW-4851) (6)

| Old code | New code | Status before | Status after | Method | Reason |
|---|---|---|---|---|---|
| USERSENDPOINTS-21045 | USERSENDPOINTS-91045 | 400 Validation | 500 Internal | `UserEndpointLog.RevokeApiKeyTheKeyCouldNotBe` | revoke failed; the inner failure now decides the status, this code only when it carries none |
| USERSENDPOINTS-21047 | USERSENDPOINTS-91047 | 400 Validation | 500 Internal | `UserEndpointLog.UnbindExternalIdentityTheBindingCouldNotBe` | unbind failed; inner failure decides |
| USERSENDPOINTS-21048 | USERSENDPOINTS-91048 | 400 Validation | 500 Internal | `UserEndpointLog.ResetPasswordFailedToResetPassword` | password reset store failed; inner failure decides |
| USERSENDPOINTS-21049 | USERSENDPOINTS-91049 | 400 Validation | 500 Internal | `UserEndpointLog.RequirePasswordChangeTheAccountCouldNotBe` | force-password-change failed; inner failure decides |
| USERSENDPOINTS-21050 | USERSENDPOINTS-91050 | 400 Validation | 500 Internal | `UserEndpointLog.SetPasswordLoginThePasswordCouldNotBe` | password store failed; inner failure decides |
| USERSENDPOINTS-21052 | USERSENDPOINTS-91052 | 400 Validation | 500 Internal | `UserEndpointLog.CreateApiKeyReturnedNothing` | key mint failed or returned nothing; inner failure decides |

These endpoints now answer the inner failure, with the endpoint's own message appended. The inner failure's first coded message picks the status. The endpoint code (now Internal) stands only when the inner failure carries no code.

## Result code (1)

| Old code | New code | Status before | Status after | Code | Reason |
|---|---|---|---|---|---|
| HTTP-91013 | HTTP-21013 | 500 Internal | 400 Validation | `HttpResultCodes.UnsupportedFilterOperator` | the caller's filter uses an operator the protocol cannot express |
## Refusals that stay uniform

Some sign-in refusals are one refusal on purpose: missing, expired and already-used read the same, so the answer cannot confirm that a code or token was real. Each keeps one code and one status:

- `DatabaseAuthorizationCodeStoreLog.NotRedeemable`, AUTHENTICATION-51272, 401.
- `ExecutionStoreLog.NotResumable` and `DatabaseExecutionStoreLog.NotResumable`, AUTHENTICATION-51132 and AUTHENTICATION-51153, both 401.

The sign-in routes in reference-servicetypes set their own statuses (`FlowSelectionStatus`, `FlowRunStatus`), and they do not change: a flow lookup answers through its `FlowNotFound` or `FlowDisabled` result code, and a run its caller failed answers 401. None of these messages carries client text.

## Where old codes were matched

Old codes appeared in three FDW test assertions (PIPELINERUN-20231 and 20232, MSSQL 51004), and all three are updated. reference-servicetypes, reference-api, reference-etl, reference-scheduler, reference-react-ui and techcom match none of the old codes.
