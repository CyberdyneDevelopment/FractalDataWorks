using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Data.RowSources.Http.Abstractions;
using Fdw.Results;

namespace Fdw.Data.RowSources.Tests;

public sealed class HttpRowEnumeratorBaseTests
{
    private sealed class TestEnumerator : HttpRowEnumeratorBase
    {
        private readonly int _rowCount;

        public TestEnumerator(int rowCount = 0)
        {
            _rowCount = rowCount;
        }

        public override async IAsyncEnumerable<IGenericResult<IDictionary<string, object?>>> EnumerateRows(
            IRowMapper mapper,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            for (int i = 0; i < _rowCount; i++)
            {
                IncrementRowsRead();
                yield return GenericResult<IDictionary<string, object?>>.Success(
                    new Dictionary<string, object?> { ["index"] = i });
            }

            await Task.CompletedTask;
        }

        public void SimulateRowError()
        {
            IncrementRowErrors();
        }
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void RowsReadStartsAtZero()
    {
        var enumerator = new TestEnumerator();

        enumerator.RowsRead.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void RowErrorsStartsAtZero()
    {
        var enumerator = new TestEnumerator();

        enumerator.RowErrors.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task EnumerateRowsIncrementsRowsRead()
    {
        var enumerator = new TestEnumerator(rowCount: 3);
        var mapper = new Mock<IRowMapper>();

        await foreach (var _ in enumerator.EnumerateRows(mapper.Object, TestContext.Current.CancellationToken))
        {
        }

        enumerator.RowsRead.ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void IncrementRowErrorsIncrementsCounter()
    {
        var enumerator = new TestEnumerator();

        enumerator.SimulateRowError();
        enumerator.SimulateRowError();

        enumerator.RowErrors.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task DisposeAsyncCanBeCalledMultipleTimes()
    {
        var enumerator = new TestEnumerator();

        await enumerator.DisposeAsync();
        await enumerator.DisposeAsync();
    }
}
