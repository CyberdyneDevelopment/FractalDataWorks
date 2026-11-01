using Fdw.Collections;

namespace Fdw.Services.Pipelines.Abstractions;

/// <summary>The base of the kinds of stored script.</summary>
public abstract class ScriptKindBase : TypeOptionBase<ScriptKindBase>, IScriptKind
{
    /// <summary>Initializes a new instance of the <see cref="ScriptKindBase"/> class.</summary>
    /// <param name="name">The kind's name, which a stored script names it by.</param>
    /// <param name="description">What a script of this kind does.</param>
    /// <param name="answersRows">Whether running it answers rows.</param>
    protected ScriptKindBase(string name, string description, bool answersRows)
        : base(name, name, description, "ScriptKinds")
    {
        AnswersRows = answersRows;
    }

    /// <inheritdoc />
    public bool AnswersRows { get; }
}
