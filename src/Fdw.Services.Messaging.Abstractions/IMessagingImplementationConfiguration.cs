using System;
using Fdw.Configuration;

namespace Fdw.Services.Messaging.Abstractions;

/// <summary>
/// The contract every messaging implementation configuration satisfies.
/// </summary>
/// <remarks>
/// Messaging is two rows: the domain row names the service and says which kind it is, and one
/// implementation row carries what that kind needs. Held by the domain configuration, never inherited
/// from it — inheriting locks the domain to its first implementation, because a second kind cannot be
/// added without changing the type the first one is.
/// </remarks>
public interface IMessagingImplementationConfiguration : IImplementationConfiguration
{
    /// <summary>Gets or sets the id of the messaging service this configuration belongs to.</summary>
    /// <remarks>
    /// The owner's logical id, which is what the configuration system resolves by. The database also
    /// carries the owner's RowId and declares the foreign key on it; that identity stays in the
    /// container's keys and never reaches this model.
    /// </remarks>
    Guid MessagingId { get; set; }
}
