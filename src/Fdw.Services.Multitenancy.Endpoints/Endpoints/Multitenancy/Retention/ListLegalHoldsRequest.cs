namespace Fdw.Services.Multitenancy.Endpoints;

/// <summary>
/// Request to list the legal holds of an organization (FDW-1575).
/// </summary>
public class ListLegalHoldsRequest : OrganizationRetentionRequest
{
    /// <summary>Gets or sets a value indicating whether only the holds that stand are listed; false lists the lifted ones too.</summary>
    public bool HeldOnly { get; set; }
}
