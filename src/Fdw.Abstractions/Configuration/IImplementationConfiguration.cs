namespace Fdw.Configuration;

/// <summary>
/// The contract every implementation configuration satisfies.
/// </summary>
/// <remarks>
/// Identity, name, and its own payload. No discriminator: the value that selected this
/// implementation is the domain's to state, not the implementation's to restate.
/// <para>
/// <see cref="Name"/> is the implementation's own persisted name, so an implementation row can be
/// resolved by name without first resolving the domain row that points at it.
/// </para>
/// </remarks>
public interface IImplementationConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the name this implementation record is looked up by.</summary>
    string Name { get; set; }
}
