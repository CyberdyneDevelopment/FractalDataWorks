using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Identity.Results;

/// <summary>
/// The named identity configuration does not exist.
/// </summary>
[TypeOption(typeof(IdentityResultCodes), "IdentityConfigurationNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class IdentityConfigurationNotFoundCode : IdentityResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="IdentityConfigurationNotFoundCode"/> class.</summary>
    public IdentityConfigurationNotFoundCode()
        : base(
            31000,
            "IdentityConfigurationNotFound",
            ResultSeverities.ByName("Error"),
            "No identity configuration named '{configurationName}' exists.",
            isRetryable: false)
    { }
}
