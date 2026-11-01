using Fdw.Commands.Data.Upserts;

namespace Fdw.Services.Etl.Pipelines.Loads.Builders;

/// <summary>
/// Reads the versioning a load row states into the command layer's versioning options (FDW-1109).
/// </summary>
internal static class LoadVersioning
{
    /// <summary>Builds the versioning options of a load row.</summary>
    /// <param name="row">The load row.</param>
    /// <returns>The options, or <see langword="null"/> when the row states no versioning; the validator has already refused a half-stated one.</returns>
    public static VersioningOptions? For(PipelineLoadStepConfiguration row)
        => string.IsNullOrWhiteSpace(row.EffectiveFromColumn) || string.IsNullOrWhiteSpace(row.EffectiveToColumn)
            ? null
            : new VersioningOptions(row.EffectiveFromColumn, row.EffectiveToColumn)
            {
                CurrentFlagColumn = row.CurrentFlagColumn,
                InsertOnlyRule = string.IsNullOrWhiteSpace(row.InsertOnlyRuleColumn) || row.InsertOnlyRuleValue is null
                    ? null
                    : new InsertOnlyRule(row.InsertOnlyRuleColumn, row.InsertOnlyRuleValue),
            };
}
