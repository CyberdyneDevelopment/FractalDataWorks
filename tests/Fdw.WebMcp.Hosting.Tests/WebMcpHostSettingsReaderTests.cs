using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Hosts.Abstractions;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.WebMcp.Hosting.Tests;

/// <summary>
/// What a host's WebMcp row has to say before WebMCP is served, and that nothing it leaves out is
/// filled in.
/// </summary>
public class WebMcpHostSettingsReaderTests
{
    private readonly Mock<IWebMcpConfigurationProvider> _configurations = new();
    private readonly Mock<ISecretManagerProvider> _secretManagers = new();

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ARowNamingNoClientKeyAndNoSelfAddressServesNeither()
    {
        var result = await Reader(new WebMcpRow()).Read(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!.ApiKeyHeader.ShouldBe("X-Webmcp-Key");
        result.Value.ClientApiKey.ShouldBeNull();
        result.Value.SelfBaseAddress.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task TheClientKeyIsReadFromTheSecretManagerTheRowNames()
    {
        var manager = new Mock<ISecretManager>();
        manager.Setup(m => m.Execute(It.IsAny<ISecretManagerCommand<SecretValue>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<SecretValue>.Success(new SecretValue("webmcp-client-key", "the-key")));
        _secretManagers.Setup(p => p.Get("Main", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ISecretManager>.Success(manager.Object));

        var result = await Reader(new WebMcpRow { SecretManagerName = "Main", SecretKeyName = "webmcp-client-key" })
            .Read(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!.ClientApiKey.ShouldBe("the-key");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ARowNamingTheSecretManagerWithoutTheKeyIsRefused()
    {
        var result = await Reader(new WebMcpRow { SecretManagerName = "Main" }).Read(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("SecretKeyName");
        _secretManagers.Verify(p => p.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ARowWithoutAHeaderIsRefused()
    {
        var result = await Reader(new WebMcpRow { ApiKeyHeader = "" }).Read(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("ApiKeyHeader");
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    [InlineData("127.0.0.1:5510")]
    [InlineData("/relative")]
    [InlineData("ftp://127.0.0.1/")]
    public async Task ASelfAddressThatIsNotAnAbsoluteHttpAddressIsRefused(string address)
    {
        var result = await Reader(new WebMcpRow { SelfBaseAddress = address }).Read(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("SelfBaseAddress");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ARowThatCannotBeReadIsRefused()
    {
        _configurations.Setup(p => p.Get(WebMcpHosting.ConfigurationName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IWebMcpImplementationConfiguration>.Failure(new GenericMessage("no row")));
        var reader = new WebMcpHostSettingsReader(_configurations.Object, _secretManagers.Object, NullLogger<WebMcpHostSettingsReader>.Instance);

        var result = await reader.Read(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("not configured");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task TheRowIsReadOnce()
    {
        var reader = Reader(new WebMcpRow { SelfBaseAddress = "http://127.0.0.1:5510" });

        (await reader.Read(TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
        var second = await reader.Read(TestContext.Current.CancellationToken);

        second.Value!.SelfBaseAddress.ShouldBe(new Uri("http://127.0.0.1:5510"));
        _configurations.Verify(p => p.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private WebMcpHostSettingsReader Reader(WebMcpRow row)
    {
        _configurations.Setup(p => p.Get(WebMcpHosting.ConfigurationName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IWebMcpImplementationConfiguration>.Success(row));
        return new WebMcpHostSettingsReader(_configurations.Object, _secretManagers.Object, NullLogger<WebMcpHostSettingsReader>.Instance);
    }
}
