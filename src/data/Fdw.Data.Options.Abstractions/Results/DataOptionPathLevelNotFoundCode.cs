using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Data.Options.Results;

/// <summary>DataOptionPathLevelNotFound (FDW-6340).</summary>
[TypeOption(typeof(DataOptionResultCodes), "DataOptionPathLevelNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DataOptionPathLevelNotFoundCode : DataOptionResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="DataOptionPathLevelNotFoundCode"/> class.</summary>
    public DataOptionPathLevelNotFoundCode()
        : base(30001, "DataOptionPathLevelNotFound", ResultSeverities.ByName("Error"),
            "Data option path '{Path}': level {Level} '{Name}' was not found (known: {Known})",
            isRetryable: false)
    {
    }

    /// <inheritdoc />
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
