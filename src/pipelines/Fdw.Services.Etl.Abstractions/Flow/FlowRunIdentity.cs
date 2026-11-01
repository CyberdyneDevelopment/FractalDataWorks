using System;
using System.Collections.Generic;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>
/// Who a pipeline run is, to a step that starts another pipeline (FDW-1403): the execution it runs under, its tenant and its roles.
/// </summary>
/// <param name="ExecutionId">The execution a started pipeline becomes a child of.</param>
/// <param name="TenantId">The tenant the started pipeline belongs to.</param>
/// <param name="RunRoles">The roles the started pipeline acts with.</param>
public sealed record FlowRunIdentity(Guid ExecutionId, Guid TenantId, IReadOnlyList<string> RunRoles);
