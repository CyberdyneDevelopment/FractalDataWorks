using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>
/// The signs, words and forms of the pipeline's condition language (FDW-1395). The console reads it to show what a filter
/// expression may say; the listing's display name is the form as it is typed and the description carries a worked example.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(PredicateOperatorBase), typeof(IPredicateOperator), typeof(PredicateOperators))]
public abstract partial class PredicateOperators : TypeCollectionBase<PredicateOperatorBase, IPredicateOperator>
{
    // DO NOT IMPLEMENT BY HAND! Source generator populates ByName/ById/All/NotFound.
}
