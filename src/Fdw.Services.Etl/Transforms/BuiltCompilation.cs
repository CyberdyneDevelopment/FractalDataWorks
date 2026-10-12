using System;
using System.Collections.Generic;
using Fdw.Services.Calculations.Abstractions.Runtime;

namespace Fdw.Services.Etl.Transforms;

/// <summary>What the Compute stages of a pipeline compiled to when the run started, by the id of each stage.</summary>
/// <param name="Calculations">The Built transformations that read one record at a time.</param>
/// <param name="Sets">The Built transformations that read and write tables.</param>
public sealed record BuiltCompilation(
    IReadOnlyDictionary<Guid, ICompiledCalculation> Calculations,
    IReadOnlyDictionary<Guid, ICompiledCalculationSet> Sets);
