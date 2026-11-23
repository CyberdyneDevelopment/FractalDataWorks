using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using Fdw.Collections.Attributes;

namespace Fdw.Web.RestEndpoints.Query;

/// <summary>A JSON string written yyyy-MM-dd, read as a date.</summary>
[TypeOption(typeof(JsonInputKinds), "Date", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DateJsonInputKind : JsonInputKindBase
{
    /// <summary>Initializes a new instance of the <see cref="DateJsonInputKind"/> class.</summary>
    public DateJsonInputKind()
        : base("Date", JsonValueKind.String)
    {
    }

    /// <inheritdoc />
    public override object? Read(JsonElement value) => DateOnly.TryParseExact(value.GetString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date : (DateOnly?)null;
}
