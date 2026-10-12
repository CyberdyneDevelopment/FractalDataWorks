using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Protocols.Tds;
using Fdw.Protocols.Tds.Types;
using Fdw.Tds.Endpoints.Hosting;
using Fdw.Tds.Endpoints.Sql;
using Fdw.Tds.Endpoints.Sql.Statements;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Tds.Endpoints.Tests;

/// <summary>
/// Runs batches the way a connection does — one session across them, a new request context for each — and
/// reads back what each wrote.
/// </summary>
internal sealed class SqlBatchHarness
{
    public SqlBatchHarness()
    {
        TdsServerVersion.TryParse("16.0.4135.4", out var version).ShouldBeTrue();
        TdsCollation.TryGet("SQL_Latin1_General_CP1_CI_AS", out var collation).ShouldBeTrue();
        Identity = new TdsServerIdentity("FDWSQL", version, "Standard Edition (64-bit)", 2, collation, "us_english", "Warehouse");
        Session = new TdsSession(51, "vendor", "tests", "test-host", "tests", "session-token", "Session", "Warehouse");
    }

    public FakeEndpoint Endpoint { get; } = new();

    public TdsServerIdentity Identity { get; }

    public TdsSession Session { get; }

    public async Task<BatchOutcome> Run(string text)
    {
        using var stream = new MemoryStream();
        var response = new TdsResponse(new TdsMessageWriter(stream, NullLogger.Instance), 64 * 1024, CancellationToken.None);
        var context = new SqlExecutionContext(Session, Identity, Endpoint, response, NullLogger.Instance);

        var result = await SqlBatchRunner.Run(text, context, TestContext.Current.CancellationToken);
        (await response.Complete()).IsSuccess.ShouldBeTrue();

        stream.Position = 0;
        var message = await new TdsMessageReader(stream, 10_000_000, NullLogger.Instance).Read(TestContext.Current.CancellationToken);
        return new BatchOutcome(result, ResponseTokens.Read(message.Value!.Payload));
    }
}
