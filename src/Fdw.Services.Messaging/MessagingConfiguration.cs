using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
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
    public string SectionName => "Messaging";

    /// <inheritdoc/>
    public string ServiceType => "Messaging";

    /// <inheritdoc/>
    [ValuesFrom(typeof(MessagingServiceTypes))]
    public string? ServiceOptionType { get; set; }

    /// <inheritdoc/>
    public string? Description { get; set; }

    /// <inheritdoc/>
    public string? DataStoreName { get; set; }

    /// <inheritdoc/>
    public string? PathName { get; set; }

    /// <inheritdoc/>
    public IMessagingImplementationConfiguration? Configuration { get; set; }
}
