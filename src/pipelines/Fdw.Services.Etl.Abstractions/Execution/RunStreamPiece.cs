using System;

namespace Fdw.Services.Etl.Abstractions.Execution;

/// <summary>
/// One piece of what a run gives its person (FDW-6140). <paramref name="Sequence"/> only grows, so a client that asks again from the last one it received gets exactly the pieces that follow, once and in order.
/// </summary>
/// <param name="Sequence">The piece's place in the stream, which a client resumes from.</param>
/// <param name="ExecutionId">The run that wrote it (a restart writes under the chain's first run, but names its own run here).</param>
/// <param name="Kind">One of <see cref="RunStreamKinds"/>.</param>
/// <param name="StepName">The step it is about; <see langword="null"/> for a text no step is named for.</param>
/// <param name="Text">The text, or how the step ended; <see langword="null"/> for a step that started or was skipped.</param>
/// <param name="CreatedAt">When it was written.</param>
public sealed record RunStreamPiece(long Sequence, Guid ExecutionId, string Kind, string? StepName, string? Text, DateTimeOffset CreatedAt);
