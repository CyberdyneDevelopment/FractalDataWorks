using System;
using System.Collections.Generic;

namespace Fdw.Services.Etl.Execution;

/// <summary>What a judgment of the person a run acts as is for (FDW-6137, FDW-6144): the run, its pipeline and the roles it acts with, which the refusals name.</summary>
/// <param name="ExecutionId">The run's execution item.</param>
/// <param name="PipelineName">The pipeline the run is of.</param>
/// <param name="RunRoles">The roles the run acts with: every permission they carry must be one the person holds.</param>
internal sealed record RunPersonSubject(Guid ExecutionId, string PipelineName, IReadOnlyList<string> RunRoles);
