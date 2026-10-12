using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Types.MsSql;

/// <summary>
/// TypeOption not found.
/// </summary>
[TypeOption(typeof(MsSqlTypesResultCodes), "OptionNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class OptionNotFoundCode : MsSqlTypesResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OptionNotFoundCode"/> class.
    /// </summary>
    public OptionNotFoundCode()
        : base(31001, "OptionNotFound",
            ResultSeverities.ByName("Error"),
            "TypeOption '{Name}' was not found in collection '{Collection}'",
            isRetryable: false)
    {
    }
}