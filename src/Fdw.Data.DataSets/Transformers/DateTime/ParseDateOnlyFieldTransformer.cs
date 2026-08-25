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
/// Parses a string value to <see cref="DateOnly"/>.
/// When a format is provided, uses <see cref="DateOnly.TryParseExact(string, string, IFormatProvider, DateTimeStyles, out DateOnly)"/>;
/// otherwise uses <see cref="DateOnly.TryParse(string, IFormatProvider, DateTimeStyles, out DateOnly)"/>.
/// Returns null on parse failure.
/// </summary>
[TypeOption(typeof(DataTransformerTypes), "ParseDateOnly")]
public sealed class ParseDateOnlyFieldTransformer : FieldTransformerTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ParseDateOnlyFieldTransformer"/> class.
    /// </summary>
    public ParseDateOnlyFieldTransformer()
        : base(
            id: 102,
            name: "ParseDateOnly",
            displayName: "Parse DateOnly",
            description: "Parses a string value to DateOnly. Returns null on parse failure.",
            category: "DateTime",
            supportsBatching: true,
            new OperationParameterDefinition
            {
                Name = "format",
                Kind = "Scalar",
                IsRequired = false,
                DisplayName = "Format",
                HelpText = "Exact date format string (e.g., 'yyyy-MM-dd'). When omitted, standard parsing is used.",
            })
    {
    }

    /// <inheritdoc/>
    public override Task<IGenericResult<object?>> Transform(
        object? input,
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
                $"ParseDateOnly expects a string input but received '{input.GetType().Name}'.");
        }

        context.Parameters.TryGetValue("format", out var format);

        return Task.FromResult<IGenericResult<object?>>(
            TryParseDateOnly(s, format, out var result)
                ? GenericResult<object?>.Success(result)
                : GenericResult<object?>.Success(null));
    }



    private static bool TryParseDateOnly(string value, string? format, out DateOnly result)
    {
        if (!string.IsNullOrWhiteSpace(format))
        {
            return DateOnly.TryParseExact(
                value, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
        }

        return DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
    }
}
