using System.Collections.Generic;
using Fdw.Operations.Abstractions.Thresholds;
using Fdw.Results;

namespace Fdw.Services.Quality.Thresholds;

/// <summary>
/// Base of the quality threshold types (FDW-3935): a type that reports a measure of a data set also knows how to take it. The measure is taken from the rows the caller read; the
/// rules of the threshold then judge it.
/// </summary>
public abstract class QualityThresholdTypeBase : ThresholdTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="QualityThresholdTypeBase"/> class.</summary>
    /// <param name="name">The option name.</param>
    /// <param name="description">What the type measures.</param>
    protected QualityThresholdTypeBase(string name, string description)
        : base(name, description)
    {
    }

    /// <inheritdoc/>
    public override string SubjectKind => "DataSet";

    /// <summary>Takes the measure.</summary>
    /// <param name="input">The parameters, rows and match timeout.</param>
    /// <returns>The measure by field name, each of the type's Fields; a failure naming why it could not be taken, which the caller reports as not run and never as a pass.</returns>
    public abstract IGenericResult<IReadOnlyDictionary<string, object?>> Measure(QualityMeasureInput input);
}
