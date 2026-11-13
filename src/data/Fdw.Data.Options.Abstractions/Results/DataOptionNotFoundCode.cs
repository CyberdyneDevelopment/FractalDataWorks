using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Data.Options.Results;

/// <summary>
/// A stored row names an option that its option type does not have.
/// </summary>
/// <remarks>The catalog's NotFound (30000): a 404, never retried. The caller is told which option type and option.</remarks>
[TypeOption(typeof(DataOptionResultCodes), "DataOptionNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DataOptionNotFoundCode : DataOptionResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataOptionNotFoundCode"/> class.
    /// </summary>
    public DataOptionNotFoundCode()
        : base(30000, "DataOptionNotFound", ResultSeverities.ByName("Error"),
            "Option type {OptionType} has no option named '{Option}'",
            isRetryable: false)
    {
    }

    /// <inheritdoc />
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
