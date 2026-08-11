using System;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Settings.Components.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Fdw.Services.Settings.Components.SettingsComponentOptions;

/// <summary>The settings component.</summary>
/// <remarks>
/// Registers the two named HttpClients the provider resolves. This is the point of declaring a
/// component rather than scanning for one: SettingsProvider calls
/// <c>CreateClient("SettingsClient")</c> and <c>CreateClient("ThemeClient")</c>, and
/// IHttpClientFactory does not throw for a name it does not know — it returns a client with no
/// BaseAddress, so the first request fails on a relative URI somewhere unrelated to the cause.
/// Registering them beside the component that names them is what makes that impossible.
/// </remarks>
[TypeOption(typeof(SettingsComponents), "Settings")]
public class SettingsProviderOption : SettingsComponentBase<SettingsProvider>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsProviderOption"/> class.
    /// </summary>
    public SettingsProviderOption()
    {
        Registration((builder, loggerFactory) =>
        {
            // Why named rather than typed clients: the provider resolves them by name through
            // IHttpClientFactory, and the name is the contract. The addresses come from the host's
            // ServiceEndpoints configuration, so a deployment moves the API without touching this.
            builder.Services.AddHttpClient("SettingsClient");
            builder.Services.AddHttpClient("ThemeClient");

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
    }
}
