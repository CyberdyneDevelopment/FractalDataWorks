using System;
using System.Collections.Generic;
using System.Linq;

namespace Fdw.Services.Etl.Graphs;

/// <summary>
/// Where each scalar setting of the component catalogue is kept on a stored component (FDW-1396): the one table the settings
/// check, the graph's API mapping and a builder's reads all agree on.
/// </summary>
/// <remarks>
/// A list setting (<c>ColumnList</c>, <c>FieldMaps</c>, <c>SplitOutputs</c>, <c>ValueSources</c>) is not here: it is rows of a child table, told
/// apart by the setting's name on the row. A new scalar setting needs a column of its own, so it needs a binding here too; the
/// catalogue test that every scalar setting a kind lists has a binding is what notices when one is missed.
/// </remarks>
public static class ComponentSettingBindings
{
    private static readonly IReadOnlyList<ScalarSetting> All =
    [
        new("dataSet", "Text", c => c.DataSetName, (c, v) => c.DataSetName = (string?)v),
        new("dataStore", "Text", c => c.DataStoreName, (c, v) => c.DataStoreName = (string?)v),
        new("path", "Text", c => c.PathName, (c, v) => c.PathName = (string?)v),
        new("container", "Text", c => c.ContainerName, (c, v) => c.ContainerName = (string?)v),
        new("physicalPath", "Text", c => c.PhysicalPath, (c, v) => c.PhysicalPath = (string?)v),
        new("physicalName", "Text", c => c.PhysicalName, (c, v) => c.PhysicalName = (string?)v),
        new("batchSize", "Number", c => c.BatchSize, (c, v) => c.BatchSize = (int?)v),
        new("loadKind", "Text", c => c.LoadKind, (c, v) => c.LoadKind = (string?)v),
        new("truncateFirst", "Boolean", c => c.TruncateFirst, (c, v) => c.TruncateFirst = (bool?)v),
        new("joinKind", "Text", c => c.JoinKind, (c, v) => c.JoinKind = (string?)v),
        new("rightPrefix", "Text", c => c.RightPrefix, (c, v) => c.RightPrefix = (string?)v),
        new("routing", "Text", c => c.Routing, (c, v) => c.Routing = (string?)v),
        new("unmatchedOutput", "Text", c => c.UnmatchedOutput, (c, v) => c.UnmatchedOutput = (string?)v),
        new("errorOutput", "Text", c => c.ErrorOutput, (c, v) => c.ErrorOutput = (string?)v),
        new("field", "Text", c => c.FieldName, (c, v) => c.FieldName = (string?)v),
        new("target", "Text", c => c.TargetName, (c, v) => c.TargetName = (string?)v),
        new("delimiter", "Text", c => c.Delimiter, (c, v) => c.Delimiter = (string?)v),
        new("keepsEmpty", "Boolean", c => c.KeepsEmpty, (c, v) => c.KeepsEmpty = (bool?)v),
        new("script", "Text", c => c.ScriptText, (c, v) => c.ScriptText = (string?)v),
        new("maxRows", "Number", c => c.MaxRows, (c, v) => c.MaxRows = (int?)v),
        new("stage", "Number", c => c.Stage, (c, v) => c.Stage = (int?)v),
        new("variable", "Text", c => c.VariableName, (c, v) => c.VariableName = (string?)v),
        new("start", "Number", c => c.StartValue, (c, v) => c.StartValue = (int?)v),
        new("end", "Number", c => c.EndValue, (c, v) => c.EndValue = (int?)v),
        new("step", "Number", c => c.StepValue, (c, v) => c.StepValue = (int?)v),
        new("list", "Text", c => c.ListVariable, (c, v) => c.ListVariable = (string?)v),
        new("condition", "Text", c => c.ConditionExpression, (c, v) => c.ConditionExpression = (string?)v),
        new("maximum", "Number", c => c.MaxIterations, (c, v) => c.MaxIterations = (int?)v),
        new("valueType", "Text", c => c.ConstantType, (c, v) => c.ConstantType = (string?)v),
        new("value", "Text", c => c.ConstantText, (c, v) => c.ConstantText = (string?)v),
        new("pipeline", "Text", c => c.TargetPipeline, (c, v) => c.TargetPipeline = (string?)v),
        new("rule", "Text", c => c.NotificationRuleName, (c, v) => c.NotificationRuleName = (string?)v),
        new("runPoint", "Text", c => c.RunPoint, (c, v) => c.RunPoint = (string?)v),
        new("message", "Text", c => c.NotificationMessage, (c, v) => c.NotificationMessage = (string?)v),
        new("transformation", "Text", c => c.TransformationName, (c, v) => c.TransformationName = (string?)v),
        new("awaitCompletion", "Boolean", c => c.AwaitCompletion, (c, v) => c.AwaitCompletion = (bool?)v),
        new("resultVariable", "Text", c => c.ResultVariable, (c, v) => c.ResultVariable = (string?)v),
        new("failOnChildFailure", "Boolean", c => c.FailOnChildFailure, (c, v) => c.FailOnChildFailure = (bool?)v),
        new("effectiveFromColumn", "Text", c => c.EffectiveFromColumn, (c, v) => c.EffectiveFromColumn = (string?)v),
        new("effectiveToColumn", "Text", c => c.EffectiveToColumn, (c, v) => c.EffectiveToColumn = (string?)v),
        new("currentFlagColumn", "Text", c => c.CurrentFlagColumn, (c, v) => c.CurrentFlagColumn = (string?)v),
        new("insertOnlyRuleColumn", "Text", c => c.InsertOnlyRuleColumn, (c, v) => c.InsertOnlyRuleColumn = (string?)v),
        new("insertOnlyRuleValue", "Text", c => c.InsertOnlyRuleValue, (c, v) => c.InsertOnlyRuleValue = (string?)v),
        new("softDeleteMode", "Text", c => c.SoftDeleteMode, (c, v) => c.SoftDeleteMode = (string?)v),
        new("deletedFlagColumn", "Text", c => c.DeletedFlagColumn, (c, v) => c.DeletedFlagColumn = (string?)v),
        new("deletedAtColumn", "Text", c => c.DeletedAtColumn, (c, v) => c.DeletedAtColumn = (string?)v),
        new("softDeleteMarkerColumn", "Text", c => c.SoftDeleteMarkerColumn, (c, v) => c.SoftDeleteMarkerColumn = (string?)v),
        new("softDeleteMarkerValue", "Text", c => c.SoftDeleteMarkerValue, (c, v) => c.SoftDeleteMarkerValue = (string?)v),
    ];

    /// <summary>Gets every scalar setting.</summary>
    public static IReadOnlyList<ScalarSetting> Scalars => All;

    /// <summary>Finds a scalar setting by its name in the catalogue.</summary>
    /// <param name="name">The setting's name.</param>
    /// <returns>The setting, or <see langword="null"/> when no scalar setting has that name.</returns>
    public static ScalarSetting? Find(string name) =>
        All.FirstOrDefault(setting => string.Equals(setting.Name, name, StringComparison.OrdinalIgnoreCase));
}
