using Fdw.Configuration;

namespace Fdw.Services.Messaging.Abstractions;

/// <summary>
/// One configured messaging service — where its data lives, and which implementation it is.
/// </summary>
/// <remarks>
/// The typed domain contract. A constructor asking for this states which domain it reads; one asking
/// for the closed generic states only a shape, and two domains that share a shape become
/// interchangeable at the call site.
/// <para>
/// It carries the store and the path because those vary by deployment — which database this
/// deployment keeps its messages in is a fact about the deployment, not about the code. The container
/// names do NOT live here: <c>Message</c>, <c>MessageRecipient</c> and <c>AccessRequest</c> are
/// structural, identical in every deployment, and already supplied by the command that owns each one.
/// Putting a fixed fact on configuration makes it look like a choice and invites someone to set it
/// wrongly.
/// </para>
/// </remarks>
public interface IMessagingConfiguration
    : IPlatformServiceConfiguration<IMessagingImplementationConfiguration>
{
    /// <summary>Gets or sets the store this deployment keeps its messaging data in.</summary>
    string? DataStoreName { get; set; }

    /// <summary>Gets or sets the path within that store holding the messaging containers.</summary>
    string? PathName { get; set; }
}
