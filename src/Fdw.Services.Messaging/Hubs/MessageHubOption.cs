using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Fdw.Collections.Attributes;
using Fdw.SignalR;

namespace Fdw.Services.Messaging.Hubs;

/// <summary>
/// Registers the message hub against the <see cref="RealTimeHubs"/> collection.
/// </summary>
/// <remarks>
/// The message hub has no dedicated broadcaster: <see cref="MessageService"/> publishes directly
/// through the typed <c>IHubContext&lt;MessageHub, IMessageHubClient&gt;</c> that <c>AddSignalR()</c>
/// provides, so <see cref="RegisterServices"/> registers nothing extra — the option exists to route
/// the hub through the same discovery/mapping path as every other FDW hub.
/// </remarks>
[TypeOption(typeof(RealTimeHubs), "Message")]
public sealed class MessageHubOption : RealTimeHubOptionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MessageHubOption"/> class.
    /// </summary>
    public MessageHubOption()
        : base(4, "Message", "/hubs/messages", typeof(MessageHub), authorizationPolicy: null)
    {
    }

    /// <inheritdoc/>
    public override void RegisterServices(IServiceCollection services, ILoggerFactory? loggerFactory)
    {
    }

    /// <inheritdoc/>
    public override void Map(IEndpointRouteBuilder endpoints) => MapHubAt<MessageHub>(endpoints);
}
