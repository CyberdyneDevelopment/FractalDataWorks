using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Data.Options.Results;

/// <summary>DataOptionMethodNotFound (FDW-6340).</summary>
[TypeOption(typeof(DataOptionResultCodes), "DataOptionMethodNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DataOptionMethodNotFoundCode : DataOptionResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="DataOptionMethodNotFoundCode"/> class.</summary>
    public DataOptionMethodNotFoundCode()
        : base(30004, "DataOptionMethodNotFound", ResultSeverities.ByName("Error"),
            "Data option path '{Path}' names method '{Method}'; the kind's method is {KindMethod}",
            isRetryable: false)
    {
    }

    /// <inheritdoc />
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
