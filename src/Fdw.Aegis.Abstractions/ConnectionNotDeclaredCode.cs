using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Aegis.Abstractions;

/// <summary>
/// The requested command/connection pair has no matching declared <c>Commands</c> entry.
/// </summary>
[TypeOption(typeof(AegisResultCodes), "ConnectionNotDeclared", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ConnectionNotDeclaredCode : AegisResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionNotDeclaredCode"/> class.
    /// </summary>
    public ConnectionNotDeclaredCode()
        : base(31000, "ConnectionNotDeclared",
            ResultSeverities.ByName("Error"),
            "Command '{commandName}' is not declared for connection '{connectionName}'.",
            isRetryable: false)
    {
    }
}
