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
/// Parses a string input to int. Optionally trims specified characters before parsing.
/// Returns null when the input cannot be parsed.
/// </summary>
[TypeOption(typeof(DataTransformerTypes), "ParseInt")]
public sealed class ParseIntFieldTransformer : FieldTransformerTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ParseIntFieldTransformer"/> class.
    /// </summary>
    public ParseIntFieldTransformer()
        : base(
            id: 201,
            name: "ParseInt",
            displayName: "Parse Int",
            description: "Parses a string value to int. Optional characters are trimmed before parsing. Returns null on parse failure.",
            category: "Numeric",
            supportsBatching: true,
            new OperationParameterDefinition
            {
                Name = "trimChars",
                Kind = "Scalar",
                IsRequired = false,
                DisplayName = "Trim Characters",
                HelpText = "Characters to trim from the input before parsing (e.g., '$,%')."
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

        if (parameters.TryGetValue("trimChars", out var trimChars)
            && !string.IsNullOrEmpty(trimChars))
        {
            text = text.Trim(trimChars.ToCharArray());
        }

        if (int.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
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
        char[]? trimArray = null;
        if (parameters.TryGetValue("trimChars", out var trimChars)
            && !string.IsNullOrEmpty(trimChars))
        {
            trimArray = trimChars.ToCharArray();
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
            if (text is null)
            {
                results.Add(null);
                continue;
            }

            if (trimArray is not null)
            {
                text = text.Trim(trimArray);
            }

            if (int.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
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
