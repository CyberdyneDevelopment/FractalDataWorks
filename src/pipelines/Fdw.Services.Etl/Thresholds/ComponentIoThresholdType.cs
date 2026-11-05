using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Operations.Abstractions.Thresholds;

namespace Fdw.Services.Etl.Thresholds;

/// <summary>
/// The threshold type of a pipeline component's input and output (FDW-3865): what a component reports when it finishes is the rows that came in, the rows that left,
/// the rows it rejected and how long it ran. A rule of a threshold of this type is criteria over those four fields and a consequence from the escalation types.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ThresholdTypes), "ComponentIo")]
public sealed class ComponentIoThresholdType : ThresholdTypeBase
{
    /// <summary>The subject kind of a pipeline component.</summary>
    public const string Subject = "Component";

    /// <summary>The name of the rows-in field.</summary>
    public const string RowsIn = "rowsIn";

    /// <summary>The name of the rows-out field.</summary>
    public const string RowsOut = "rowsOut";

    /// <summary>The name of the rejected-rows field.</summary>
    public const string Rejected = "rejected";

    /// <summary>The name of the run-time field.</summary>
    public const string RunTime = "runTime";

    private static readonly IReadOnlyList<ThresholdField> Reported =
    [
        new ThresholdField(RowsIn, typeof(long), "rows", "The rows that came in on the component's inputs."),
        new ThresholdField(RowsOut, typeof(long), "rows", "The rows that left on the component's outputs, not counting its error outputs."),
        new ThresholdField(Rejected, typeof(long), "rows", "The rows that left through the component's error outputs."),
        new ThresholdField(RunTime, typeof(TimeSpan), "time", "How long the component ran, from its start to its end."),
    ];

    /// <summary>Initializes a new instance of the <see cref="ComponentIoThresholdType"/> class.</summary>
    public ComponentIoThresholdType()
        : base("ComponentIo", "What a pipeline component took in, passed on, rejected and how long it took, judged when it finishes.")
    {
    }

    /// <inheritdoc/>
    public override string SubjectKind => Subject;

    /// <inheritdoc/>
    public override IReadOnlyList<ThresholdField> Fields => Reported;
}
