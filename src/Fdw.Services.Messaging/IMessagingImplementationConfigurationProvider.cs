using Fdw.Services.Abstractions;
using Fdw.Services.Messaging.Abstractions;

namespace Fdw.Services.Messaging;

/// <summary>Supplies the Messaging implementation's own configuration to the domain that registers it.</summary>
public interface IMessagingImplementationConfigurationProvider
    : IImplementationConfigurationProvider<IMessagingImplementationConfiguration>
{
}
