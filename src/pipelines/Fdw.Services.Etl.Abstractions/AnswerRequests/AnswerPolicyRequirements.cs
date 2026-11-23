using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.AnswerRequests;

/// <summary>What an answer policy states a call requires (FDW-6142): Automatic (it goes on with no request), Refused, NeedsPerson (the person always answers) or GrantOrPerson (a stored grant that allows a stated permission covers it, otherwise the person answers).</summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(AnswerPolicyRequirementBase), typeof(IAnswerPolicyRequirement), typeof(AnswerPolicyRequirements))]
public abstract partial class AnswerPolicyRequirements : TypeCollectionBase<AnswerPolicyRequirementBase, IAnswerPolicyRequirement>
{
    // DO NOT IMPLEMENT BY HAND! Source generator populates ByName/ById/All/NotFound.
}
