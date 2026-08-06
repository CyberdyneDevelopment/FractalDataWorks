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
/// Parses a string value to <see cref="DateTimeOffset"/> using <see cref="DateTimeStyles.AssumeUniversal"/>.
/// Returns null on parse failure rather than an error.
/// </summary>
[TypeOption(typeof(DataTransformerTypes), "ParseDateTimeOffset")]
public sealed class ParseDateTimeOffsetFieldTransformer : FieldTransformerTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ParseDateTimeOffsetFieldTransformer"/> class.
    /// </summary>
    public ParseDateTimeOffsetFieldTransformer()
        : base(
            id: 101,
            name: "ParseDateTimeOffset",
            displayName: "Parse DateTimeOffset",
            description: "Parses a string value to DateTimeOffset using AssumeUniversal. Returns null on parse failure.",
            category: "DateTime",
            supportsBatching: true,
            new OperationParameterDefinition
            {
                Name = "culture",
                Kind = "Scalar",
                IsRequired = false,
                DisplayName = "Culture",
                HelpText = "Culture name for parsing (e.g., 'en-US'). Defaults to InvariantCulture.",
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

        if (input is not string s)
        {
            throw new InvalidOperationException(
                $"ParseDateTimeOffset expects a string input but received '{input.GetType().Name}'.");
        }

        var culture = ResolveCulture(parameters);

        return Task.FromResult<IGenericResult<object?>>(
            DateTimeOffset.TryParse(s, culture, DateTimeStyles.AssumeUniversal, out var result)
                ? GenericResult<object?>.Success(result)
                : GenericResult<object?>.Success(null));
    }

    /// <inheritdoc/>
    public override Task<IGenericResult<IReadOnlyList<object?>>> ExecuteBatch(
        IReadOnlyList<object?> inputs,
        IReadOnlyDictionary<string, string> parameters,
        FieldTransformContext context,
        CancellationToken cancellationToken = default)
    {
        var culture = ResolveCulture(parameters);
        var results = new List<object?>(inputs.Count);

        foreach (var input in inputs)
        {
            if (input is null)
            {
                results.Add(null);
                continue;
            }

            if (input is not string s)
            {
                throw new InvalidOperationException(
                    $"ParseDateTimeOffset expects a string input but received '{input.GetType().Name}'.");
            }

            results.Add(
                DateTimeOffset.TryParse(s, culture, DateTimeStyles.AssumeUniversal, out var parsed)
                    ? parsed
                    : null);
        }

        return Task.FromResult(GenericResult<IReadOnlyList<object?>>.Success(results));
    }

    private static CultureInfo ResolveCulture(IReadOnlyDictionary<string, string> parameters)
    {
        if (parameters.TryGetValue("culture", out var cultureName) &&
            !string.IsNullOrWhiteSpace(cultureName))
        {
            return CultureInfo.GetCultureInfo(cultureName);
        }

        return CultureInfo.InvariantCulture;
    }
}
