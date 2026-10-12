using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Data.RowSources.Http.Abstractions;
using Fdw.Results;

namespace Fdw.Data.RowSources.Http.Abstractions.Tests;

public class HttpRowEnumeratorBaseTests
{
    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void RowsReadStartsAtZero()
    {
        var sut = new TestHttpRowEnumerator();

        sut.RowsRead.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void RowErrorsStartsAtZero()
    {
        var sut = new TestHttpRowEnumerator();

        sut.RowErrors.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void IncrementRowsReadIncreasesCounter()
    {
        var sut = new TestHttpRowEnumerator();

        sut.TestIncrementRowsRead();
        sut.TestIncrementRowsRead();
        sut.TestIncrementRowsRead();

        sut.RowsRead.ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void IncrementRowErrorsIncreasesCounter()
    {
        var sut = new TestHttpRowEnumerator();

        sut.TestIncrementRowErrors();
        sut.TestIncrementRowErrors();

        sut.RowErrors.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task DisposeAsyncCallsDisposeAsyncCore()
    {
        var sut = new TestHttpRowEnumerator();

        await sut.DisposeAsync();

        sut.DisposeCoreCallCount.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task DisposeAsyncIsIdempotent()
    {
        var sut = new TestHttpRowEnumerator();

        await sut.DisposeAsync();
        await sut.DisposeAsync();
        await sut.DisposeAsync();

        sut.DisposeCoreCallCount.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void CountersAreIndependent()
    {
        var sut = new TestHttpRowEnumerator();

        sut.TestIncrementRowsRead();
        sut.TestIncrementRowsRead();
        sut.TestIncrementRowErrors();

        sut.RowsRead.ShouldBe(2);
        sut.RowErrors.ShouldBe(1);
    }

    private sealed class TestHttpRowEnumerator : HttpRowEnumeratorBase
    {
        public int DisposeCoreCallCount { get; private set; }

        public void TestIncrementRowsRead() => IncrementRowsRead();
        public void TestIncrementRowErrors() => IncrementRowErrors();

        public override async IAsyncEnumerable<IGenericResult<IDictionary<string, object?>>> EnumerateRows(
            IRowMapper mapper,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }

        protected override ValueTask DisposeAsyncCore()
        {
            DisposeCoreCallCount++;
            return default;
        }
    }
}
