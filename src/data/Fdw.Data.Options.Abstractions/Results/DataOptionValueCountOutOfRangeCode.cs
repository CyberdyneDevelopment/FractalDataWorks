using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Data.Options.Results;

/// <summary>DataOptionValueCountOutOfRange (FDW-6340).</summary>
[TypeOption(typeof(DataOptionResultCodes), "DataOptionValueCountOutOfRange", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DataOptionValueCountOutOfRangeCode : DataOptionResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="DataOptionValueCountOutOfRangeCode"/> class.</summary>
    public DataOptionValueCountOutOfRangeCode()
        : base(20001, "DataOptionValueCountOutOfRange", ResultSeverities.ByName("Error"),
            "Kind {Kind} on field '{Field}' takes {Minimum} to {Maximum} values and was given {Count}",
            isRetryable: false)
    {
    }

    /// <inheritdoc />
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
