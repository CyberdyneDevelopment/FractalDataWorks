using System;
using System.Text;
using Fdw.Collections;

namespace Fdw.Services.Data.Abstractions;

/// <summary>
/// Base class for schema discovery type definitions. A discovery type names a store-specific
/// discoverer so a connection type can declare which ones it supports; it is looked up by name
/// (<c>SchemaDiscoveryTypes.ByName</c>) and carries no behaviour of its own.
/// </summary>
/// <remarks>
/// Why there is no Register here any more: this declared <c>abstract IServiceCollection Register(IServiceCollection)</c>
/// and each option registered its own discoverer into the container, driven by a loop in
/// <c>ConfigurationGatewayDataStoreProvider</c> that existed only to run it. But the discoverer is used by
/// the CONNECTION type — <c>MsSqlConnectionType.DiscoverSchema()</c> resolves it — so the registration now
/// lives there, in the service that requires it, using the phase shape that service already has. That
/// leaves this a plain type option, which is what it always was.
/// </remarks>
public abstract class SchemaDiscoveryTypeBase : TypeOptionBase<int, SchemaDiscoveryTypeBase>, ISchemaDiscoveryType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaDiscoveryTypeBase"/> class.
    /// </summary>
    /// <param name="name">The name of this schema discovery type (e.g., "MsSql").</param>
    /// <param name="displayName">The display name for this schema discovery type.</param>
    /// <param name="description">A description of this schema discovery type.</param>
    /// <param name="category">The category (defaults to "SchemaDiscovery").</param>
    /// <remarks>
    /// No id parameter: options were handing one in — MsSql 1, PostgreSql 2 — from separate packages with
    /// nothing coordinating the numbers, so two contributors picking the same integer collided silently.
    /// A name collision already fails loudly in <c>ByName</c>, which is how these are resolved, so the name
    /// is the right source — exactly as <c>ServiceTypeBase</c> derives from it.
    /// </remarks>
    protected SchemaDiscoveryTypeBase(
        string name,
        string displayName,
        string description,
        string? category = null)
        : base(DeriveId(name), name, name, displayName, description, category ?? "SchemaDiscovery")
    {
    }

    // Why FNV-1a: it is the hash the generated collections already use to derive an id, so a name derived
    // here agrees with what the collection computes rather than introducing a second scheme.
    private static int DeriveId(string name)
    {
        if (name is null)
            throw new ArgumentNullException(nameof(name));

        const int FnvPrime = 0x01000193;
        var hash = unchecked((int)2166136261);

        foreach (var b in Encoding.UTF8.GetBytes(name))
        {
            hash ^= b;
            hash = unchecked(hash * FnvPrime);
        }

        return hash;
    }
}
