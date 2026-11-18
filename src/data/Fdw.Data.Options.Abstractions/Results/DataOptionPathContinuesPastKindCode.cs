using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Data.Options.Results;

/// <summary>DataOptionPathContinuesPastKind (FDW-6340).</summary>
[TypeOption(typeof(DataOptionResultCodes), "DataOptionPathContinuesPastKind", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DataOptionPathContinuesPastKindCode : DataOptionResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="DataOptionPathContinuesPastKindCode"/> class.</summary>
    public DataOptionPathContinuesPastKindCode()
        : base(20003, "DataOptionPathContinuesPastKind", ResultSeverities.ByName("Error"),
            "Data option path '{Path}' names '{Extra}' below the kind '{Kind}'",
            isRetryable: false)
    {
    }

    /// <inheritdoc />
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
