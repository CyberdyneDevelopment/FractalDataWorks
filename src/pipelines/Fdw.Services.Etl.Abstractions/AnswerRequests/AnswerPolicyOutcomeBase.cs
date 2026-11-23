using Fdw.Collections;

namespace Fdw.Services.Etl.Abstractions.AnswerRequests;

/// <summary>Base class for the options of <see cref="AnswerPolicyOutcomes"/>, using the CRTP pattern.</summary>
public abstract class AnswerPolicyOutcomeBase : TypeOptionBase<AnswerPolicyOutcomeBase>, IAnswerPolicyOutcome
{
    /// <summary>Initializes a new instance of the <see cref="AnswerPolicyOutcomeBase"/> class.</summary>
    /// <param name="name">The option name.</param>
    /// <param name="displayName">The name shown to a person.</param>
    /// <param name="description">What the option means.</param>
    /// <param name="callGoesOn">Whether the call goes on with no request.</param>
    /// <param name="refusesTheCall">Whether the call is refused.</param>
    /// <param name="keepsARequest">Whether the call waits for the person.</param>
    protected AnswerPolicyOutcomeBase(string name, string displayName, string description, bool callGoesOn, bool refusesTheCall, bool keepsARequest) : base(name, displayName, description, "AnswerPolicyOutcomes")
    {
        CallGoesOn = callGoesOn;
        RefusesTheCall = refusesTheCall;
        KeepsARequest = keepsARequest;
    }

    /// <inheritdoc />
    public bool CallGoesOn { get; }

    /// <inheritdoc />
    public bool RefusesTheCall { get; }

    /// <inheritdoc />
    public bool KeepsARequest { get; }
}
