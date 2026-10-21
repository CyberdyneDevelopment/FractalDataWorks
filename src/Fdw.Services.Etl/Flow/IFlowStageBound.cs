namespace Fdw.Services.Etl.Flow;

/// <summary>
/// A component that is recorded under the record of its own stage (FDW-2060): an action or a loop. The step that runs the level gives it the level's
/// stage records and its stage number before the graph starts.
/// </summary>
internal interface IFlowStageBound
{
    /// <summary>Gives the component the records of its level and its own stage.</summary>
    /// <param name="records">The stage records of the level.</param>
    /// <param name="stage">The component's stage number; 0 for none.</param>
    void BindStages(FlowStageRecords records, int stage);
}
