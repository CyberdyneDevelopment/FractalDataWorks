using System.Collections.Generic;
using Fdw.Commands.Data.Abstractions;

namespace Fdw.Services.Connections.Abstractions.UnitOfWork;

/// <summary>What each step of a committed unit of work answered (FDW-4988), in step order.</summary>
/// <param name="Affected">The rows each step affected.</param>
/// <param name="Answers">The answer of each step whose command reports its unmatched keys; <see langword="null"/> for every other step.</param>
public sealed record UnitOfWorkAnswers(IReadOnlyList<int> Affected, IReadOnlyList<BulkWriteAnswer?> Answers);
