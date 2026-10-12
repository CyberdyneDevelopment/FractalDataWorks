using Fdw.Configuration;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Calculations;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions.ResultCodes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fdw.Services.Calculations.Abstractions;

/// <summary>
/// Generic CRTP base for calculation entity types that use a typed configuration.
/// Seals the dispatch methods and exposes typed abstract methods for implementors.
/// </summary>
/// <typeparam name="TConfiguration">The concrete configuration type for this calculation entity.</typeparam>
public abstract class CalculationEntityBase<TConfiguration> : CalculationEntityTypeBase
    where TConfiguration : class, IGenericConfiguration
{
    /// <summary>
    /// Initializes a new instance of <see cref="CalculationEntityBase{TConfiguration}"/>.
    /// </summary>
    protected CalculationEntityBase(string name, string displayName, string description)
        : base(name, displayName, description) { }

    /// <inheritdoc />
    public sealed override Type ConfigurationType => typeof(TConfiguration);

    /// <inheritdoc />
    public sealed override void Configure(IServiceCollection services, IConfiguration configuration)
        => services.Configure<List<TConfiguration>>(
               configuration.GetSection($"Calculations:{Name}"));

    /// <inheritdoc />
    public sealed override IGenericResult ValidateConfiguration(IGenericConfiguration configuration)
    {
        if (configuration is not TConfiguration typed)
            return GenericResult.Failure(CalculationEntityResultCodes.ByName("ConfigurationTypeMismatch"));
        return ValidateTypedConfiguration(typed);
    }

    /// <inheritdoc />
    public sealed override Task<IGenericResult<string>> Execute(
        ICalculationEntity entity,
        IReadOnlyList<ResolvedCalculationInput> inputs,
        ICalculationContext context,
        CancellationToken cancellationToken)
        => ExecuteTyped(entity, inputs, context, cancellationToken);

    /// <summary>
    /// Validates the strongly-typed configuration for this calculation entity.
    /// </summary>
    protected abstract IGenericResult ValidateTypedConfiguration(TConfiguration config);

    /// <summary>
    /// Executes the calculation with resolved inputs and typed context.
    /// </summary>
    protected abstract Task<IGenericResult<string>> ExecuteTyped(
        ICalculationEntity entity,
        IReadOnlyList<ResolvedCalculationInput> inputs,
        ICalculationContext context,
        CancellationToken cancellationToken);
}
