using Fdw.Collections;

namespace Fdw.Services.Etl.Abstractions.AnswerRequests;

/// <summary>Base class for the options of <see cref="AnswerPolicyRequirements"/>, using the CRTP pattern.</summary>
public abstract class AnswerPolicyRequirementBase : TypeOptionBase<AnswerPolicyRequirementBase>, IAnswerPolicyRequirement
{
    /// <summary>Initializes a new instance of the <see cref="AnswerPolicyRequirementBase"/> class.</summary>
    /// <param name="name">The option name.</param>
    /// <param name="displayName">The name shown to a person.</param>
    /// <param name="description">What the option means.</param>
    /// <param name="needsPermission">Whether a call under this requirement states the permission a grant must allow.</param>
    /// <param name="outcomeWhenCovered">The name of the outcome when a stored grant that has not ended allows the call.</param>
    /// <param name="outcomeWhenNot">The name of the outcome when none does.</param>
    protected AnswerPolicyRequirementBase(string name, string displayName, string description, bool needsPermission, string outcomeWhenCovered, string outcomeWhenNot) : base(name, displayName, description, "AnswerPolicyRequirements")
    {
        NeedsPermission = needsPermission;
        OutcomeWhenCovered = outcomeWhenCovered;
        OutcomeWhenNot = outcomeWhenNot;
    }

    private string OutcomeWhenCovered { get; }

    private string OutcomeWhenNot { get; }

    /// <inheritdoc />
    public bool NeedsPermission { get; }

    /// <inheritdoc />
    public IAnswerPolicyOutcome Judge(bool grantCovers) => AnswerPolicyOutcomes.ByName(grantCovers ? OutcomeWhenCovered : OutcomeWhenNot);
}
