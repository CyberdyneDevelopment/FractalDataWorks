using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.ExternalIdentityProviders.Results;

/// <summary>
/// The presented subject does not match anything this provisioner recognizes — the canonical
/// NOT-FOUND CONTRACT outcome an <see cref="Abstractions.IExternalIdentityProvisioner"/> returns to
/// mean "not mine, try the next one" rather than a hard failure. See
/// <see cref="Abstractions.IExternalIdentityProvisioner"/>'s remarks for the full contract.
/// </summary>
[TypeOption(typeof(ExternalIdentityProvisionerResultCodes), "ProvisionerNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ProvisionerNotFoundCode : ExternalIdentityProvisionerResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="ProvisionerNotFoundCode"/> class.</summary>
    public ProvisionerNotFoundCode()
        : base(30000, "ProvisionerNotFound",
            ResultSeverities.ByName("Warning"),
            "No configured rule matches the presented subject",
            isRetryable: false)
    {
    }
}
