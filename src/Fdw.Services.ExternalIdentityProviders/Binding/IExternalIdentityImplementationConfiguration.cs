using System;
using Fdw.Configuration;

namespace Fdw.Services.ExternalIdentityProviders.Binding;

/// <summary>The contract every ExternalIdentity implementation carries.</summary>
public interface IExternalIdentityImplementationConfiguration : IImplementationConfiguration
{
    /// <summary>Gets or sets the domain record's durable id.</summary>
    Guid ExternalIdentityId { get; set; }

    /// <summary>Gets or sets the domain record's row id -- the foreign key the constraint is on.</summary>
    int ExternalIdentityRowId { get; set; }

    /// <summary>Gets or sets the issuer this identity came from.</summary>
    string Provider { get; set; }

    /// <summary>Gets or sets the subject that issuer proved.</summary>
    string ExternalSubject { get; set; }

    /// <summary>Gets or sets the local user the subject resolves to.</summary>
    Guid UserId { get; set; }

    /// <summary>Gets or sets whether the binding is in force.</summary>
    bool IsActive { get; set; }
}
