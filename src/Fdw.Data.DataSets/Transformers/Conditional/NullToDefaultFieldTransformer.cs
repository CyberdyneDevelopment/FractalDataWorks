using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions;

namespace Fdw.Data.DataSets;

/// <summary>
/// When the input is null or empty string, returns a typed default value parsed from configuration.
/// </summary>
[TypeOption(typeof(DataTransformerTypes), "NullToDefault")]
public sealed class NullToDefaultFieldTransformer : FieldTransformerTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NullToDefaultFieldTransformer"/> class.
    /// </summary>
    public NullToDefaultFieldTransformer()
        : base(
            id: 600,
            name: "NullToDefault",
            displayName: "Null to Default",
            description: "When the input is null or empty string, returns a typed default value.",
            category: "Conditional",
            supportsBatching: true,
            new OperationParameterDefinition
            {
                Name = "defaultValue",
                Kind = "Scalar",
                IsRequired = true,
                DisplayName = "Default Value",
                HelpText = "The default value to return when the input is null or empty."
            },
            new OperationParameterDefinition
            {
                Name = "type",
                Kind = "Scalar",
                IsRequired = true,
                DisplayName = "Type",
                HelpText = "The type to parse the default value as (String, Decimal, Int, Bool)."
            })
    {
    }

    /// <inheritdoc/>
    public override Task<IGenericResult<object?>> Execute(
        object? input,
        IReadOnlyDictionary<string, string> parameters,
        FieldTransformContext context,
        CancellationToken cancellationToken = default)
    {
        var isNullOrEmpty = input is null
            || (input is string s && string.Equals(s, string.Empty, StringComparison.Ordinal));

        if (!isNullOrEmpty)
        {
            return Task.FromResult(GenericResult<object?>.Success(input));
        }

        if (!parameters.TryGetValue("defaultValue", out var defaultValue)
            || !parameters.TryGetValue("type", out var type))
        {
            return Task.FromResult(GenericResult<object?>.Success(null));
        }

        var parsed = ParseDefault(defaultValue, type);

        return Task.FromResult(GenericResult<object?>.Success(parsed));
    }

    private static object? ParseDefault(string defaultValue, string type)
    {
        if (string.Equals(type, "String", StringComparison.OrdinalIgnoreCase))
        {
            return defaultValue;
        }

        if (string.Equals(type, "Decimal", StringComparison.OrdinalIgnoreCase))
        {
            return decimal.TryParse(defaultValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var d)
                ? (object)d
                : null;
        }

        if (string.Equals(type, "Int", StringComparison.OrdinalIgnoreCase))
        {
            return int.TryParse(defaultValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i)
                ? (object)i
                : null;
        }

        if (string.Equals(type, "Bool", StringComparison.OrdinalIgnoreCase))
        {
            return bool.TryParse(defaultValue, out var b)
                ? (object)b
                : null;
        }

        return null;
    }
}
