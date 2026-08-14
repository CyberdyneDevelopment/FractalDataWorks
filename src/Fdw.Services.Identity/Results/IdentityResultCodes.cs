using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Identity.Results;

/// <summary>
/// TypeCollection for managed identity result codes.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(IdentityResultCodeBase), typeof(IResultCode), typeof(IdentityResultCodes))]
public abstract partial class IdentityResultCodes : TypeCollectionBase<IdentityResultCodeBase, IResultCode>
{
}
