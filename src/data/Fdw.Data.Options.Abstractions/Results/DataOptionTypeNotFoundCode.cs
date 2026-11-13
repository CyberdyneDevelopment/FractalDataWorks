using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Data.Options.Results;

/// <summary>
/// A stored row names an option type that is not one of the data option types.
/// </summary>
/// <remarks>NotFound in this package's own slot (31000), because 30000 is the option itself: a 404, never retried.</remarks>
[TypeOption(typeof(DataOptionResultCodes), "DataOptionTypeNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DataOptionTypeNotFoundCode : DataOptionResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataOptionTypeNotFoundCode"/> class.
    /// </summary>
    public DataOptionTypeNotFoundCode()
        : base(31000, "DataOptionTypeNotFound", ResultSeverities.ByName("Error"),
            "'{OptionType}' is not a data option type",
            isRetryable: false)
    {
    }

    /// <inheritdoc />
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
