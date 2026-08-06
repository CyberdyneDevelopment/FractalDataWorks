using Fdw.ServiceTypes;
using $namespace$.$serviceName$.Abstractions;

namespace $namespace$.$serviceName$;

/// <summary>
/// Base class for $serviceName$ service type definitions.
/// </summary>
public abstract class $serviceName$TypeBase<TService, TFactory, TConfiguration>
    : ServiceTypeBase<TService, TFactory, TConfiguration>,
      I$serviceName$Type
    where TService : class, I$serviceName$Service
    where TFactory : class, I$serviceName$Factory
    where TConfiguration : class, I$serviceName$Configuration
{
    protected $serviceName$TypeBase(
        string name,
        string displayName,
        string description,
        string? category = null)
        : base(
            name,
            $"Services:$serviceName$:{name}",
            displayName,
            description,
            category ?? "$serviceName$")
    {
    }
}
