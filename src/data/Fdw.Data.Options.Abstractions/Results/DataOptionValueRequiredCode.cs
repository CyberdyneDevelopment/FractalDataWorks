using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Data.Options.Results;

/// <summary>
/// An option that needs a value was applied to a row that has none.
/// </summary>
/// <remarks>The catalog's RequiredValueMissing (20000): a 400, never retried. The caller is told which option and column.</remarks>
[TypeOption(typeof(DataOptionResultCodes), "DataOptionValueRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DataOptionValueRequiredCode : DataOptionResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataOptionValueRequiredCode"/> class.
    /// </summary>
    public DataOptionValueRequiredCode()
        : base(20000, "DataOptionValueRequired", ResultSeverities.ByName("Error"),
            "Option {OptionType}.{Option} needs a value, and the row for column '{Column}' of container '{Container}' has none",
            isRetryable: false)
    {
    }

    /// <inheritdoc />
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
