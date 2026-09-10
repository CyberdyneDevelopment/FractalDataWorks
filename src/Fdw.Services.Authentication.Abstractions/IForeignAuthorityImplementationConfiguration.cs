using System;
using Fdw.Configuration;

namespace Fdw.Services.Authentication.Abstractions;

/// <summary>The contract every ForeignAuthority implementation carries.</summary>
public interface IForeignAuthorityImplementationConfiguration : IImplementationConfiguration
{
    /// <summary>Gets or sets the domain record's durable id.</summary>
    Guid ForeignAuthorityId { get; set; }

    /// <summary>Gets or sets the domain record's row id -- the foreign key the constraint is on.</summary>
    int ForeignAuthorityRowId { get; set; }

    /// <summary>Gets or sets the ForeignAuthority Issuer.</summary>
    string Issuer { get; set; }

    /// <summary>Gets or sets the ForeignAuthority JwksUri.</summary>
    string JwksUri { get; set; }

    /// <summary>Gets or sets the ForeignAuthority ValidAudiences.</summary>
    string ValidAudiences { get; set; }

    /// <summary>Gets or sets the ForeignAuthority ValidAlgorithms.</summary>
    string ValidAlgorithms { get; set; }

    /// <summary>Gets or sets the ForeignAuthority AssertableMethods.</summary>
    string? AssertableMethods { get; set; }

    /// <summary>Gets or sets the ForeignAuthority ClockSkewSeconds.</summary>
    int ClockSkewSeconds { get; set; }
}
