using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.PostgreSql.Results;

/// <summary>
/// Compound query translation failed with exception.
/// </summary>
[TypeOption(typeof(PostgreSqlDataResultCodes), "CompoundQueryTranslationFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class CompoundQueryTranslationFailedCode : PostgreSqlDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompoundQueryTranslationFailedCode"/> class.
    /// </summary>
    public CompoundQueryTranslationFailedCode()
        : base(91006, "CompoundQueryTranslationFailed",
            ResultSeverities.ByName("Error"),
            "Failed to translate compound query: {ErrorMessage}",
            isRetryable: false)
    {
    }
}
