using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using Fdw.Collections.Attributes;

namespace Fdw.Web.RestEndpoints.Query;

/// <summary>A calculation that yields a date-time.</summary>
[TypeOption(typeof(FilterCalculationResultKinds), "Moment")]
[ExcludeFromCodeCoverage]
public sealed class MomentFilterCalculationResultKind : FilterCalculationResultKindBase
{
    private static readonly Type[] Fields = [typeof(DateTime), typeof(DateTimeOffset)];

    /// <summary>Initializes a new instance of the <see cref="MomentFilterCalculationResultKind"/> class.</summary>
    public MomentFilterCalculationResultKind()
        : base("Moment")
    {
    }

    /// <inheritdoc />
    public override Type ValueType => typeof(DateTimeOffset);

    /// <inheritdoc />
    public override string Word => "a date-time";

    /// <inheritdoc />
    public override IReadOnlyList<Type> FieldTypes => Fields;

    /// <inheritdoc />
    public override JsonElement Literal(object value) => JsonSerializer.SerializeToElement(((DateTimeOffset)value).ToString("O", CultureInfo.InvariantCulture));
}
