using System;
using System.Linq;
using Fdw.Results;
using Fdw.Services.Calculations;
using Fdw.Services.Calculations.Abstractions;

namespace Fdw.Calculations.Endpoints.CalculationEntities;

/// <summary>
/// Maps a calculation entity to the detail DTO that the get, create and update calculation endpoints answer,
/// so the three answer the same fields.
/// </summary>
public static class CalculationEntityDetailMapper
{
    /// <summary>Answers a calculation read as the detail a get or update endpoint finds, or as nothing found.</summary>
    /// <param name="read">The result of reading the calculation from the calculation service.</param>
    /// <returns>
    /// The detail when the calculation was read; a success with no value when it does not exist, which the endpoint answers as 404;
    /// the read's own failure for any other failure, so a failed read is not reported as a missing calculation.
    /// </returns>
    public static IGenericResult<CalculationEntityDetailDto?> Found(IGenericResult<ICalculationEntity> read)
    {
        ArgumentNullException.ThrowIfNull(read);

        if (read.IsFailure)
        {
            return FailureCategory.IsMissing(read)
                ? GenericResult<CalculationEntityDetailDto?>.Success((CalculationEntityDetailDto?)null)
                : read.ToNewResult<CalculationEntityDetailDto?>();
        }

        return GenericResult<CalculationEntityDetailDto?>.Success(read.Value is { } entity ? ToDetail(entity) : null);
    }

    // The stored configuration, read through the kind that declares its settings; nothing to read when the calculation holds none
    // or its kind is not registered.
    private static Dictionary<string, object?>? ConfigurationOf(ICalculationEntity entity)
    {
        if (entity.TypedConfiguration is not { } configuration)
            return null;

        var kind = CalculationEntityTypes.ByName(entity.CalculationEntityType);
        return kind == CalculationEntityTypes.NotFound
            ? null
            : new Dictionary<string, object?>(kind.SettingValues(configuration), StringComparer.Ordinal);
    }

    /// <summary>Maps a calculation entity to its detail response.</summary>
    /// <param name="entity">The calculation entity.</param>
    /// <returns>The detail DTO, with the entity's inputs and output.</returns>
    public static CalculationEntityDetailDto ToDetail(ICalculationEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new CalculationEntityDetailDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            CalculationEntityType = entity.CalculationEntityType,
            OutputDataSetName = entity.Output.OutputDataSetName,
            ResultFieldName = entity.Output.ResultFieldName,
            ResultDataTypeName = entity.Output.ResultDataTypeName,
            IsEnabled = entity.IsEnabled,
            Configuration = ConfigurationOf(entity),
            Inputs = entity.Inputs.Select(i => new CalculationEntityInputDto
            {
                InputAlias = i.InputAlias,
                InputKind = i.Kind.Name,
                DataSetName = i.DataSetName,
                ConnectionName = i.ConnectionName,
                ContainerPath = i.ContainerPath,
                ScalarValueType = i.ScalarValue?.ValueType.Name,
                ScalarSerializedValue = i.ScalarValue?.SerializedValue
            }).ToList()
        };
    }
}
