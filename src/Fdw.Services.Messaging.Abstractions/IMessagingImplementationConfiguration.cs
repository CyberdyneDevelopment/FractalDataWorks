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
    /// <summary>Gets or sets the data store the messages themselves live in.</summary>
    string? DataStoreName { get; set; }

    /// <summary>Gets or sets the path within that store.</summary>
    string? PathName { get; set; }

    /// <summary>Gets or sets the human-readable description.</summary>
    string? Description { get; set; }
}
