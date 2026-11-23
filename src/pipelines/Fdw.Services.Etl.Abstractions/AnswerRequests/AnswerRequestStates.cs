using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.AnswerRequests;

/// <summary>The states of an answer request (FDW-6142): Keeping (written, its parts not yet), Open (the person may answer), Answered (the answer is taken and the start of the continuation is owed), Started (the continuation was started) and Expired.</summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(AnswerRequestStateBase), typeof(IAnswerRequestState), typeof(AnswerRequestStates))]
public abstract partial class AnswerRequestStates : TypeCollectionBase<AnswerRequestStateBase, IAnswerRequestState>
{
    // DO NOT IMPLEMENT BY HAND! Source generator populates ByName/ById/All/NotFound.
}
