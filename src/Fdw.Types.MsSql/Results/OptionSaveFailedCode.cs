using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Types.MsSql;

/// <summary>
/// Failed to save TypeOption.
/// </summary>
[TypeOption(typeof(MsSqlTypesResultCodes), "OptionSaveFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class OptionSaveFailedCode : MsSqlTypesResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OptionSaveFailedCode"/> class.
    /// </summary>
    public OptionSaveFailedCode()
        : base(71000, "OptionSaveFailed",
            ResultSeverities.ByName("Error"),
            "Failed to save TypeOption '{Name}': {ErrorMessage}",
            isRetryable: true)
    {
    }
}