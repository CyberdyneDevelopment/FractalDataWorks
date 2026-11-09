using System;
using System.Collections.Generic;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>What a status rule reads about one data set (FDW-4488).</summary>
/// <remarks>
/// A rule reads only this: it does no I/O, takes nothing from DI and never asks for the time, so it can be tested by constructing the input.
/// </remarks>
/// <param name="Writers">The pipelines that write the data set, each with its latest run. Empty when none writes it.</param>
/// <param name="Window">How long a completed run counts as recent. The caller reads it from the setting and states it; no rule assumes one.</param>
/// <param name="Now">The time the status is judged at.</param>
public sealed record DataSetStatusInput(IReadOnlyList<DataSetWriterRun> Writers, TimeSpan Window, DateTimeOffset Now);
