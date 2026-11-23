using Fdw.Results;
using Fdw.ServiceTypes;
using Fdw.Services.Workspaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Workspaces;

/// <summary>
/// FDW-7104 on the declared-defaults mechanism (FDW-7103): the most versions a place's history answers is configuration. With nothing stated the declared
/// default applies; a value stated under <c>Places:Default:MaxVersions</c> wins; a host's <see cref="ServiceTypeDefaults.Override"/> applies when the
/// configuration states none, and the configuration wins over it; a value that is not a whole number of at least one refuses registration.
/// </summary>
/// <remarks>An override is process-wide, so this runs in the collection every reader of this default runs in, and each test puts the declared value back.</remarks>
[Collection(nameof(ServicesTestCollection))]
[Trait("Category", "Configuration")]
public sealed class PlaceVersionLimitsRegistrationTests
{
    private const string Key = "Places:Default:" + nameof(PlaceVersionLimits.MaxVersions);

    private static (HostApplicationBuilder Builder, IGenericResult<IHostApplicationBuilder> Registered) Register(string? stated)
    {
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
        if (stated is not null)
            builder.Configuration[Key] = stated;

        return (builder, new DefaultPlaceServiceType().Register(builder, NullLoggerFactory.Instance));
    }

    private static int MaxVersionsOf(HostApplicationBuilder builder)
    {
        using var services = builder.Services.BuildServiceProvider();
        return services.GetRequiredService<PlaceVersionLimits>().MaxVersions;
    }

    private static int Declared => (int)new DefaultPlaceServiceType().DeclaredDefaults[nameof(PlaceVersionLimits.MaxVersions)];

    // Puts the declared default back after a test overrode it: the override store holds no removal.
    private static void Restore() => ServiceTypeDefaults.Override(new DefaultPlaceServiceType(), nameof(PlaceVersionLimits.MaxVersions), Declared);

    [Fact]
    [Trait("Priority", "P0")]
    public void TheDeclaredDefaultAppliesWhenNothingIsStated()
    {
        var (builder, registered) = Register(null);

        registered.IsSuccess.ShouldBeTrue(registered.CurrentMessage);
        MaxVersionsOf(builder).ShouldBe(Declared);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AValueTheConfigurationStatesWinsOverTheDeclaredOne()
    {
        var (builder, registered) = Register("2500");

        registered.IsSuccess.ShouldBeTrue(registered.CurrentMessage);
        MaxVersionsOf(builder).ShouldBe(2500);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AValueTheHostOverridesBeforeRegisteringApplies()
    {
        try
        {
            ServiceTypeDefaults.Override(new DefaultPlaceServiceType(), nameof(PlaceVersionLimits.MaxVersions), 42);
            var (builder, registered) = Register(null);

            registered.IsSuccess.ShouldBeTrue(registered.CurrentMessage);
            MaxVersionsOf(builder).ShouldBe(42);
        }
        finally
        {
            Restore();
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void TheConfigurationWinsOverAValueTheHostOverrode()
    {
        try
        {
            ServiceTypeDefaults.Override(new DefaultPlaceServiceType(), nameof(PlaceVersionLimits.MaxVersions), 42);
            var (builder, registered) = Register("77");

            registered.IsSuccess.ShouldBeTrue(registered.CurrentMessage);
            MaxVersionsOf(builder).ShouldBe(77);
        }
        finally
        {
            Restore();
        }
    }

    [Theory]
    [Trait("Priority", "P0")]
    [InlineData("many", "ABSTRACTIONS3-61037")]
    [InlineData("1.5", "ABSTRACTIONS3-61037")]
    [InlineData("0", "ABSTRACTIONS3-61036")]
    [InlineData("-1", "ABSTRACTIONS3-61036")]
    public void AStatedValueThatIsNotAWholeNumberOfAtLeastOneRefusesRegistrationAndNamesTheKey(string value, string code)
    {
        var (builder, registered) = Register(value);

        registered.IsSuccess.ShouldBeFalse();
        registered.Messages.ShouldContain(m => m.Code == code);
        registered.CurrentMessage!.ShouldContain(Key);
        builder.Services.ShouldNotContain(descriptor => descriptor.ServiceType == typeof(PlaceVersionLimits));
    }
}
