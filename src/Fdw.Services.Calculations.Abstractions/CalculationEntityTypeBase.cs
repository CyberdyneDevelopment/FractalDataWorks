using Fdw.Configuration;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Calculations;
using Fdw.Collections;
using Fdw.Results;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fdw.Services.Calculations.Abstractions;

/// <summary>
/// Abstract base class for all calculation entity types.
/// Its Guid id is derived from the option's fully qualified type name (<c>GuidTypeOptionBase</c>).
/// </summary>
public abstract class CalculationEntityTypeBase :
    GuidTypeOptionBase<CalculationEntityTypeBase>,
    ICalculationEntityType
{
    /// <summary>
    /// Initializes a new instance of <see cref="CalculationEntityTypeBase"/>.
    /// </summary>
    /// <param name="name">The unique type name used as the registry key.</param>
    /// <param name="displayName">Human-readable display name.</param>
    /// <param name="description">Description of what this calculation type does.</param>
    protected CalculationEntityTypeBase(string name, string displayName, string description)
        : base(name)
    {
        DisplayName = displayName;
        Description = description;
    }

    /// <summary>
    /// Gets the human-readable display name for this calculation entity type.
    /// </summary>
    public new string DisplayName { get; }

    /// <summary>
    /// Gets the description of this calculation entity type.
    /// </summary>
    public new string Description { get; }

    /// <inheritdoc />
    public abstract Type ConfigurationType { get; }

    /// <summary>
    /// Gets the container name for loading this type's configuration record from ConfigurationDb.
    /// Returns <c>null</c> when the type has no additional typed configuration table.
    /// </summary>
    public virtual string? TypedContainerName => null;

    /// <summary>
    /// Gets the settings this kind reads from the configuration a create or update sends, in the order a form shows them.
    /// Create and update refuse a configuration member that is not named here.
    /// </summary>
    public abstract IReadOnlyList<CalculationSetting> Settings { get; }

    /// <summary>
    /// Reads the value of each declared setting from a configuration of this kind: the same-named member of
    /// <see cref="ConfigurationType"/>, matched without regard to case as create binds it.
    /// </summary>
    /// <param name="configuration">A configuration of this kind's <see cref="ConfigurationType"/>.</param>
    /// <returns>One entry per declared setting, by the setting's name.</returns>
    /// <exception cref="ArgumentException">The configuration is not an instance of <see cref="ConfigurationType"/>.</exception>
    public IReadOnlyDictionary<string, object?> SettingValues(IGenericConfiguration configuration)
    {
        if (configuration is null)
            throw new ArgumentNullException(nameof(configuration));

        if (!ConfigurationType.IsInstanceOfType(configuration))
            throw new ArgumentException(
                $"The configuration is a {configuration.GetType().Name}, not the {ConfigurationType.Name} that calculation type '{Name}' reads.",
                nameof(configuration));

        var values = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var setting in Settings)
        {
            values[setting.Name] = ConfigurationType
                .GetProperty(setting.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)?
                .GetValue(configuration);
        }

        return values;
    }

    /// <summary>
    /// Builds a typed configuration from a raw node configuration dictionary.
    /// Base returns <c>null</c> for types with no typed configuration table.
    /// </summary>
    public virtual IGenericConfiguration? CreateTypedConfiguration(
        IReadOnlyDictionary<string, object?> nodeConfiguration, Guid entityId)
        => null;

    /// <inheritdoc />
    public abstract void Configure(IServiceCollection services, IConfiguration configuration);

    /// <inheritdoc />
    public abstract IGenericResult ValidateConfiguration(IGenericConfiguration configuration);

    /// <inheritdoc />
    public abstract Task<IGenericResult<string>> Execute(
        ICalculationEntity entity,
        IReadOnlyList<ResolvedCalculationInput> inputs,
        ICalculationContext context,
        CancellationToken cancellationToken);
}
