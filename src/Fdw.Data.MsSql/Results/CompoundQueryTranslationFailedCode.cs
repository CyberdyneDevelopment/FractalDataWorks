using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.MsSql.Results;

/// <summary>
/// Compound query translation failed with exception.
/// </summary>
[TypeOption(typeof(MsSqlDataResultCodes), "CompoundQueryTranslationFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class CompoundQueryTranslationFailedCode : MsSqlDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompoundQueryTranslationFailedCode"/> class.
    /// </summary>
    public CompoundQueryTranslationFailedCode()
        : base(91002, "CompoundQueryTranslationFailed",
            ResultSeverities.ByName("Error"),
            "Failed to translate compound query: {ErrorMessage}",
            isRetryable: false)
    {
    }
}