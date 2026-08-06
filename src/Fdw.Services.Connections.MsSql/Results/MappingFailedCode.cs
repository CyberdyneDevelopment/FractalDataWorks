using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.MsSql.Results;

/// <summary>
/// POCO mapping failed.
/// </summary>
[TypeOption(typeof(MsSqlResultCodes), "MappingFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class MappingFailedCode : MsSqlResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MappingFailedCode"/> class.
    /// </summary>
    public MappingFailedCode()
        : base(
            90002,
            "MappingFailed",
            ResultSeverities.ByName("Error"),
            "Failed to map type '{TypeName}': {ErrorMessage}",
            isRetryable: false)
    {
    }
}
