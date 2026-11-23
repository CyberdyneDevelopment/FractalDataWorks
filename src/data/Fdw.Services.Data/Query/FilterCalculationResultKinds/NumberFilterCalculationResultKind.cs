using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using Fdw.Collections.Attributes;

namespace Fdw.Web.RestEndpoints.Query;

/// <summary>A calculation that yields a number.</summary>
[TypeOption(typeof(FilterCalculationResultKinds), "Number")]
[ExcludeFromCodeCoverage]
public sealed class NumberFilterCalculationResultKind : FilterCalculationResultKindBase
{
    private static readonly Type[] Fields = [typeof(byte), typeof(short), typeof(int), typeof(long), typeof(decimal), typeof(double), typeof(float)];

    /// <summary>Initializes a new instance of the <see cref="NumberFilterCalculationResultKind"/> class.</summary>
    public NumberFilterCalculationResultKind()
        : base("Number")
    {
    }

    /// <inheritdoc />
    public override Type ValueType => typeof(decimal);

    /// <inheritdoc />
    public override string Word => "a number";

    /// <inheritdoc />
    public override IReadOnlyList<Type> FieldTypes => Fields;

    /// <inheritdoc />
    public override JsonElement Literal(object value) => JsonSerializer.SerializeToElement((decimal)value);
}
