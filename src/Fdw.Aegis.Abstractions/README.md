# Fdw.Aegis.Abstractions

Aegis contracts.

Implementations live in the sibling packages; this one holds the interfaces, base types and models they agree on, so a consumer can depend on the contract without taking the implementation.

## Interfaces (3)

| Type | Kind | Purpose |
|---|---|---|
| `IApprovalPolicyConfiguration` | interface | Marker interface for typed Aegis command approval-policy bodies (PreApprovedCommandConfiguration,… |
| `IApprovalPolicyEvaluator` | interface | Deterministically evaluates an against the declared approval policy and renders a . |
| `IVerdictDisposition` | interface | A closed disposition an can render for an . Behavior (whether the disposition is final, whether it… |

## Base types (4)

| Type | Kind | Purpose |
|---|---|---|
| `AegisResultCodeBase` | class | Base class for Aegis Gateway result codes. |
| `AegisResultCodes` | class | TypeCollection for Aegis Gateway result codes. Codes use categorized catalog numbers (prefix "AEG"; Code… |
| `VerdictDispositionBase` | class | CRTP base class for options. Each concrete disposition supplies its id, name, and the two behavior flags… |
| `VerdictDispositions` | class | Closed collection of verdict dispositions (Approve/Deny/Abstain/Pending). A TypeCollection, not an enum… |

## Models and supporting types (12)

| Type | Kind | Purpose |
|---|---|---|
| `AbstainDisposition` | class | A non-terminal verdict: the evaluator declines to decide (e.g. an agent approver passing to a human).… |
| `ActionDeniedCode` | class | The approval policy rendered a non-approving verdict for the requested action. |
| `ApprovalRequest` | class | The deterministic ask submitted to an . Mirrors the shape Claude submits to the Aegis request_action… |
| `ApproveDisposition` | class | A terminal, injection-permitting verdict. The only disposition for which is . |
| `ConnectionNotDeclaredCode` | class | The requested command/connection pair has no matching declared Commands entry. |
| `DenyDisposition` | class | A terminal, non-approving verdict. The fail-closed default for a new . |
| `InjectionFailedCode` | class | Aegis.Injector resolved the secret but the downstream injection call failed. |
| `ParameterNotInAllowListCode` | class | A submitted parameter is absent from the command's ParameterAllowList, or its value is not one of the… |
| `PendingDisposition` | class | A non-terminal verdict: the request is enqueued and awaiting a decision (Phase 2 human-in-the-loop). Not… |
| `RequiredValueMissingCode` | class | A required value was not provided. Reuses the FDW-reserved canonical Validation code (20000). |
| `SecretResolutionFailedCode` | class | Aegis.Injector failed to resolve the referenced secret from its declared secret manager. |
| `Verdict` | class | The outcome of evaluating an . |

## Installation

```bash
dotnet add package Fdw.Aegis.Abstractions --prerelease
```

## Dependencies

`Fdw.Abstractions` · `Fdw.Collections` · `Fdw.Results` · `Fdw.Results.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
