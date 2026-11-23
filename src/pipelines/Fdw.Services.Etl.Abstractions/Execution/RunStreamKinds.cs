namespace Fdw.Services.Etl.Abstractions.Execution;

/// <summary>The kinds of piece a run gives the person it is for (FDW-6140); the stream's column allows no other text.</summary>
public static class RunStreamKinds
{
    /// <summary>A text a step wrote.</summary>
    public const string Text = "Text";

    /// <summary>A step began.</summary>
    public const string StepStarted = "StepStarted";

    /// <summary>A step ended; the text says how.</summary>
    public const string StepEnded = "StepEnded";

    /// <summary>A step did not run because none of the lines that lead to it fired (FLOW-21036).</summary>
    public const string StepSkipped = "StepSkipped";
}
