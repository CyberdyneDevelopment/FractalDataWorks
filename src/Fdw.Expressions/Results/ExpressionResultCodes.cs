using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Expressions.Results;

/// <summary>
/// TypeCollection for Expression domain result codes.
/// Codes use the categorized-number scheme (Code == "EXPR-{number}", Id == EventId == number).
/// </summary>
[TypeCollection(typeof(ExpressionResultCodeBase), typeof(IResultCode), typeof(ExpressionResultCodes))]
public abstract partial class ExpressionResultCodes : TypeCollectionBase<ExpressionResultCodeBase, IResultCode>
{
}