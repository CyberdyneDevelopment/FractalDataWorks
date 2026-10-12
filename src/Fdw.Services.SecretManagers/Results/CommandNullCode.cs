using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.SecretManagers.Results;

/// <summary>
/// Command validation failed because the command was null.
/// </summary>
[TypeOption(typeof(SecretManagerResultCodes), "CommandNull", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class CommandNullCode : SecretManagerResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandNullCode"/> class.
    /// </summary>
    public CommandNullCode()
        : base(
            20000,
            "CommandNull",
            ResultSeverities.ByName("Error"),
            "Command cannot be null",
            isRetryable: false)
    { }
}
