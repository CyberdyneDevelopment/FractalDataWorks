using Fdw.ServiceTypes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shouldly;
using Xunit;

namespace Fdw.Services.Messaging.Tests;

/// <summary>
/// FDW-7059, on the declared-defaults mechanism (FDW-7103): the most alerts one question may name is configuration. The messaging option declares the
/// default, a host changes it with <see cref="ServiceTypeDefaults.Override"/> before registering, and a value its configuration states under <see cref="DefaultMessagingServiceType.AlertsSection"/> wins; a value that is not a whole number of at least
/// one refuses registration. An override is process-wide, so these tests share the UI comment limits' collection and never run beside them.
/// </summary>
[Collection("DefaultMessagingServiceType.UiFeedbackLimits")]
[Trait("Category", "Configuration")]
public sealed class AlertLimitsRegistrationTests
{
    private static (HostApplicationBuilder Builder, Fdw.Results.IGenericResult<IHostApplicationBuilder> Registered) Register(string? maxAlerts)
    {
        var builder = Host.CreateApplicationBuilder();
        if (maxAlerts is not null)
            builder.Configuration[$"{DefaultMessagingServiceType.AlertsSection}:MaxAlerts"] = maxAlerts;

        return (builder, new DefaultMessagingServiceType().Register(builder, null));
    }

    private static AlertLimits Registered(HostApplicationBuilder builder)
    {
        using var services = builder.Services.BuildServiceProvider();
        return services.GetRequiredService<AlertLimits>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void TheDeclaredDefaultIsAHundredAndAppliesWhenTheHostStatesNone()
    {
        var (builder, registered) = Register(null);

        registered.IsSuccess.ShouldBeTrue(registered.CurrentMessage);
        new DefaultMessagingServiceType().DeclaredDefaults["MaxAlerts"].ShouldBe(100);
        Registered(builder).MaxAlerts.ShouldBe(MessagingDefaults.Of("MaxAlerts"));
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AValueTheConfigurationStatesWins()
    {
        var (builder, registered) = Register("250");

        registered.IsSuccess.ShouldBeTrue(registered.CurrentMessage);
        Registered(builder).MaxAlerts.ShouldBe(250);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AValueTheHostOverridesBeforeRegisteringApplies()
    {
        try
        {
            ServiceTypeDefaults.Override(new DefaultMessagingServiceType(), "MaxAlerts", 40);
            var (builder, registered) = Register(null);

            registered.IsSuccess.ShouldBeTrue(registered.CurrentMessage);
            Registered(builder).MaxAlerts.ShouldBe(40);
        }
        finally
        {
            ServiceTypeDefaults.Override(new DefaultMessagingServiceType(), "MaxAlerts", new DefaultMessagingServiceType().DeclaredDefaults["MaxAlerts"]);
        }
    }

    [Theory]
    [Trait("Priority", "P0")]
    [InlineData("0", "ABSTRACTIONS3-61036")]
    [InlineData("many", "ABSTRACTIONS3-61037")]
    public void AStatedValueThatIsNotAWholeNumberOfAtLeastOneRefusesRegistrationAndNamesTheKey(string value, string code)
    {
        var (builder, registered) = Register(value);

        registered.IsFailure.ShouldBeTrue();
        registered.Messages.ShouldContain(m => m.Code == code);
        registered.CurrentMessage!.ShouldContain($"{DefaultMessagingServiceType.AlertsSection}:MaxAlerts");
        builder.Services.ShouldNotContain(descriptor => descriptor.ServiceType == typeof(AlertLimits));
    }
}
