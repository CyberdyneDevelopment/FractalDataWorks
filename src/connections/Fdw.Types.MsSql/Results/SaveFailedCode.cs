using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Types.MsSql;

/// <summary>
/// Save operation failed.
/// </summary>
[TypeOption(typeof(MsSqlTypesResultCodes), "SaveFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SaveFailedCode : MsSqlTypesResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SaveFailedCode"/> class.
    /// </summary>
    public SaveFailedCode()
        : base(71001, "SaveFailed",
            ResultSeverities.ByName("Error"),
            "Failed to save TypeCollection metadata: {ErrorMessage}",
            isRetryable: true)
    {
    }
}