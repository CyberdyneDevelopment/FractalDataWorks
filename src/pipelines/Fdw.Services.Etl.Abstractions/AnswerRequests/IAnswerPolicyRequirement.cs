using Fdw.Collections;

namespace Fdw.Services.Etl.Abstractions.AnswerRequests;

/// <summary>What an answer policy states a call requires (FDW-6142): Automatic (it goes on with no request), Refused, NeedsPerson (the person always answers) or GrantOrPerson (a stored grant that allows a stated permission covers it, otherwise the person answers).</summary>
public interface IAnswerPolicyRequirement : ITypeOption<int, IAnswerPolicyRequirement>
{
    /// <summary>Gets whether a call under this requirement states the permission a stored grant must allow.</summary>
    bool NeedsPermission { get; }

    /// <summary>Judges a call under this requirement.</summary>
    /// <param name="grantCovers">Whether a stored grant that has not ended allows the call's permission.</param>
    /// <returns>What the call is judged to be.</returns>
    IAnswerPolicyOutcome Judge(bool grantCovers);
}
