using Fdw.Configuration;
using System;
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
/// Defines a calculation entity type that can be registered in the CalculationEntityTypes collection.
/// </summary>
public interface ICalculationEntityType : ITypeOption<Guid, CalculationEntityTypeBase>
{
    /// <summary>
    /// Gets the concrete configuration type for this calculation entity type.
    /// </summary>
    Type ConfigurationType { get; }

    /// <summary>
    /// Gets the container name for loading this type's configuration record from ConfigurationDb.
    /// Returns <c>null</c> when the type has no additional typed configuration table.
    /// </summary>
    string? TypedContainerName { get; }

    /// <summary>
    /// Gets the settings this kind reads from the configuration a create or update sends, in the order a form shows them.
    /// </summary>
    IReadOnlyList<CalculationSetting> Settings { get; }

    /// <summary>
    /// Reads the value of each declared setting from a configuration of this kind, by the setting's name.
    /// </summary>
    /// <param name="configuration">A configuration of this kind's <see cref="ConfigurationType"/>.</param>
    /// <returns>One entry per declared setting.</returns>
    IReadOnlyDictionary<string, object?> SettingValues(IGenericConfiguration configuration);

    /// <summary>
    /// Builds a typed <see cref="IGenericConfiguration"/> from a raw node configuration dictionary.
    /// Returns <c>null</c> for entity types that carry no typed configuration.
    /// Used by the graph-compile bridge to construct typed config from designer node state.
    /// </summary>
    IGenericConfiguration? CreateTypedConfiguration(IReadOnlyDictionary<string, object?> nodeConfiguration, Guid entityId);

    /// <summary>
    /// Binds the configuration section for this calculation entity type.
    /// Called during Phase 1a (Configure) of DI registration.
    /// </summary>
    void Configure(IServiceCollection services, IConfiguration configuration);

    /// <summary>
    /// Validates that the provided configuration is correct for this entity type.
    /// </summary>
    IGenericResult ValidateConfiguration(IGenericConfiguration configuration);

    /// <summary>
    /// Executes the calculation using the provided entity, resolved inputs, and context.
    /// Returns the serialized output as a string.
    /// </summary>
    Task<IGenericResult<string>> Execute(
        ICalculationEntity entity,
        IReadOnlyList<ResolvedCalculationInput> inputs,
        ICalculationContext context,
        CancellationToken cancellationToken);
}
