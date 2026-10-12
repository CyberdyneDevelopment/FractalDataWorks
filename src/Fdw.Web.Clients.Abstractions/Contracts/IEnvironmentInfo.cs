namespace Fdw.Web.Clients.Abstractions.Contracts;

using System;

/// <summary>
/// Abstraction for environment information used across Operations and Analytics domains.
/// </summary>
public interface IEnvironmentInfo
{
    /// <summary>Gets the unique identifier.</summary>
    Guid Id { get; }
    /// <summary>Gets the environment name.</summary>
    string Name { get; }
    /// <summary>Gets the environment description.</summary>
    string Description { get; }
}
