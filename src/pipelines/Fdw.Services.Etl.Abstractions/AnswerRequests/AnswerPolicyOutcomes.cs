using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.AnswerRequests;

/// <summary>What judging a call under an answer policy answers (FDW-6142): the call goes on by itself (Automatic), would be refused (WouldBeRefused), needs the person (NeedsPerson) or goes on under a stored grant (Granted).</summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(AnswerPolicyOutcomeBase), typeof(IAnswerPolicyOutcome), typeof(AnswerPolicyOutcomes))]
public abstract partial class AnswerPolicyOutcomes : TypeCollectionBase<AnswerPolicyOutcomeBase, IAnswerPolicyOutcome>
{
    // DO NOT IMPLEMENT BY HAND! Source generator populates ByName/ById/All/NotFound.
}
