using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Settings;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>
/// What every host that goes through PlatformServices has, for a test host built by hand (FDW-6244): the authentication-context accessor, which
/// PlatformServiceEntry registers for every host, and the effective settings, of which the graph pipeline type's run services read the stop grace
/// (<see cref="SettingDefinitions.PipelineStopGraceSeconds"/>) when the host is built.
/// </summary>
internal static class PlatformHostServices
{
    /// <summary>The stop grace, in seconds, the test hosts' settings state.</summary>
    public const int StopGraceSeconds = 30;

    /// <summary>Adds the accessor and the settings to <paramref name="services"/>.</summary>
    /// <param name="services">The test host's services.</param>
    public static void Add(IServiceCollection services)
    {
        services.AddSingleton<IAuthenticationContextAccessor, AuthenticationContextAccessor>();
        var settings = new Mock<IEffectiveSettingsProvider>(MockBehavior.Strict);
        settings.Setup(s => s.GetEffectiveValue<int>(SettingDefinitions.PipelineStopGraceSeconds, null, null)).Returns(StopGraceSeconds);
        services.AddSingleton(settings.Object);
    }
}
