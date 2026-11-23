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
/// FDW-7104 on the declared-defaults mechanism (FDW-7103): the workspace endpoints' limits are configuration. With nothing stated the option's declared
/// defaults apply; a value the host's configuration states under <c>Workspaces:Default</c> wins; a host's <see cref="ServiceTypeDefaults.Override"/> applies
/// when its configuration states none, and the configuration wins over it; a value that is not a whole number of at least one refuses registration.
/// </summary>
/// <remarks>An override is process-wide, so this runs in the collection every reader of these defaults runs in, and each test puts the declared value back.</remarks>
[Collection(nameof(ServicesTestCollection))]
[Trait("Category", "Configuration")]
public sealed class WorkspaceLimitsRegistrationTests
{
    private const string Section = "Workspaces:Default";

    private static (HostApplicationBuilder Builder, IGenericResult<IHostApplicationBuilder> Registered) Register(params (string Key, string Value)[] stated)
    {
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
        foreach (var (key, value) in stated)
            builder.Configuration[$"{Section}:{key}"] = value;

        return (builder, new DefaultWorkspaceServiceType().Register(builder, NullLoggerFactory.Instance));
    }

    private static WorkspaceLimits Registered(HostApplicationBuilder builder)
    {
        using var services = builder.Services.BuildServiceProvider();
        return services.GetRequiredService<WorkspaceLimits>();
    }

    private static int Declared(string field) => (int)new DefaultWorkspaceServiceType().DeclaredDefaults[field];

    // Puts a declared default back after a test overrode it: the override store holds no removal.
    private static void Restore(string field) => ServiceTypeDefaults.Override(new DefaultWorkspaceServiceType(), field, Declared(field));

    [Fact]
    [Trait("Priority", "P0")]
    public void TheDeclaredDefaultsApplyWhenNothingIsStated()
    {
        var (builder, registered) = Register();

        registered.IsSuccess.ShouldBeTrue(registered.CurrentMessage);
        var limits = Registered(builder);
        limits.LineageMaxNodes.ShouldBe(Declared(nameof(WorkspaceLimits.LineageMaxNodes)));
        limits.LineageMaxDepth.ShouldBe(Declared(nameof(WorkspaceLimits.LineageMaxDepth)));
        limits.LineageMaxPathDepth.ShouldBe(Declared(nameof(WorkspaceLimits.LineageMaxPathDepth)));
        limits.LineageSearchDefaultTake.ShouldBe(Declared(nameof(WorkspaceLimits.LineageSearchDefaultTake)));
        limits.LineageSearchMaxTake.ShouldBe(Declared(nameof(WorkspaceLimits.LineageSearchMaxTake)));
        limits.NoteMaxBodyLength.ShouldBe(Declared(nameof(WorkspaceLimits.NoteMaxBodyLength)));
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AValueTheConfigurationStatesWinsOverTheDeclaredOne()
    {
        var (builder, registered) = Register(
            (nameof(WorkspaceLimits.LineageMaxNodes), "900"),
            (nameof(WorkspaceLimits.LineageMaxDepth), "7"),
            (nameof(WorkspaceLimits.LineageMaxPathDepth), "11"),
            (nameof(WorkspaceLimits.LineageSearchDefaultTake), "30"),
            (nameof(WorkspaceLimits.LineageSearchMaxTake), "250"),
            (nameof(WorkspaceLimits.NoteMaxBodyLength), "12000"));

        registered.IsSuccess.ShouldBeTrue(registered.CurrentMessage);
        var limits = Registered(builder);
        (limits.LineageMaxNodes, limits.LineageMaxDepth, limits.LineageMaxPathDepth).ShouldBe((900, 7, 11));
        (limits.LineageSearchDefaultTake, limits.LineageSearchMaxTake, limits.NoteMaxBodyLength).ShouldBe((30, 250, 12000));
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AValueTheHostOverridesBeforeRegisteringApplies()
    {
        try
        {
            ServiceTypeDefaults.Override(new DefaultWorkspaceServiceType(), nameof(WorkspaceLimits.LineageMaxNodes), 1234);
            var (builder, registered) = Register();

            registered.IsSuccess.ShouldBeTrue(registered.CurrentMessage);
            Registered(builder).LineageMaxNodes.ShouldBe(1234);
        }
        finally
        {
            Restore(nameof(WorkspaceLimits.LineageMaxNodes));
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void TheConfigurationWinsOverTheOverrideAndTheOverrideOverTheDeclaredDefault()
    {
        try
        {
            ServiceTypeDefaults.Override(new DefaultWorkspaceServiceType(), nameof(WorkspaceLimits.NoteMaxBodyLength), 6000);
            ServiceTypeDefaults.Override(new DefaultWorkspaceServiceType(), nameof(WorkspaceLimits.LineageMaxDepth), 3);
            var (builder, registered) = Register((nameof(WorkspaceLimits.NoteMaxBodyLength), "9000"));

            registered.IsSuccess.ShouldBeTrue(registered.CurrentMessage);
            var limits = Registered(builder);
            limits.NoteMaxBodyLength.ShouldBe(9000);
            limits.LineageMaxDepth.ShouldBe(3);
            limits.LineageMaxNodes.ShouldBe(Declared(nameof(WorkspaceLimits.LineageMaxNodes)));
        }
        finally
        {
            Restore(nameof(WorkspaceLimits.NoteMaxBodyLength));
            Restore(nameof(WorkspaceLimits.LineageMaxDepth));
        }
    }

    [Theory]
    [Trait("Priority", "P0")]
    [InlineData(nameof(WorkspaceLimits.LineageMaxNodes), "lots", "ABSTRACTIONS3-61037")]
    [InlineData(nameof(WorkspaceLimits.LineageSearchMaxTake), "2.5", "ABSTRACTIONS3-61037")]
    [InlineData(nameof(WorkspaceLimits.LineageMaxDepth), "0", "ABSTRACTIONS3-61036")]
    [InlineData(nameof(WorkspaceLimits.NoteMaxBodyLength), "-5", "ABSTRACTIONS3-61036")]
    public void AStatedValueThatIsNotAWholeNumberOfAtLeastOneRefusesRegistrationAndNamesTheKey(string key, string value, string code)
    {
        var (builder, registered) = Register((key, value));

        registered.IsSuccess.ShouldBeFalse();
        registered.Messages.ShouldContain(m => m.Code == code);
        registered.CurrentMessage!.ShouldContain($"{Section}:{key}");
        builder.Services.ShouldNotContain(descriptor => descriptor.ServiceType == typeof(WorkspaceLimits));
    }
}
