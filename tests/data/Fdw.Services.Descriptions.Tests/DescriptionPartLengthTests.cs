using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Descriptions.Abstractions;
using Fdw.Services.Descriptions.Configuration;
using Fdw.Services.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Descriptions.Tests;

/// <summary>
/// The part length is configuration (FDW-6123): declared on <see cref="DescriptionServiceTypes"/>, 4000 unless a host sets another before registering, refused below one, and
/// the service the registration builds splits at it. It is a static setting, so these tests do not run beside one another or beside any other that registers the service type.
/// </summary>
[Collection("DescriptionServiceTypes.PartLength")]
[Trait("Category", "Descriptions")]
public sealed class DescriptionPartLengthTests
{
    private static HostApplicationBuilder Builder(List<IDescriptionImplementationConfiguration> saved)
    {
        var builder = Host.CreateApplicationBuilder();
        var provider = new Mock<IDescriptionConfigurationProvider>(MockBehavior.Strict);
        provider
            .Setup(p => p.InUnit(It.IsAny<Func<ConfigurationWriteScope, Task<IGenericResult>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<ConfigurationWriteScope, Task<IGenericResult>>, CancellationToken>((work, _) => work(null!));
        provider
            .Setup(p => p.Save<IDescriptionImplementationConfiguration>(
                It.IsAny<IDescriptionImplementationConfiguration>(), "Description", "Description", It.IsAny<string>(), It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .Callback<IDescriptionImplementationConfiguration, string, string, string, ConfigurationWriteScope, CancellationToken>((part, _, _, _, _, _) => saved.Add(part))
            .ReturnsAsync(GenericResult.Success());
        provider
            .Setup(p => p.ReadCurrent(It.IsAny<FilterExpression>(), It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<DescriptionDomainConfiguration>>.Success([]));
        var gateways = new Mock<IConfigurationGatewayProvider>(MockBehavior.Strict);
        var accessor = new Mock<IAuthenticationContextAccessor>(MockBehavior.Strict);
        accessor.SetupGet(a => a.Current).Returns((IAuthenticationContext?)null);
        builder.Services.AddSingleton(provider.Object);
        builder.Services.AddSingleton<IEffectiveSettingsProvider>(new EnglishToday());
        builder.Services.AddSingleton(gateways.Object);
        builder.Services.AddSingleton(accessor.Object);
        return builder;
    }

    private sealed class EnglishToday : IEffectiveSettingsProvider
    {
        public T GetEffectiveValue<T>(string settingName, Guid? tenantId = null, string? roleName = null) => throw new NotSupportedException();

        public bool TryGetEffectiveValue<T>(string settingName, out T value, Guid? tenantId = null, string? roleName = null)
        {
            value = (T)(object)"en-US";
            return settingName == SettingDefinitions.DescriptionLanguage;
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void ThePartLengthIsFourThousandUntilAHostSetsAnother()
        => DescriptionServiceTypes.PartLength.ShouldBe(4000);

    [Theory]
    [Trait("Priority", "P0")]
    [InlineData(0)]
    [InlineData(-1)]
    public void RegistrationRefusesAPartLengthBelowOneAndTheHostDoesNotStart(int length)
    {
        var declared = DescriptionServiceTypes.PartLength;
        try
        {
            DescriptionServiceTypes.PartLength = length;

            var registered = new DefaultDescriptionServiceType().Register(Builder([]), null);

            registered.IsFailure.ShouldBeTrue();
        }
        finally
        {
            DescriptionServiceTypes.PartLength = declared;
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task TheServiceRegistrationBuildsSplitsAtThePartLengthTheHostSetBeforeRegistering()
    {
        var declared = DescriptionServiceTypes.PartLength;
        var saved = new List<IDescriptionImplementationConfiguration>();
        try
        {
            DescriptionServiceTypes.PartLength = 10;
            var builder = Builder(saved);
            new DefaultDescriptionServiceType().Register(builder, null).IsSuccess.ShouldBeTrue();
            using var services = builder.Services.BuildServiceProvider();

            var written = await services.GetRequiredService<IDescriptionService>().Save(
                new DescriptionKey("DataSet", "Text", Guid.Parse("00000000-0000-0000-0000-0000000000a1"), "en-US"), "Orders", null, "alpha beta gamma delta", TestContext.Current.CancellationToken);

            written.IsSuccess.ShouldBeTrue(written.CurrentMessage);
            saved.Select(part => part.Text).ShouldBe(["alpha beta", " gamma", " delta"]);
        }
        finally
        {
            DescriptionServiceTypes.PartLength = declared;
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ADescriptionSavedBeforeTheLengthChangedKeepsItsPartsAndAReadJoinsThemEitherWay()
    {
        // The re-split happens at the next save, not on a read: the same stored parts are read joined whatever the length is now.
        var asked = new List<DomainQuery>();
        var provider = new Mock<IDescriptionConfigurationProvider>(MockBehavior.Strict);
        provider
            .Setup(p => p.Get(It.IsAny<DomainQuery>(), It.IsAny<CancellationToken>()))
            .Callback<DomainQuery, CancellationToken>((query, _) => asked.Add(query))
            .ReturnsAsync(() => GenericResult<ConfigurationPage<DescriptionDomainConfiguration>>.Success(new ConfigurationPage<DescriptionDomainConfiguration>(
                0, int.MaxValue - 1, false, _ => Parts(("alpha beta", 1), (" gamma", 2)))));
        var gateways = new Mock<IConfigurationGatewayProvider>(MockBehavior.Strict);
        gateways.SetupGet(g => g.KeyChunkSize).Returns(10);
        var accessor = new Mock<IAuthenticationContextAccessor>(MockBehavior.Strict);
        accessor.SetupGet(a => a.Current).Returns((IAuthenticationContext?)null);
        var key = new DescriptionKey("DataSet", "Text", Guid.Parse("00000000-0000-0000-0000-0000000000a1"), "en-US");

        // Stored as parts of 10, read by a service whose part length is now 4000 and by one whose is 3: the same text.
        var big = new DescriptionService(provider.Object, new EnglishToday(), gateways.Object, accessor.Object, 4000);
        var small = new DescriptionService(provider.Object, new EnglishToday(), gateways.Object, accessor.Object, 3);

        (await big.ReadMany(key.Type, key.SubType, [key.ThingId], TestContext.Current.CancellationToken)).Value![key.ThingId].Text.ShouldBe("alpha beta gamma");
        (await small.ReadMany(key.Type, key.SubType, [key.ThingId], TestContext.Current.CancellationToken)).Value![key.ThingId].Text.ShouldBe("alpha beta gamma");
        asked.Count.ShouldBe(2);
    }

    private static async IAsyncEnumerable<DescriptionDomainConfiguration> Parts(params (string Text, int Ordinal)[] parts)
    {
        foreach (var (text, ordinal) in parts)
        {
            yield return new DescriptionDomainConfiguration
            {
                DescriptionType = "DataSet", DescriptionSubType = "Text", ThingId = Guid.Parse("00000000-0000-0000-0000-0000000000a1"), Language = "en-US", Ordinal = ordinal, ThingName = "Orders", Text = text,
            };
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }
}
