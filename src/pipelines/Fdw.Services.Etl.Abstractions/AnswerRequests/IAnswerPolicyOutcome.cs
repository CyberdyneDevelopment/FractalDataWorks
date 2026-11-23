using Fdw.Collections;

namespace Fdw.Services.Etl.Abstractions.AnswerRequests;

/// <summary>What judging a call under an answer policy answers (FDW-6142): the call goes on by itself (Automatic), would be refused (WouldBeRefused), needs the person (NeedsPerson) or goes on under a stored grant (Granted).</summary>
public interface IAnswerPolicyOutcome : ITypeOption<int, IAnswerPolicyOutcome>
{
    /// <summary>Gets whether the call goes on with no request.</summary>
    bool CallGoesOn { get; }

    /// <summary>Gets whether the call is refused.</summary>
    bool RefusesTheCall { get; }

    /// <summary>Gets whether the call waits for the person, so a request is kept.</summary>
    bool KeepsARequest { get; }
}
