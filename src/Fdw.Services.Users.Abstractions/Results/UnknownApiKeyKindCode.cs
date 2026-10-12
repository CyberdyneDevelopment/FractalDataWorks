using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Users.Results;

/// <summary>
/// The requested API key kind is neither <c>user</c> nor <c>agent</c>. The two authenticate as
/// different principals, so neither is assumed for an unrecognised name.
/// </summary>
[TypeOption(typeof(UserResultCodes), "UnknownApiKeyKind", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class UnknownApiKeyKindCode : UserResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnknownApiKeyKindCode"/> class.
    /// </summary>
    public UnknownApiKeyKindCode()
        : base(20001, "UnknownApiKeyKind",
            ResultSeverities.ByName("Error"),
            "API key kind must be 'user' or 'agent'.",
            isRetryable: false)
    {
    }
}
