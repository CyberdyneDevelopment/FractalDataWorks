using Microsoft.Extensions.DependencyInjection;

namespace Fdw.Services.Abstractions;


/// <summary>
/// Base class for service lifetime types.
/// </summary>
public abstract class ServiceLifetimeBase : IServiceLifetime
{
    /// <summary>
    /// Gets the unique identifier for this lifetime type.
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// Gets the name of this lifetime type.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets a description of when this lifetime should be used.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the Microsoft ServiceLifetime enum value.
    /// </summary>
    public ServiceLifetime EnumValue { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceLifetimeBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for the lifetime type.</param>
    /// <param name="name">The name of the lifetime type.</param>
    /// <param name="description">A description of when this lifetime should be used.</param>
    /// <param name="enumValue">The Microsoft ServiceLifetime enum value.</param>
    protected ServiceLifetimeBase(int id, string name, string description, ServiceLifetime enumValue)
    {
        Id = id;
        Name = name;
        Description = description;
        EnumValue = enumValue;
    }
}