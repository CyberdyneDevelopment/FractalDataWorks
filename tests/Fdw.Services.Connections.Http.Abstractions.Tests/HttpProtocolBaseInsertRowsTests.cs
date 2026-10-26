using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Fdw.Commands.Data.Abstractions;
using Fdw.Configuration;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Http.Abstractions.OptionTypes.HttpProtocolOptions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Connections.Http.Abstractions.Tests;

[ExcludeFromCodeCoverage]
internal sealed class ListSendingHttpProtocol : HttpProtocolBase
{
    public ListSendingHttpProtocol()
        : base("ListProtocol", "Sends up to three insert rows in one request", "application/json")
    {
    }

    public override int InsertRowsPerRequest => 3;

    public new Task<IGenericResult<HttpContent?>> BuildRequestBody(
        IDataCommand command,
        IStorageContainer container,
        HttpProtocolContext context,
        CancellationToken cancellationToken)
        => base.BuildRequestBody(command, container, context, cancellationToken);
}

/// <summary>
/// FDW-2570: a protocol that says it sends several insert rows in one request sends them as a list, always a list, and refuses a command
/// that carries more rows than that; the default protocol keeps sending one row as an object.
/// </summary>
public sealed class HttpProtocolBaseInsertRowsTests
{
    private readonly HttpProtocolContext _context = new(new Mock<IGenericConfiguration>().Object, NullLoggerFactory.Instance, null, null, null);
    private readonly Mock<IStorageContainer> _container = new();

    private static IDataCommandWithInput Insert(params string[] names)
    {
        var command = new Mock<IDataCommandWithInput>();
        command.Setup(c => c.CommandType).Returns("Insert");
        command.Setup(c => c.InputData).Returns(names.Select(name => new { Name = name }).ToArray());
        return command.Object;
    }

    private static async Task<string> BodyOf(IGenericResult<HttpContent?> result)
        => await result.Value!.ReadAsStringAsync(TestContext.Current.CancellationToken);

    [Fact]
    public async Task AProtocolThatSendsAListSendsTheRowsOfARequestAsAList()
    {
        var result = await new ListSendingHttpProtocol().BuildRequestBody(Insert("a", "b"), _container.Object, _context, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        using var body = JsonDocument.Parse(await BodyOf(result));
        body.RootElement.ValueKind.ShouldBe(JsonValueKind.Array);
        body.RootElement.GetArrayLength().ShouldBe(2);
    }

    [Fact]
    public async Task AProtocolThatSendsAListSendsALastBatchOfOneRowAsAListToo()
    {
        var result = await new ListSendingHttpProtocol().BuildRequestBody(Insert("a"), _container.Object, _context, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        using var body = JsonDocument.Parse(await BodyOf(result));
        body.RootElement.ValueKind.ShouldBe(JsonValueKind.Array);
        body.RootElement.GetArrayLength().ShouldBe(1);
    }

    [Fact]
    public async Task ACommandWithMoreRowsThanTheProtocolSendsInARequestIsRefused()
    {
        var protocol = new ListSendingHttpProtocol();

        await Should.ThrowAsync<InvalidOperationException>(
            async () => await protocol.BuildRequestBody(Insert("a", "b", "c", "d"), _container.Object, _context, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task TheDefaultProtocolStillSendsASingleRowAsAnObject()
    {
        var result = await new TestHttpProtocol().BuildRequestBody(Insert("a"), _container.Object, _context, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        using var body = JsonDocument.Parse(await BodyOf(result));
        body.RootElement.ValueKind.ShouldBe(JsonValueKind.Object);
    }
}
