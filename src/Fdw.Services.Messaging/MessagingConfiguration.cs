using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Messaging.Abstractions;

namespace Fdw.Services.Messaging;

/// <summary>
/// A configured messaging service — a <c>msg.Messaging</c> row in ConfigurationDb.
/// </summary>
/// <remarks>
/// The domain row: it names the service, says which kind it is, and says where this deployment keeps
/// its messaging data. What that kind needs in order to deliver a message lives on the implementation
/// this holds.
/// <para>
/// No property carries a value default. The store and the path are exactly the values that used to be
/// <c>const string</c> on MessageService and AccessRequestService, and defaulting them here would put
/// the same invisible assumption back one layer down.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Messaging",
    ServiceType = "Messaging",
    DisplayName = "Messaging",
    Description = "Where this deployment keeps its messages, and which messaging implementation delivers them.")]
public sealed partial class MessagingConfiguration : IMessagingConfiguration
{
    /// <inheritdoc/>
    public Guid Id { get; set; }

    /// <inheritdoc/>
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc/>
    [ValuesFrom(typeof(MessagingServiceTypes))]
    /// <summary>Gets the domain this record belongs to.</summary>
    public string Domain => "Messaging";

    public string? Implementation { get; set; }

    /// <inheritdoc/>
    public string? Description { get; set; }

    /// <inheritdoc/>
    public string? DataStoreName { get; set; }

    /// <inheritdoc/>
    public string? PathName { get; set; }

    /// <inheritdoc/>
    public IMessagingImplementationConfiguration? Configuration { get; set; }

    /// <inheritdoc />
    /// <remarks>
    /// The non-generic view of <see cref="Configuration"/>. The platform service provider reads the
    /// implementation without naming this domain's implementation contract; netstandard2.0 rules out
    /// a default interface implementation, so each domain record states it.
    /// </remarks>
    IGenericConfiguration? IDomainConfiguration.ImplementationConfiguration => Configuration;

}
