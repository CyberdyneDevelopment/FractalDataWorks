namespace Fdw.Configuration;

/// <summary>
/// The contract every implementation configuration satisfies.
/// </summary>
/// <remarks>
/// Identity and its own payload. No name and no discriminator: an implementation is identified by
/// its reference to the domain record, the name comes from that record, and the discriminator that
/// selected this implementation is the domain's to state, not the implementation's to restate.
/// </remarks>
public interface IImplementationConfiguration : IGenericConfiguration
{
}
