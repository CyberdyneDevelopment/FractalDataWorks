using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Services.SecretManagers.Abstractions.Results;

/// <summary>
/// TypeCollection for SecretManager result codes.
/// Codes use categorized numbers (prefix "SECRETMANAGER"); the number is the whole identity
/// (Id == EventId == number, Code == "SECRETMANAGER-{number}") and its category is number / 10000.
/// </summary>
[TypeCollection(typeof(SecretManagerResultCodeBase), typeof(IResultCode), typeof(SecretManagerResultCodes))]
public abstract partial class SecretManagerResultCodes : TypeCollectionBase<SecretManagerResultCodeBase, IResultCode>
{
}
