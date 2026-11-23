using Fdw.Collections;

namespace Fdw.Services.Etl.Abstractions.AnswerRequests;

/// <summary>The states of an answer request (FDW-6142): Keeping (written, its parts not yet), Open (the person may answer), Answered (the answer is taken and the start of the continuation is owed), Started (the continuation was started) and Expired.</summary>
public interface IAnswerRequestState : ITypeOption<int, IAnswerRequestState>
{
    /// <summary>Gets whether a request in this state may still be answered.</summary>
    bool AcceptsAnAnswer { get; }

    /// <summary>Gets whether a request in this state has an answer whose continuation has not started.</summary>
    bool OwesTheStart { get; }

    /// <summary>Gets whether the person is shown a request in this state (a request still being kept is not).</summary>
    bool IsReadable { get; }
}
