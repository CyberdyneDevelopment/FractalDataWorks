using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Services.ExternalIdentityProviders.Results;

/// <summary>
/// TypeCollection for ExternalIdentityProvisioner result codes.
/// </summary>
[TypeCollection(typeof(ExternalIdentityProvisionerResultCodeBase), typeof(IResultCode), typeof(ExternalIdentityProvisionerResultCodes))]
public abstract partial class ExternalIdentityProvisionerResultCodes : TypeCollectionBase<ExternalIdentityProvisionerResultCodeBase, IResultCode>
{
}
