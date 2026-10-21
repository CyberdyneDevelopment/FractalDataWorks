using Fdw.Collections;

namespace Fdw.Services.Pipelines.Abstractions;

/// <summary>What running a stored script means (FDW-1442): it selects rows, or it changes data and answers how many rows it affected.</summary>
public interface IScriptKind : ITypeOption<int, IScriptKind>
{
    /// <summary>Gets whether running a script of this kind answers rows; false when it answers the number of rows it affected.</summary>
    bool AnswersRows { get; }
}
