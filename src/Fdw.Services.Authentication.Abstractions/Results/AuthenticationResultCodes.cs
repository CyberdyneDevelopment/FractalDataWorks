using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Authentication.Abstractions.Results;

/// <summary>
/// TypeCollection for authentication result codes.
/// </summary>
[TypeCollection(typeof(AuthenticationResultCodeBase), typeof(IResultCode), typeof(AuthenticationResultCodes))]
public abstract partial class AuthenticationResultCodes : TypeCollectionBase<AuthenticationResultCodeBase, IResultCode>
{
}
