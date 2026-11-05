using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Results;
using static Fdw.Services.Tests.ServiceProviderLifetimeTests;

namespace Fdw.Services.Tests;

/// <summary>
/// FDW-3574: the base get-all reads every member through the domain configuration provider and
/// builds each one. It returned <c>Success([])</c> whatever was stored.
/// </summary>
[Collection(nameof(ServicesTestCollection))]
public sealed class DomainServiceProviderGetAllTests
{
    [Fact]
    public async Task GetAllBuildsEveryStoredMember()
    {
        var provider = Provider(new TestServiceConfigurationProvider(
        [
            new TestServiceConfiguration { Name = "first", Implementation = "TypeA", Value = "1" },
            new TestServiceConfiguration { Name = "second", Implementation = "TypeA", Value = "2" },
        ]));
        provider.Register("TypeA", () => new TestServiceImplementationProvider(new TestServiceFactory()));

        var result = await provider.Get(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Select(s => s.ConfigurationValue).ShouldBe(["1", "2"]);
    }

    [Fact]
    public async Task GetAllAnswersAnEmptyListOnlyWhenNothingIsStored()
    {
        var provider = Provider(new TestServiceConfigurationProvider([]));
        provider.Register("TypeA", () => new TestServiceImplementationProvider(new TestServiceFactory()));

        var result = await provider.Get(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetAllFailsWhenAMemberNamesAnUnregisteredImplementation()
    {
        var provider = Provider(new TestServiceConfigurationProvider(
        [
            new TestServiceConfiguration { Name = "first", Implementation = "TypeA" },
            new TestServiceConfiguration { Name = "orphan", Implementation = "NotRegistered" },
        ]));
        provider.Register("TypeA", () => new TestServiceImplementationProvider(new TestServiceFactory()));

        var result = await provider.Get(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldBe(ServicesResultCodes.ByName("NoFactoryRegistered"));
    }

    [Fact]
    public async Task GetAllPassesTheConfigurationReadFailureThrough()
    {
        var read = new GenericMessage("the configuration store is unreachable");
        var store = new Mock<IDomainConfigurationProvider<TestServiceConfiguration>>();
        store.Setup(s => s.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<TestServiceConfiguration>>.Failure(read));
        var provider = Provider(store.Object);

        var result = await provider.Get(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldContain(read);
    }

    [Fact]
    public async Task GetAllFailsWithNoDomainConfigurationProvider()
    {
        var provider = new TestServiceProvider(NullLogger<TestServiceProvider>.Instance);

        var result = await provider.Get(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldBe(ServicesResultCodes.ByName("NoDomainConfigurationProvider"));
    }

    private static TestServiceProvider Provider(IDomainConfigurationProvider<TestServiceConfiguration> store)
    {
        var provider = new TestServiceProvider(NullLogger<TestServiceProvider>.Instance);
        provider.Register(store).IsSuccess.ShouldBeTrue();
        return provider;
    }
}
