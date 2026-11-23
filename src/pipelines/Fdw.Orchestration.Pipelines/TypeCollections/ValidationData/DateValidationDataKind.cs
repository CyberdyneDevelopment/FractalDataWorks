using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;

namespace Fdw.Orchestration.Pipelines.TypeCollections.ValidationData;

/// <summary>A date or a date and time.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ValidationDataKinds), "Date")]
public sealed class DateValidationDataKind : ValidationDataKindBase
{
    /// <summary>Initializes a new instance of the <see cref="DateValidationDataKind"/> class.</summary>
    public DateValidationDataKind()
        : base("Date", ["DATE", "DATETIME"])
    {
    }

    /// <inheritdoc />
    public override bool Accepts(string text) => DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
}
