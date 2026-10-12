using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Users.Results;

/// <summary>
/// The binding names an issuer that was not among the ones this platform could read, and at least
/// one authority could not be read at all — so the platform cannot honestly say the issuer is
/// unknown. Refusing on an incomplete list would name an authority that may be configured and
/// merely unreadable this second.
/// </summary>
[TypeOption(typeof(UserResultCodes), "ExternalIssuersUnreadable", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ExternalIssuersUnreadableCode : UserResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExternalIssuersUnreadableCode"/> class.
    /// </summary>
    public ExternalIssuersUnreadableCode()
        : base(70002, "ExternalIssuersUnreadable",
            ResultSeverities.ByName("Error"),
            "Whether this platform can resolve that issuer could not be determined: an authority could not be read.",
            isRetryable: true)
    {
    }
}
