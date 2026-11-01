namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// A schema discovery option as the wire carries it: what a connection type offers, and what a
/// connection chose.
/// </summary>
/// <param name="Name">The option's name; what a connection stores in its <c>SchemaDiscovery</c>.</param>
/// <param name="DisplayName">The option's display name.</param>
/// <param name="DiscoversSchema">Whether choosing the option discovers anything; false for <c>None</c>.</param>
public sealed record SchemaDiscoveryTypeInfo(string Name, string DisplayName, bool DiscoversSchema)
{
    /// <summary>Describes a registered option.</summary>
    /// <param name="option">The option.</param>
    /// <returns>The option as the wire carries it.</returns>
    public static SchemaDiscoveryTypeInfo Of(ISchemaDiscoveryType option)
        => new(option.Name, option.DisplayName, option.DiscoversSchema);

    /// <summary>
    /// Describes the option a connection stores by name. The stored name is kept as it is, so a name
    /// that is not a registered option still shows what the connection holds, and reports that it does
    /// not discover; discovering through it is refused, with the reason.
    /// </summary>
    /// <param name="storedName">The name a connection stores in its <c>SchemaDiscovery</c>.</param>
    /// <returns>The option as the wire carries it.</returns>
    public static SchemaDiscoveryTypeInfo Of(string storedName)
    {
        var option = SchemaDiscoveryTypes.ByName(storedName);
        return new(storedName, option.DisplayName, option.DiscoversSchema);
    }
}
