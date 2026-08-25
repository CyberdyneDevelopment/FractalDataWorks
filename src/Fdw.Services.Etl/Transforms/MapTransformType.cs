using System;
using Fdw.Data.Transformations;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Fdw.Data.DataSets;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Logging;
using Fdw.Services.Etl.Results;
using Microsoft.Extensions.Logging;
using OptionTransformTypes = Fdw.Services.Etl.Abstractions.OptionTypes.TransformTypes;

namespace Fdw.Services.Etl.Transforms;

/// <summary>
/// Transform type that maps fields from source to destination with optional renaming and type conversion.
/// </summary>
[TypeOption(typeof(OptionTransformTypes), "Map")]
public sealed class MapTransformType : TransformTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MapTransformType"/> class.
    /// </summary>
    public MapTransformType() : base(
        id: 1,
        name: "Map",
        displayName: "Field Mapping",
        description: "Maps fields from source to destination with optional renaming and type conversion",
        category: "Structure",
        modifiesStructure: true,
        canFilterRecords: false)
    {
    }

    /// <inheritdoc />
    public override async Task<IGenericResult<IDictionary<string, object?>>> Transform(
        IDictionary<string, object?> input,
        IGenericConfiguration configuration,
        ITransformContext context,
        CancellationToken cancellationToken = default)
    {
        var pipelineConfig = configuration as PipelineTransformConfiguration;
        if (pipelineConfig?.FieldMappings == null || pipelineConfig.FieldMappings.Count == 0)
        {
            // No mappings defined - pass through all fields
            return GenericResult<IDictionary<string, object?>>.Success(
                new Dictionary<string, object?>(input, StringComparer.OrdinalIgnoreCase));
        }

        var output = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (var mapping in pipelineConfig.FieldMappings)
        {
            if (!mapping.IsEnabled)
                continue;

            cancellationToken.ThrowIfCancellationRequested();
            await MapField(mapping, input, output, context, cancellationToken).ConfigureAwait(false);
        }

        return GenericResult<IDictionary<string, object?>>.Success(output);
    }

    /// <inheritdoc />
    public override IGenericResult MapSpecToConfiguration(ITransformOperationSpec spec, IGenericConfiguration target, ILogger logger)
    {
        if (target is not PipelineTransformConfiguration config)
        {
            return GenericResult.Failure(EtlLog.WrongConfigurationType(logger, spec.Name, target.GetType().Name));
        }

        if (spec.FieldMappings.Count == 0)
        {
            return GenericResult.Failure(EtlLog.MapFieldMappingsMissing(logger, spec.Name));
        }

        config.FieldMappings = spec.FieldMappings
            .Select(fm => new PipelineTransformFieldMappingConfiguration
            {
                PipelineTransformId = config.Id,
                Name = fm.DestinationField,
                SourceField = fm.SourceField,
                DestinationField = fm.DestinationField,
                TransformExpression = fm.TransformExpression,
                DefaultValue = fm.DefaultValue,
                TargetType = fm.TargetType,
                IsRequired = fm.IsRequired,
                IsEnabled = fm.IsEnabled
            })
            .ToList();

        EtlLog.TransformSpecMapped(logger, spec.Name, spec.OperationType);
        return GenericResult.Success();
    }

    private static async Task MapField(
        PipelineTransformFieldMappingConfiguration mapping,
        IDictionary<string, object?> input,
        Dictionary<string, object?> output,
        ITransformContext context,
        CancellationToken cancellationToken)
    {
        var sourceField = mapping.SourceField;
        var destinationField = mapping.DestinationField ?? sourceField;

        if (input.TryGetValue(sourceField, out var value))
        {
            // Apply default value if source is null
            if (value == null && mapping.DefaultValue != null)
            {
                value = mapping.DefaultValue;
            }

            // Apply a named field transformer (e.g. "FromUnixMilliseconds" epoch-ms -> DateTimeOffset)
            // before the TargetType coercion — see ApplyNamedTransformer for why TargetType alone can't.
            value = await ApplyTransformChain(mapping.Transforms, value, sourceField, input, context, cancellationToken).ConfigureAwait(false);

            // Apply type conversion if specified
            if (!string.IsNullOrEmpty(mapping.TargetType) && value != null)
            {
                var conversionResult = ConvertValue(value, mapping.TargetType);
                if (conversionResult.IsSuccess)
                {
                    value = conversionResult.Value;
                }
                else
                {
                    context.ReportError($"Type conversion failed for field '{sourceField}' to type '{mapping.TargetType}': {conversionResult.Messages[0].Message}", input);
                }
            }

            output[destinationField] = value;
        }
        else if (mapping.DefaultValue != null)
        {
            // Source field missing but default provided
            var defaultValue = (object?)mapping.DefaultValue;

            // Convert default value to target type if specified
            if (!string.IsNullOrEmpty(mapping.TargetType) && defaultValue != null)
            {
                var conversionResult = ConvertValue(defaultValue, mapping.TargetType);
                if (conversionResult.IsSuccess)
                {
                    defaultValue = conversionResult.Value;
                }
            }

            output[destinationField] = defaultValue;
        }
        else if (mapping.IsRequired)
        {
            // Required field missing with no default
            context.ReportError($"Required field '{sourceField}' is missing and no default value provided", input);
        }
        // If source field missing, not required, and no default, skip the field
    }

    /// <summary>
    /// Applies a field mapping's ordered transform chain (each step a <see cref="TransformationTypes"/> option such as
    /// "FromUnixMilliseconds") to a mapped value before TargetType coercion, returning the transformed
    /// value (or the original when no transformer is named / the value is null).
    /// </summary>
    /// <remarks>
    /// Why: TargetType's <c>ConvertValue</c> is a primitive cast switch (a long becomes .NET ticks, not
    /// epoch-ms) and cannot express semantic conversions; the TransformationTypes collection
    /// (FromUnixMilliseconds, ParseDateTimeOffset, Timezone, ...) is the system mechanism for them. This
    /// honours the otherwise-unread <c>TransformExpression</c> column so a Map field-mapping can name a
    /// transformer instead of producing garbage.
    /// </remarks>
    private static async Task<object?> ApplyTransformChain(
        IReadOnlyList<IFieldMappingTransform> transforms,
        object? value,
        string sourceField,
        IDictionary<string, object?> input,
        ITransformContext context,
        CancellationToken cancellationToken)
    {
        if (value == null)
        {
            return value;
        }

        // Why the chain and not a single name: a field mapping's transforms are stored as ordered
        // rows, each with its own parameters. Reading only the first name meant a Map transform could
        // say WHICH transform to run but never WHAT to run it with.
        foreach (var step in transforms.OrderBy(s => s.Ordinal))
        {
            if (TransformationTypes.ByName(step.TransformType) is not FieldTransformationBase transformer)
            {
                context.ReportError($"Field transformer '{step.TransformType}' is not a registered DataTransformerType for field '{sourceField}'", input);
                return value;
            }

            // Why this is checked instead of letting the transform cope: the previous arrangement
            // handed every transform an empty parameter bag, and each one quietly fell back to its
            // own idea of a default - BoolToString returned the empty string for every row rather
            // than either configured label. A missing required parameter is a configuration error,
            // and naming it is the only way anyone finds out. It is reported against the field, so
            // the message identifies the mapping to fix.
            var missing = transformer.ExpectedParameters
                .Where(definition => definition.IsRequired && !step.Parameters.ContainsKey(definition.Name))
                .Select(definition => definition.Name)
                .ToList();

            if (missing.Count > 0)
            {
                context.ReportError(
                    $"Field transformer '{step.TransformType}' for field '{sourceField}' is missing required parameter(s): {string.Join(", ", missing)}. Configure them on the field mapping's transform step.",
                    input);
                return value;
            }

            // Why the context is built here and passed whole: it carries both what the step was
            // configured with and the record the step may read siblings from. This is the same
            // context the dataset source mappers build - one calling convention, so a transform
            // cannot behave differently depending on which reader invoked it.
            var transformResult = await transformer.Transform(
                value,
                new TransformationContext
                {
                    OperatingDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    ExecutionTimestamp = DateTimeOffset.UtcNow,
                    CurrentRecord = new Dictionary<string, object?>(input, StringComparer.Ordinal),
                    Parameters = step.Parameters,
                    CancellationToken = cancellationToken,
                },
                cancellationToken).ConfigureAwait(false);

            if (!transformResult.IsSuccess)
            {
                context.ReportError($"Field transformer '{step.TransformType}' failed for field '{sourceField}': {transformResult.Messages[0].Message}", input);
                return value;
            }

            value = transformResult.Value;
            if (value == null)
            {
                return value;
            }
        }

        return value;
    }

    /// <summary>
    /// Converts a value to the specified target type.
    /// </summary>
    private static IGenericResult<object?> ConvertValue(object value, string targetType)
    {
        try
        {
            var normalizedType = targetType.ToLowerInvariant().Trim();
            object? convertedValue = normalizedType switch
            {
                "string" => value.ToString(),
                "int" or "int32" => ConvertToInt32(value),
                "long" or "int64" => ConvertToInt64(value),
                "decimal" => ConvertToDecimal(value),
                "double" or "float64" => ConvertToDouble(value),
                "float" or "single" or "float32" => ConvertToSingle(value),
                "bool" or "boolean" => ConvertToBoolean(value),
                "datetime" or "date" => ConvertToDateTime(value),
                "guid" or "uuid" => ConvertToGuid(value),
                "byte[]" or "binary" => ConvertToByteArray(value),
                _ => throw new NotSupportedException($"Unsupported target type: {targetType}")
            };

            return GenericResult<object?>.Success(convertedValue);
        }
        catch (Exception ex)
        {
            return GenericResult<object?>.Failure(
                EtlResultCodes.ByName("TypeConversionFailed"),
                ResultDetails.Create().With("Message", ex.Message));
        }
    }

    private static int ConvertToInt32(object value)
    {
        return value switch
        {
            int i => i,
            long l => checked((int)l),
            double d => checked((int)d),
            decimal dec => checked((int)dec),
            string s when int.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) => result,
            _ => Convert.ToInt32(value, CultureInfo.InvariantCulture)
        };
    }

    private static long ConvertToInt64(object value)
    {
        return value switch
        {
            long l => l,
            int i => i,
            double d => checked((long)d),
            decimal dec => checked((long)dec),
            string s when long.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) => result,
            _ => Convert.ToInt64(value, CultureInfo.InvariantCulture)
        };
    }

    private static decimal ConvertToDecimal(object value)
    {
        return value switch
        {
            decimal d => d,
            int i => i,
            long l => l,
            double dbl => (decimal)dbl,
            float f => (decimal)f,
            string s when decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) => result,
            _ => Convert.ToDecimal(value, CultureInfo.InvariantCulture)
        };
    }

    private static double ConvertToDouble(object value)
    {
        return value switch
        {
            double d => d,
            float f => f,
            decimal dec => (double)dec,
            int i => i,
            long l => l,
            string s when double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) => result,
            _ => Convert.ToDouble(value, CultureInfo.InvariantCulture)
        };
    }

    private static float ConvertToSingle(object value)
    {
        return value switch
        {
            float f => f,
            double d => (float)d,
            decimal dec => (float)dec,
            int i => i,
            long l => l,
            string s when float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) => result,
            _ => Convert.ToSingle(value, CultureInfo.InvariantCulture)
        };
    }

    private static bool ConvertToBoolean(object value)
    {
        return value switch
        {
            bool b => b,
            int i => i != 0,
            long l => l != 0,
            string s when bool.TryParse(s, out var result) => result,
            string s when s.Equals("1", StringComparison.Ordinal) => true,
            string s when s.Equals("0", StringComparison.Ordinal) => false,
            string s when s.Equals("yes", StringComparison.OrdinalIgnoreCase) => true,
            string s when s.Equals("no", StringComparison.OrdinalIgnoreCase) => false,
            _ => Convert.ToBoolean(value, CultureInfo.InvariantCulture)
        };
    }

    private static DateTime ConvertToDateTime(object value)
    {
        return value switch
        {
            DateTime dt => dt,
            DateTimeOffset dto => dto.UtcDateTime,
            // Why: the exact 'Z'-suffixed ISO-8601 arm MUST be checked before the generic TryParse
            // arm below — a generic DateTime.TryParse with DateTimeStyles.None silently converts a
            // UTC 'Z' timestamp to HOST-LOCAL time (Kind=Local), which is non-deterministic across
            // machines/timezones. AdjustToUniversal here forces a stable UTC result.
            string s when DateTime.TryParseExact(s, "yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var utc) => utc,
            string s when DateTime.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) => date,
            string s when DateTime.TryParseExact(s, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var iso) => iso,
            // Why: generic ISO-8601 fallback is also forced deterministic — AssumeUniversal treats an
            // offset-less string as already UTC, AdjustToUniversal converts an explicit offset/'Z'
            // string to UTC — so every parsed value is a stable UTC DateTime regardless of host
            // timezone (never DateTimeStyles.None, which yields host-local, non-deterministic Kind).
            string s when DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var result) => result,
            long ticks when ticks > 0 => new DateTime(ticks, DateTimeKind.Utc),
            _ => Convert.ToDateTime(value, CultureInfo.InvariantCulture)
        };
    }

    private static Guid ConvertToGuid(object value)
    {
        return value switch
        {
            Guid g => g,
            string s when Guid.TryParse(s, out var result) => result,
            byte[] bytes when bytes.Length == 16 => new Guid(bytes),
            _ => throw new InvalidCastException($"Cannot convert {value.GetType().Name} to Guid")
        };
    }

    private static byte[] ConvertToByteArray(object value)
    {
        return value switch
        {
            byte[] bytes => bytes,
            string s => Convert.FromBase64String(s),
            _ => throw new InvalidCastException($"Cannot convert {value.GetType().Name} to byte[]")
        };
    }
}
