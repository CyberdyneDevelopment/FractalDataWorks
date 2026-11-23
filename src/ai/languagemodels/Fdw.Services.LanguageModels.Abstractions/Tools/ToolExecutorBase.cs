using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>
/// Base class for the options of <see cref="ToolExecutors"/>: who runs a tool a model may call.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class ToolExecutorBase : TypeOptionBase<ToolExecutorBase>, IToolExecutor
{
    /// <summary>Initializes a new instance of the <see cref="ToolExecutorBase"/> class.</summary>
    /// <param name="name">The name of the option.</param>
    /// <param name="runsInHost">Whether the host runs the tool and sends back its result.</param>
    protected ToolExecutorBase(string name, bool runsInHost) : base(name)
    {
        RunsInHost = runsInHost;
    }

    /// <inheritdoc />
    public bool RunsInHost { get; }
}
