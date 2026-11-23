using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.AnswerRequests;

/// <summary>The kinds of answer request a run keeps for its person (FDW-6142): a Question the person answers in words, and a Choice the person answers with one of the options the run showed.</summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(AnswerRequestKindBase), typeof(IAnswerRequestKind), typeof(AnswerRequestKinds))]
public abstract partial class AnswerRequestKinds : TypeCollectionBase<AnswerRequestKindBase, IAnswerRequestKind>
{
    // DO NOT IMPLEMENT BY HAND! Source generator populates ByName/ById/All/NotFound.
}
