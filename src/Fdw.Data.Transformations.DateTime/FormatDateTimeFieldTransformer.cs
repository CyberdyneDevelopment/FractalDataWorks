using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions;

namespace Fdw.Data.Transformations;

/// <summary>
/// Formats a date or time value as text by a format string, in the invariant culture (FDW-1957): how a file name is built from a date.
/// </summary>
[TypeOption(typeof(TransformationTypes), "FormatDateTime")]
public sealed class FormatDateTimeFieldTransformer : FieldTransformationBase
{
    /// <summary>Initializes a new instance of the <see cref="FormatDateTimeFieldTransformer"/> class.</summary>
    public FormatDateTimeFieldTransformer()
        : base(
            name: "FormatDateTime",
            displayName: "Format DateTime",
            description: "Formats a date or time value as text by a format string (for example yyyyMMdd), in the invariant culture.",
            category: "DateTime",
            supportsBatching: true,
            new OperationParameterDefinition
            {
                Name = "format",
                Kind = "Scalar",
                IsRequired = true,
                DisplayName = "Format",
                HelpText = "The .NET date and time format string (for example yyyyMMdd or yyyy-MM-ddTHH:mm:ss)."
            })
    {
    }

    /// <inheritdoc/>
    public override Task<IGenericResult<object?>> Transform(
        object? input,
        TransformationContext context,
        CancellationToken cancellationToken = default)
    {
        if (input is null)
        {
            return Task.FromResult(GenericResult<object?>.Success(null));
        }

        if (!context.Parameters.TryGetValue("format", out var format) || string.IsNullOrEmpty(format))
        {
            throw new InvalidOperationException("FormatDateTime needs the parameter 'format'.");
        }

        if (input is not (DateTimeOffset or DateTime or DateOnly or TimeOnly))
        {
            throw new InvalidOperationException(
                $"FormatDateTime expects a date or time input but received '{input.GetType().Name}'; parse a text value first.");
        }

        return Task.FromResult(GenericResult<object?>.Success(((IFormattable)input).ToString(format, CultureInfo.InvariantCulture)));
    }
}
