using System;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// Marks a connection configuration property whose value must never be shown: a response carries
/// <see cref="ConnectionSecretMask.Value"/> in its place, and an update that sends the mask keeps the
/// stored value.
/// </summary>
/// <remarks>
/// A configuration holds secret REFERENCES (a secret manager name, a key name), never the secret itself;
/// the reference is still masked, because naming where a credential lives is itself sensitive.
/// String dictionaries on a configuration are masked entry by entry without this marker; see
/// <see cref="IConnectionEndpointBehavior"/>.
/// </remarks>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class ConnectionSecretAttribute : Attribute
{
}
