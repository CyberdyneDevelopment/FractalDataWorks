using System;
using Fdw.Configuration;

namespace Fdw.Services.Authentication.Abstractions;

/// <summary>
/// The contract every authentication-service implementation configuration satisfies.
/// </summary>
/// <remarks>
/// An authentication service is two rows: the domain row names it and says which kind it is, and one
/// implementation row carries what that kind needs to validate a token. A LocalKey entry needs an
/// audience; a remote issuer needs a metadata address. Neither belongs on the domain row, because a
/// host that declares one kind would carry the other kind's columns empty.
/// <para>
/// Held by the domain configuration, never inherited from it. Inheriting locks the domain to its
/// first implementation: the second kind cannot be added without changing the type the first one is.
/// </para>
/// </remarks>
public interface IAuthenticationServiceImplementationConfiguration : IImplementationConfiguration
{
    /// <summary>Gets or sets the id of the authentication service this configuration belongs to.</summary>
    /// <remarks>
    /// The owner's logical id, which is what the configuration system resolves by. The database also
    /// carries the owner's RowId and declares the foreign key on it; that identity stays in the
    /// container's keys and never reaches this model.
    /// </remarks>
    Guid AuthenticationServiceId { get; set; }
}
