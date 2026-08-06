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
/// Parses a string input to decimal using the specified culture.
/// Returns null when the input cannot be parsed.
/// </summary>
[TypeOption(typeof(DataTransformerTypes), "ParseDecimal")]
public sealed class ParseDecimalFieldTransformer : FieldTransformerTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ParseDecimalFieldTransformer"/> class.
    /// </summary>
    public ParseDecimalFieldTransformer()
        : base(
            id: 200,
            name: "ParseDecimal",
            displayName: "Parse Decimal",
            description: "Parses a string value to decimal using the specified culture. Returns null on parse failure.",
            category: "Numeric",
            supportsBatching: true,
            new OperationParameterDefinition
            {
                Name = "culture",
                Kind = "Scalar",
                IsRequired = false,
                DisplayName = "Culture",
                HelpText = "Culture name for parsing (e.g., 'en-US'). Defaults to InvariantCulture."
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
        if (input is null)
        {
            return Task.FromResult(GenericResult<object?>.Success(null));
        }

        var text = input.ToString();
        if (text is null)
        {
            return Task.FromResult(GenericResult<object?>.Success(null));
        }

        var culture = CultureInfo.InvariantCulture;
        if (parameters.TryGetValue("culture", out var cultureName)
            && !string.IsNullOrWhiteSpace(cultureName))
        {
            culture = CultureInfo.GetCultureInfo(cultureName);
        }

        if (decimal.TryParse(text, NumberStyles.Any, culture, out var result))
        {
            return Task.FromResult(GenericResult<object?>.Success(result));
        }

        return Task.FromResult(GenericResult<object?>.Success(null));
    }

    /// <inheritdoc/>
    public override Task<IGenericResult<IReadOnlyList<object?>>> ExecuteBatch(
        IReadOnlyList<object?> inputs,
        IReadOnlyDictionary<string, string> parameters,
        FieldTransformContext context,
        CancellationToken cancellationToken = default)
    {
        var culture = CultureInfo.InvariantCulture;
        if (parameters.TryGetValue("culture", out var cultureName)
            && !string.IsNullOrWhiteSpace(cultureName))
        {
            culture = CultureInfo.GetCultureInfo(cultureName);
        }

        var results = new List<object?>(inputs.Count);
        foreach (var input in inputs)
        {
            if (input is null)
            {
                results.Add(null);
                continue;
            }

            var text = input.ToString();
            if (text is not null && decimal.TryParse(text, NumberStyles.Any, culture, out var parsed))
            {
                results.Add(parsed);
            }
            else
            {
                results.Add(null);
            }
        }

        return Task.FromResult(GenericResult<IReadOnlyList<object?>>.Success(results));
    }
}
