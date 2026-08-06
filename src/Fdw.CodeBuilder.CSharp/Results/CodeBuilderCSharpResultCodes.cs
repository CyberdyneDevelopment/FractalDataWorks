using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.CodeBuilder.CSharp.Results;

/// <summary>
/// TypeCollection for CodeBuilder CSharp result codes.
/// Codes use categorized numbers (Id == EventId == number) with the "CODEBUILDER" prefix.
/// </summary>
[TypeCollection(typeof(CodeBuilderCSharpResultCodeBase), typeof(IResultCode), typeof(CodeBuilderCSharpResultCodes))]
public abstract partial class CodeBuilderCSharpResultCodes : TypeCollectionBase<CodeBuilderCSharpResultCodeBase, IResultCode>
{
}
