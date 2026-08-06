using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.UI.Providers.Results;

/// <summary>
/// A provider context exposed a callback its provider never wired.
/// </summary>
/// <remarks>
/// Why this is a failure rather than a silent no-op: a context callback that defaults to
/// <c>Task.CompletedTask</c> reports success for an operation that never ran, so a page's save
/// button appears to work and the record is silently not written. Category 6 (Configuration)
/// because the defect is in how the provider was wired, not in what the user submitted.
/// </remarks>
[TypeOption(typeof(UIProviderResultCodes), "CallbackNotProvided", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class CallbackNotProvidedCode : UIProviderResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CallbackNotProvidedCode"/> class.
    /// </summary>
    public CallbackNotProvidedCode()
        : base(61000, "CallbackNotProvided",
            ResultSeverities.ByName("Error"),
            "The provider did not supply this operation.",
            isRetryable: false)
    {
    }
}
