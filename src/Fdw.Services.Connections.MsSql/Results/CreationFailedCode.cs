using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.MsSql.Results;

/// <summary>
/// Connection factory failed to create connection.
/// </summary>
[TypeOption(typeof(MsSqlResultCodes), "CreationFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class CreationFailedCode : MsSqlResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreationFailedCode"/> class.
    /// </summary>
    public CreationFailedCode()
        : base(
            71000,
            "CreationFailed",
            ResultSeverities.ByName("Error"),
            "Failed to create connection '{ConnectionName}': {ErrorMessage}",
            isRetryable: false)
    {
    }
}
