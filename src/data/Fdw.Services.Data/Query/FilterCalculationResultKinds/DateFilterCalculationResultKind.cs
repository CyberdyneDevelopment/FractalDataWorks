using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using Fdw.Collections.Attributes;

namespace Fdw.Web.RestEndpoints.Query;

/// <summary>A calculation that yields a date.</summary>
[TypeOption(typeof(FilterCalculationResultKinds), "Date")]
[ExcludeFromCodeCoverage]
public sealed class DateFilterCalculationResultKind : FilterCalculationResultKindBase
{
    private static readonly Type[] Fields = [typeof(DateOnly), typeof(DateTime)];

    /// <summary>Initializes a new instance of the <see cref="DateFilterCalculationResultKind"/> class.</summary>
    public DateFilterCalculationResultKind()
        : base("Date")
    {
    }

    /// <inheritdoc />
    public override Type ValueType => typeof(DateOnly);

    /// <inheritdoc />
    public override string Word => "a date";

    /// <inheritdoc />
    public override IReadOnlyList<Type> FieldTypes => Fields;

    /// <inheritdoc />
    public override JsonElement Literal(object value) => JsonSerializer.SerializeToElement(((DateOnly)value).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
}
