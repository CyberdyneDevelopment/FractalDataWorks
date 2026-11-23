using Fdw.Collections;

namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>
/// One option of <see cref="ToolExecutors"/>: who runs a tool a model may call.
/// </summary>
public interface IToolExecutor : ITypeOption<int, ToolExecutorBase>
{

    /// <summary>Gets a value indicating whether the host runs the tool and sends back its result.</summary>
    bool RunsInHost { get; }
}
