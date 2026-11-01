using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Types.MsSql;

/// <summary>
/// Schema initialization failed.
/// </summary>
[TypeOption(typeof(MsSqlTypesResultCodes), "SchemaInitializationFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SchemaInitializationFailedCode : MsSqlTypesResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaInitializationFailedCode"/> class.
    /// </summary>
    public SchemaInitializationFailedCode()
        : base(71002, "SchemaInitializationFailed",
            ResultSeverities.ByName("Error"),
            "Failed to initialize types schema: {ErrorMessage}",
            isRetryable: true)
    {
    }
}