using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Users.Results;

/// <summary>
/// TypeCollection for User result codes.
/// EventId range: 7850-7899
/// </summary>
[TypeCollection(typeof(UserResultCodeBase), typeof(IResultCode), typeof(UserResultCodes))]
public abstract partial class UserResultCodes : TypeCollectionBase<UserResultCodeBase, IResultCode>
{
}

// =============================================================================
// User Result Codes (7850-7899)
// =============================================================================