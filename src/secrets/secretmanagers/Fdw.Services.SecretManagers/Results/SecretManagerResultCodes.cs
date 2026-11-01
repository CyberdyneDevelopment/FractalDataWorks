using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Services.SecretManagers.Results;

/// <summary>
/// TypeCollection for SecretManager result codes.
/// EventId range: 6000-6099 (within SecretManagers allocation)
/// </summary>
[TypeCollection(typeof(SecretManagerResultCodeBase), typeof(IResultCode), typeof(SecretManagerResultCodes))]
public abstract partial class SecretManagerResultCodes : TypeCollectionBase<SecretManagerResultCodeBase, IResultCode>
{
}
