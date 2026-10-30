using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Calculations.Endpoints.CalculationEntities;

/// <summary>
/// Maps the input declarations of a create or update calculation request to calculation inputs, so both
/// endpoints read a Scalar input's value and refuse an input kind or scalar type the service does not have, and an input member its
/// kind does not take.
/// </summary>
public static class CalculationEntityInputMapper
{
    /// <summary>Maps input declarations to calculation inputs.</summary>
    /// <param name="dtos">The input declarations of the request.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>
    /// The inputs, or a validation failure naming the input and what was wrong: an input kind or scalar value type
    /// that is not offered, a member the input's kind does not take, or a Scalar input without its type or value.
    /// </returns>
    public static IGenericResult<List<CalculationInput>> ToInputs(IEnumerable<CalculationEntityInputDto> dtos, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(dtos);
        ArgumentNullException.ThrowIfNull(logger);

        var inputs = new List<CalculationInput>();
        foreach (var dto in dtos)
        {
            if (string.IsNullOrWhiteSpace(dto.InputKind) || CalculationInputKinds.ByName(dto.InputKind).Id == 0)
            {
                return GenericResult<List<CalculationInput>>.Failure(
                    CalculationEndpointLog.InputKindUnknown(logger, dto.InputAlias, dto.InputKind, OfferedKinds));
            }

            var stray = UndeclaredMember(dto);
            if (stray is { } member)
            {
                return GenericResult<List<CalculationInput>>.Failure(
                    CalculationEndpointLog.InputMemberNotDeclared(
                        logger, dto.InputAlias, dto.InputKind, member, string.Join(", ", CalculationInputKinds.ByName(dto.InputKind).Settings.Select(setting => setting.Name))));
            }

            var absent = RequiredMemberLeftOut(dto);
            if (absent is { } required)
            {
                return GenericResult<List<CalculationInput>>.Failure(
                    CalculationEndpointLog.InputMemberRequired(logger, dto.InputAlias, dto.InputKind, required));
            }

            CalculationScalarValue? scalar = null;
            if (string.Equals(dto.InputKind, "Scalar", StringComparison.OrdinalIgnoreCase))
            {
                var read = ScalarOf(dto, logger);
                if (!read.IsSuccess || read.Value is not { } value)
                {
                    return read.ToNewResult<List<CalculationInput>>();
                }

                scalar = value;
            }

            inputs.Add(new CalculationInput
            {
                Kind = CalculationInputKinds.ByName(dto.InputKind),
                DataSetName = dto.DataSetName,
                ConnectionName = dto.ConnectionName,
                ContainerPath = dto.ContainerPath,
                ScalarValue = scalar,
                InputAlias = dto.InputAlias
            });
        }

        return GenericResult<List<CalculationInput>>.Success(inputs);
    }

    // The first member the input sends that its kind does not declare. A member that is null or empty is not sent, so a client that sends a
    // read back, which carries a null for every member its kind does not use, is not refused.
    private static string? UndeclaredMember(CalculationEntityInputDto dto)
    {
        var declared = CalculationInputKinds.ByName(dto.InputKind).Settings.Select(setting => setting.Name).ToList();
        return MembersOf(dto)
            .Where(member => !string.IsNullOrEmpty(member.Value) && !declared.Contains(member.Name, StringComparer.OrdinalIgnoreCase))
            .Select(member => member.Name)
            .FirstOrDefault();
    }

    // The first member the kind declares as required that the input leaves out. A value is left out when it is null, empty or blank, unless
    // the kind declares that an empty value is a value (a String scalar's value): then only null is left out.
    private static string? RequiredMemberLeftOut(CalculationEntityInputDto dto)
    {
        var members = MembersOf(dto);
        return CalculationInputKinds.ByName(dto.InputKind).Settings
            .Where(setting => setting.IsRequired
                && IsLeftOut(members.Single(member => string.Equals(member.Name, setting.Name, StringComparison.OrdinalIgnoreCase)).Value, setting))
            .Select(setting => setting.Name)
            .FirstOrDefault();
    }

    private static bool IsLeftOut(string? value, CalculationSetting setting) => setting.AllowsEmpty ? value is null : string.IsNullOrWhiteSpace(value);

    // Every input member a request can carry, named as the kinds declare them.
    private static (string Name, string? Value)[] MembersOf(CalculationEntityInputDto dto) =>
    [
        ("dataSetName", dto.DataSetName),
        ("connectionName", dto.ConnectionName),
        ("containerPath", dto.ContainerPath),
        ("scalarValueType", dto.ScalarValueType),
        ("scalarSerializedValue", dto.ScalarSerializedValue),
    ];

    private static string OfferedKinds => string.Join(", ", CalculationInputKinds.All().Select(kind => kind.Name));

    private static string OfferedScalarTypes => string.Join(", ", ScalarValueTypes.All().Select(type => type.Name));

    // Runs after RequiredMemberLeftOut, so both members are present; the pattern says so to the compiler and, were a caller ever to skip that
    // check, answers with the same coded refusal instead of reading a null.
    private static IGenericResult<CalculationScalarValue> ScalarOf(CalculationEntityInputDto dto, ILogger logger)
    {
        if (dto is not { ScalarValueType: { } valueType, ScalarSerializedValue: { } serialized })
        {
            return GenericResult<CalculationScalarValue>.Failure(
                CalculationEndpointLog.InputMemberRequired(logger, dto.InputAlias, dto.InputKind, dto.ScalarValueType is null ? "scalarValueType" : "scalarSerializedValue"));
        }

        var type = ScalarValueTypes.ByName(valueType);
        if (type.Id == 0)
        {
            return GenericResult<CalculationScalarValue>.Failure(
                CalculationEndpointLog.ScalarValueTypeUnknown(logger, dto.InputAlias, valueType, OfferedScalarTypes));
        }

        // The type says what text is a value of it, the empty text included: a String takes it, an Int32 does not.
        if (!type.IsValidText(serialized))
        {
            return GenericResult<CalculationScalarValue>.Failure(
                CalculationEndpointLog.ScalarValueInvalid(logger, dto.InputAlias, type.Name, serialized));
        }

        return GenericResult<CalculationScalarValue>.Success(new CalculationScalarValue
        {
            ValueType = type,
            SerializedValue = serialized
        });
    }
}
