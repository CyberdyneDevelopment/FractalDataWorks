using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.SecretManagers.Abstractions.Results;

/// <summary>
/// No handler found for the specified command type.
/// </summary>
[TypeOption(typeof(SecretManagerResultCodes), "NoHandlerFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class NoHandlerFoundCode : SecretManagerResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NoHandlerFoundCode"/> class.
    /// </summary>
    public NoHandlerFoundCode()
        : base(60002, "NoHandlerFound",
            ResultSeverities.ByName("Error"),
            "No handler found for command type '{CommandType}'",
            isRetryable: false)
    {
    }
}
