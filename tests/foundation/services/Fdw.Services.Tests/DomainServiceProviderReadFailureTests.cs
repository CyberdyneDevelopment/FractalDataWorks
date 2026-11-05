using System;
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
/// FDW-3586: a configuration read that fails reaches the caller with its own failure. Build answered
/// ConfigurationNotFound for every failure, so an ambiguous name or an unreachable store read as absent.
/// </summary>
[Collection(nameof(ServicesTestCollection))]
public sealed class DomainServiceProviderReadFailureTests
{
    [Fact]
    public async Task GetByNamePassesTheReadFailureThrough()
    {
        var read = new GenericMessage("two rows carry the name 'orders'");
        var store = new Mock<IDomainConfigurationProvider<TestServiceConfiguration>>();
        store.Setup(s => s.Get("orders", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<TestServiceConfiguration>.Failure(read));

        var result = await Provider(store.Object).Get("orders", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldContain(read);
        result.Code.ShouldNotBe(ServicesResultCodes.ByName("ConfigurationNotFound"));
    }

    [Fact]
    public async Task GetByIdPassesTheReadFailureThrough()
    {
        var id = Guid.NewGuid();
        var read = new GenericMessage("the configuration store is unreachable");
        var store = new Mock<IDomainConfigurationProvider<TestServiceConfiguration>>();
        store.Setup(s => s.Get(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<TestServiceConfiguration>.Failure(read));

        var result = await Provider(store.Object).Get(id, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldContain(read);
        result.Code.ShouldNotBe(ServicesResultCodes.ByName("ConfigurationNotFound"));
    }

    [Fact]
    public async Task GetByNameAnswersConfigurationNotFoundForASuccessWithNoValue()
    {
        var store = new Mock<IDomainConfigurationProvider<TestServiceConfiguration>>();
        store.Setup(s => s.Get("orders", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<TestServiceConfiguration>.Success(null!));

        var result = await Provider(store.Object).Get("orders", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldBe(ServicesResultCodes.ByName("ConfigurationNotFound"));
    }

    private static TestServiceProvider Provider(IDomainConfigurationProvider<TestServiceConfiguration> store)
    {
        var provider = new TestServiceProvider(NullLogger<TestServiceProvider>.Instance);
        provider.Register(store).IsSuccess.ShouldBeTrue();
        provider.Register("TypeA", () => new TestServiceImplementationProvider(new TestServiceFactory()));
        return provider;
    }
}
