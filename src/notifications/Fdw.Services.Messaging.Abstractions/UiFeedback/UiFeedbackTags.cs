using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Messaging;

/// <summary>
/// The tags a UI comment can carry: Usability Issue, Feature Request and Calculation Issue (FDW-2142). A comment carries any number, or none.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(UiFeedbackTagBase), typeof(IUiFeedbackTag), typeof(UiFeedbackTags))]
public abstract partial class UiFeedbackTags : TypeCollectionBase<UiFeedbackTagBase, IUiFeedbackTag>
{
    // DO NOT IMPLEMENT BY HAND! Source generator populates ByName/ById/All/NotFound.
}
