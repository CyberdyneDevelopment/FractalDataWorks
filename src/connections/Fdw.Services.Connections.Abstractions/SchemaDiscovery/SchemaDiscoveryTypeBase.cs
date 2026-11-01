using Fdw.Collections;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// Base class for schema discovery options. An option names a strategy so a connection type can
/// declare which ones it offers and a connection can name the one it uses; it is looked up by name
/// (<c>SchemaDiscoveryTypes.ByName</c>).
/// </summary>
/// <remarks>
/// Why no registration here: the services a strategy needs (the SQL Server discoverer, for example)
/// are registered and resolved by the connection type that performs the discovery, in the phases that
/// type already has. That leaves this a plain type option.
/// </remarks>
public abstract class SchemaDiscoveryTypeBase : TypeOptionBase<SchemaDiscoveryTypeBase>, ISchemaDiscoveryType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaDiscoveryTypeBase"/> class.
    /// </summary>
    /// <param name="name">The name of this schema discovery option (e.g., "MsSql").</param>
    /// <param name="displayName">The display name for this schema discovery option.</param>
    /// <param name="description">A description of this schema discovery option.</param>
    /// <param name="discoversSchema">Whether choosing this option discovers anything.</param>
    /// <remarks>
    /// <para>
    /// No id parameter: options were handing one in (MsSql 1, PostgreSql 2) from separate packages with
    /// nothing coordinating the numbers, so two contributors picking the same integer collided silently.
    /// The id is the option's own, derived from its fully qualified type name like every int-keyed type
    /// option's, so no two packages can hand in the same number.
    /// </para>
    /// <para>
    /// No category parameter: every option is in <see cref="SchemaDiscoveryTypes.ServiceCategory"/>. The
    /// parameter it replaces defaulted a missing category with <c>??</c>, and no option ever passed one.
    /// </para>
    /// <para>
    /// Why <paramref name="discoversSchema"/> has no default: whether an option discovers is the one fact
    /// every option must state, so it is stated at each option rather than assumed here.
    /// </para>
    /// </remarks>
    protected SchemaDiscoveryTypeBase(
        string name,
        string displayName,
        string description,
        bool discoversSchema)
        : base(name, name, displayName, description, SchemaDiscoveryTypes.ServiceCategory)
    {
        DiscoversSchema = discoversSchema;
    }

    /// <inheritdoc />
    public bool DiscoversSchema { get; }
}
