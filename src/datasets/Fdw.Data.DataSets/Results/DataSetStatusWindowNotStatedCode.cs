using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.DataSets.Results;

/// <summary>
/// The setting that says how long a completed run counts as recent is not stated for the caller (FDW-4488).
/// </summary>
/// <remarks>
/// Nothing assumes a window: how long a data set may go without a completed run before it reads as Idle is configuration, and a list without it stops and names the setting.
/// </remarks>
[TypeOption(typeof(DataSetsResultCodes), "DataSetStatusWindowNotStated", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DataSetStatusWindowNotStatedCode : DataSetsResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="DataSetStatusWindowNotStatedCode"/> class.</summary>
    public DataSetStatusWindowNotStatedCode()
        : base(90003, "DataSetStatusWindowNotStated",
            ResultSeverities.ByName("Error"),
            "The status of a data set needs the setting '{setting}', which is not stated for this caller or server: state how many hours a completed run counts as recent",
            isRetryable: false)
    {
    }
}
