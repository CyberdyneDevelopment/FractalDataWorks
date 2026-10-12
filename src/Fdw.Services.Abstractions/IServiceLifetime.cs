using Microsoft.Extensions.DependencyInjection;

namespace Fdw.Services.Abstractions;

/// <summary>
/// Interface for service lifetime types.
/// </summary>
public interface IServiceLifetime
{
    /// <summary>
    /// Gets the unique identifier for this lifetime type.
    /// </summary>
    int Id { get; }

    /// <summary>
    /// Gets the name of this lifetime type.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets a description of when this lifetime should be used.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the Microsoft ServiceLifetime enum value for DI container registration.
    /// </summary>
    ServiceLifetime EnumValue { get; }
}