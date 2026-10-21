using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.MsSql.Results;

/// <summary>
/// Building a nearest search's statement failed with an exception (FDW-1354).
/// </summary>
[TypeOption(typeof(MsSqlDataResultCodes), "NearestTranslationFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class NearestTranslationFailedCode : MsSqlDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NearestTranslationFailedCode"/> class.
    /// </summary>
    public NearestTranslationFailedCode()
        : base(91010, "NearestTranslationFailed",
            ResultSeverities.ByName("Error"),
            "Nearest translation failed: {ErrorMessage}",
            isRetryable: false)
    {
    }
}
