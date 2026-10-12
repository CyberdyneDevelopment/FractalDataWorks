using System;
using System.Text.Json;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Calculations;
using Microsoft.Extensions.Logging;

namespace Fdw.Calculations.Endpoints.CalculationEntities;

/// <summary>
/// Reads a calculation's type-specific configuration from a request body into the configuration class
/// its calculation entity type declares.
/// </summary>
/// <remarks>
/// The implementation is the record a calculation is written as, so create and update both need it.
/// The JSON carries only the type's own settings (for Formula: <c>FormulaLanguage</c>,
/// <c>FormulaBody</c>, <c>TimeoutSeconds</c>); the service stamps the shared fields (name, inputs,
/// output) onto it. Same shape as the secret manager create endpoint: resolve the option, deserialize
/// into its <c>ConfigurationType</c>, and let the option validate what it received.
/// </remarks>
public static class CalculationTypedConfigurationReader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Deserializes <paramref name="configuration"/> into <paramref name="calculationEntityType"/>'s
    /// configuration class and validates it.
    /// </summary>
    /// <param name="calculationEntityType">The calculation entity type name, e.g. <c>Formula</c>.</param>
    /// <param name="configuration">The type-specific settings, as a JSON object.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>The typed configuration, or a validation failure naming what was wrong.</returns>
    public static IGenericResult<IGenericConfiguration> Read(
        string calculationEntityType, JsonElement configuration, ILogger logger)
    {
        var option = CalculationEntityTypes.ByName(calculationEntityType);
        if (option == CalculationEntityTypes.NotFound)
            return GenericResult<IGenericConfiguration>.Failure(
                CalculationEndpointLog.TypedConfigurationUnknownType(logger, calculationEntityType));

        if (configuration.ValueKind != JsonValueKind.Object)
            return GenericResult<IGenericConfiguration>.Failure(
                CalculationEndpointLog.TypedConfigurationUnreadable(
                    logger, calculationEntityType, option.ConfigurationType.Name, "configuration must be a JSON object"));

        object? parsed;
        try
        {
            parsed = configuration.Deserialize(option.ConfigurationType, JsonOptions);
        }
        catch (JsonException ex)
        {
            return GenericResult<IGenericConfiguration>.Failure(
                CalculationEndpointLog.TypedConfigurationUnreadable(
                    logger, calculationEntityType, option.ConfigurationType.Name, ex.Message));
        }

        if (parsed is not IGenericConfiguration typed)
            return GenericResult<IGenericConfiguration>.Failure(
                CalculationEndpointLog.TypedConfigurationUnreadable(
                    logger, calculationEntityType, option.ConfigurationType.Name, "configuration did not deserialize"));

        var valid = option.ValidateConfiguration(typed);
        if (valid.IsFailure)
            return valid.ToNewResult<IGenericConfiguration>();

        return GenericResult<IGenericConfiguration>.Success(typed);
    }
}
