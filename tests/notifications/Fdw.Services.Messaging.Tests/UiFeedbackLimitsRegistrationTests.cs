using Fdw.ServiceTypes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shouldly;
using Xunit;

namespace Fdw.Services.Messaging.Tests;

/// <summary>
/// FDW-7058, on the declared-defaults mechanism (FDW-7103): the UI comment limits are configuration. The messaging option declares the defaults, a host
/// changes one with <see cref="ServiceTypeDefaults.Override"/> before registering, and a value its configuration states under
/// <see cref="DefaultMessagingServiceType.UiFeedbackSection"/> wins; a value that is not a whole number of at least one refuses registration. An override is
/// process-wide, so these tests do not run beside one another and each puts the declared value back.
/// </summary>
[Collection("DefaultMessagingServiceType.UiFeedbackLimits")]
[Trait("Category", "Configuration")]
public sealed class UiFeedbackLimitsRegistrationTests
{
    private static (HostApplicationBuilder Builder, Fdw.Results.IGenericResult<IHostApplicationBuilder> Registered) Register(params (string Key, string Value)[] stated)
    {
        var builder = Host.CreateApplicationBuilder();
        foreach (var (key, value) in stated)
            builder.Configuration[$"{DefaultMessagingServiceType.UiFeedbackSection}:{key}"] = value;

        return (builder, new DefaultMessagingServiceType().Register(builder, null));
    }

    private static UiFeedbackLimits Registered(HostApplicationBuilder builder)
    {
        using var services = builder.Services.BuildServiceProvider();
        return services.GetRequiredService<UiFeedbackLimits>();
    }

    // Puts a declared default back after a test overrode it: the override store holds no removal, and the declared value is what a host that changes nothing gets.
    private static void Restore(string field) =>
        ServiceTypeDefaults.Override(new DefaultMessagingServiceType(), field, new DefaultMessagingServiceType().DeclaredDefaults[field]);

    [Fact]
    [Trait("Priority", "P0")]
    public void TheDeclaredDefaultsApplyWhenTheHostStatesNone()
    {
        var (builder, registered) = Register();

        registered.IsSuccess.ShouldBeTrue(registered.CurrentMessage);
        var limits = Registered(builder);
        limits.MaxBodyLength.ShouldBe(MessagingDefaults.Of("MaxBodyLength"));
        limits.MaxPictureBytes.ShouldBe(MessagingDefaults.Of("MaxPictureBytes"));
        limits.MaxAnchors.ShouldBe(MessagingDefaults.Of("MaxAnchors"));
        limits.MaxAnchorLength.ShouldBe(MessagingDefaults.Of("MaxAnchorLength"));
        limits.MaxPageSize.ShouldBe(MessagingDefaults.Of("MaxPageSize"));
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void TheShippedPictureDefaultIsSixteenMebibytes()
        => new DefaultMessagingServiceType().DeclaredDefaults["MaxPictureBytes"].ShouldBe(16 * 1024 * 1024);

    [Fact]
    [Trait("Priority", "P0")]
    public void AValueTheConfigurationStatesWinsOverTheDeclaredOne()
    {
        var (builder, registered) = Register(
            ("MaxBodyLength", "3000"), ("MaxPictureBytes", "33554432"), ("MaxAnchors", "40"), ("MaxAnchorLength", "8000"), ("MaxPageSize", "500"));

        registered.IsSuccess.ShouldBeTrue(registered.CurrentMessage);
        var limits = Registered(builder);
        limits.MaxBodyLength.ShouldBe(3000);
        limits.MaxPictureBytes.ShouldBe(33_554_432);
        limits.MaxAnchors.ShouldBe(40);
        limits.MaxAnchorLength.ShouldBe(8000);
        limits.MaxPageSize.ShouldBe(500);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AValueTheHostOverridesBeforeRegisteringApplies()
    {
        try
        {
            ServiceTypeDefaults.Override(new DefaultMessagingServiceType(), "MaxPictureBytes", 1_000_000);
            var (builder, registered) = Register();

            registered.IsSuccess.ShouldBeTrue(registered.CurrentMessage);
            Registered(builder).MaxPictureBytes.ShouldBe(1_000_000);
        }
        finally
        {
            Restore("MaxPictureBytes");
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void TheConfigurationWinsOverAValueTheHostOverrode()
    {
        try
        {
            ServiceTypeDefaults.Override(new DefaultMessagingServiceType(), "MaxPageSize", 100);
            var (builder, registered) = Register(("MaxPageSize", "300"));

            registered.IsSuccess.ShouldBeTrue(registered.CurrentMessage);
            Registered(builder).MaxPageSize.ShouldBe(300);
        }
        finally
        {
            Restore("MaxPageSize");
        }
    }

    [Theory]
    [Trait("Priority", "P0")]
    [InlineData("MaxPictureBytes", "lots", "ABSTRACTIONS3-61037")]
    [InlineData("MaxAnchors", "2.5", "ABSTRACTIONS3-61037")]
    [InlineData("MaxPageSize", "99999999999", "ABSTRACTIONS3-61037")]
    [InlineData("MaxPictureBytes", "0", "ABSTRACTIONS3-61036")]
    [InlineData("MaxBodyLength", "-5", "ABSTRACTIONS3-61036")]
    public void AStatedValueThatIsNotAWholeNumberOfAtLeastOneRefusesRegistrationAndNamesTheKey(string key, string value, string code)
    {
        var (builder, registered) = Register((key, value));

        registered.IsFailure.ShouldBeTrue();
        registered.Messages.ShouldContain(m => m.Code == code);
        registered.CurrentMessage!.ShouldContain($"{DefaultMessagingServiceType.UiFeedbackSection}:{key}");
        registered.CurrentMessage!.ShouldContain(value);
        builder.Services.ShouldNotContain(descriptor => descriptor.ServiceType == typeof(UiFeedbackLimits));
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AValueTheHostOverrodeBelowOneRefusesRegistration()
    {
        try
        {
            ServiceTypeDefaults.Override(new DefaultMessagingServiceType(), "MaxAnchors", 0);
            var (_, registered) = Register();

            registered.IsFailure.ShouldBeTrue();
            registered.Messages.ShouldContain(m => m.Code == "ABSTRACTIONS3-61034");
        }
        finally
        {
            Restore("MaxAnchors");
        }
    }
}
