using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Messaging;

/// <summary>
/// The urgencies a UI comment can carry: Low, Normal, High and Security Issue (FDW-2142). A comment states one, and there is no default.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(UiFeedbackUrgencyBase), typeof(IUiFeedbackUrgency), typeof(UiFeedbackUrgencies))]
public abstract partial class UiFeedbackUrgencies : TypeCollectionBase<UiFeedbackUrgencyBase, IUiFeedbackUrgency>
{
    // DO NOT IMPLEMENT BY HAND! Source generator populates ByName/ById/All/NotFound.
}
