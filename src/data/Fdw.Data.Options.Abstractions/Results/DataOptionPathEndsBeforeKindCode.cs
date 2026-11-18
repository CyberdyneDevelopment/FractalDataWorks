using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Data.Options.Results;

/// <summary>DataOptionPathEndsBeforeKind (FDW-6340).</summary>
[TypeOption(typeof(DataOptionResultCodes), "DataOptionPathEndsBeforeKind", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DataOptionPathEndsBeforeKindCode : DataOptionResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="DataOptionPathEndsBeforeKindCode"/> class.</summary>
    public DataOptionPathEndsBeforeKindCode()
        : base(20002, "DataOptionPathEndsBeforeKind", ResultSeverities.ByName("Error"),
            "Data option path '{Path}' ends at '{Name}', which has layers below it ({Known}), not at a kind",
            isRetryable: false)
    {
    }

    /// <inheritdoc />
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
