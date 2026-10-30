using System;
using Fdw.Services.Etl.Abstractions.OptionTypes;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// Why a waiting run cannot start now (FDW-3097), as the scheduler's own rules judged it.
/// </summary>
/// <param name="Reason">The option of <see cref="RunWaitReasons"/> that says which rule holds the run.</param>
/// <param name="Limit">The limit that holds the run, for a server, tenant or pipeline limit; otherwise <see langword="null"/>.</param>
/// <param name="BlockedByExecutionId">The run in flight or ahead of this one that holds it, when one does; otherwise <see langword="null"/>.</param>
/// <param name="Table">The table both runs write, for a shared table; otherwise <see langword="null"/>.</param>
/// <param name="HeldUntil">When a held run joins the queue (FDW-3129); otherwise <see langword="null"/>.</param>
public sealed record PipelineRunBlock(IRunWaitReason Reason, int? Limit, Guid? BlockedByExecutionId, string? Table, DateTimeOffset? HeldUntil = null);
