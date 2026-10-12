using System.Collections.Generic;

namespace Fdw.Sql.Commands.Project.Translators;

/// <summary>Aggregate project-level statistics returned from GetProjectInfo.</summary>
public sealed record SqlProjectSummary(
    string ProjectPath,
    int ScriptCount,
    int TotalObjectCount,
    IReadOnlyDictionary<string, int> ObjectCountsByKind);
