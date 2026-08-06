using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Aegis.Abstractions;

/// <summary>
/// The approval policy rendered a non-approving verdict for the requested action.
/// </summary>
[TypeOption(typeof(AegisResultCodes), "ActionDenied", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ActionDeniedCode : AegisResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ActionDeniedCode"/> class.
    /// </summary>
    public ActionDeniedCode()
        : base(51000, "ActionDenied",
            ResultSeverities.ByName("Warning"),
            "Action '{commandName}' was denied: {reason}",
            isRetryable: false)
    {
    }
}
