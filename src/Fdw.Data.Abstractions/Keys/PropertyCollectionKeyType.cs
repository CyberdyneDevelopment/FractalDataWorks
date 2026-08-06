using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions;

/// <summary>
/// PropertyCollection key type — declares that a KVP child container's rows should be loaded
/// into a named dictionary property on the parent POCO.
/// </summary>
/// <remarks>
/// <para>
/// This key type is a pure metadata / gateway contract. It is never a physical database
/// constraint. Its two jobs are:
/// <list type="bullet">
/// <item>
/// <description>
/// <b>Binding declaration</b> — a <c>data.DataContainerKey</c> row with
/// <c>TypeId = 'PropertyCollection'</c> and <c>Name = '&lt;DictName&gt;'</c> tells the gateway
/// cascade: "when you encounter this child container, load its KVP rows into the property
/// named <c>&lt;DictName&gt;</c> on the parent POCO."
/// </description>
/// </item>
/// <item>
/// <description>
/// <b>Gateway dispatch</b> — <c>ConfigurationGateway</c> reads <c>key.KeyType</c>
/// and, when it equals <c>PropertyCollection</c>, uses <c>key.KeyName</c> as the destination
/// property name on the parent POCO, then writes the child's Name/Value rows into that
/// <c>IDictionary&lt;string, string?&gt;</c> property.
/// </description>
/// </item>
/// </list>
/// </para>
/// <para>
/// Why: binding lives in the <c>data.DataContainerKey</c> seed row, NOT in code or table-name
/// conventions. A parent can have multiple PropertyCollection keys, each binding to a different
/// dict property (e.g., <c>Authentication</c>, <c>Headers</c>, <c>Security</c>). The seed
/// decides the name; the POCO mirrors it exactly. No naming convention, no heuristic,
/// no reflection on table names.
/// </para>
/// <para>
/// Why no <c>data.MsSqlDataContainerKey</c> typed body: PropertyCollection keys have no
/// physical database representation (no index, no constraint). The detail loader handles
/// this via a LEFT JOIN so that PropertyCollection key rows flow through with no typed body
/// attached — which is correct semantics for a binding-declaration key.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(KeyTypes), "PropertyCollection")]
public sealed class PropertyCollectionKeyType : KeyTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="PropertyCollectionKeyType"/> class.</summary>
    public PropertyCollectionKeyType()
        : base(
            id: 9,
            name: "PropertyCollection",
            isPrimaryKey: false,
            hasConstraint: false,
            isReference: true,
            isSystemGenerated: false)
    {
    }

    /// <inheritdoc />
    /// <remarks>
    /// Why: PropertyCollection keys carry no uniqueness semantics — multiple KVP rows can share
    /// the same parent FK. Uniqueness is enforced at the application level per dict key.
    /// </remarks>
    public override bool SupportsUniqueness => false;
}
