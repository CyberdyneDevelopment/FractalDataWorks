using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Execution;

/// <summary>A DateTime, read as UTC whatever kind it states.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RunTimeShapes), "Time")]
public sealed class TimeRunTimeShape : RunTimeShapeBase
{
    /// <summary>Initializes a new instance of the <see cref="TimeRunTimeShape"/> class.</summary>
    public TimeRunTimeShape()
        : base("Time")
    {
    }

    /// <inheritdoc />
    public override bool Matches(object? value) => value is DateTime;

    /// <inheritdoc />
    public override DateTimeOffset ToOffset(object value) => new(DateTime.SpecifyKind((DateTime)value, DateTimeKind.Utc));
}
