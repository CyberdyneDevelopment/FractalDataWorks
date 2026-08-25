using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Web.RestEndpoints.Base;
using Fdw.Web.RestEndpoints.Pagination;

namespace Fdw.Web.RestEndpoints.Tests.Base;

// Test implementation for QueryEndpointBase with custom request
public class TestQueryEndpoint : QueryEndpointBase<TestPagedRequest, TestQueryResult>
{
    private readonly System.Func<TestPagedRequest, CancellationToken, Task<IGenericResult<PagedResponse<TestQueryResult>>>> _executeFunc;
    private readonly int? _defaultPageSize;
    private readonly int? _maxPageSize;

    public TestQueryEndpoint(
        System.Func<TestPagedRequest, CancellationToken, Task<IGenericResult<PagedResponse<TestQueryResult>>>> executeFunc,
        int? defaultPageSize = null,
        int? maxPageSize = null)
    {
        _executeFunc = executeFunc;
        _defaultPageSize = defaultPageSize;
        _maxPageSize = maxPageSize;
    }

    protected override Task<IGenericResult<PagedResponse<TestQueryResult>>> ExecuteQuery(
        TestPagedRequest query,
        CancellationToken ct)
    {
        return _executeFunc(query, ct);
    }

    protected override int GetDefaultPageSize() => _defaultPageSize ?? base.GetDefaultPageSize();
    protected override int GetMaxPageSize() => _maxPageSize ?? base.GetMaxPageSize();

    // Public wrappers for testing protected methods
    public int PublicGetDefaultPageSize() => GetDefaultPageSize();
    public int PublicGetMaxPageSize() => GetMaxPageSize();
    public Task<object> PublicExecute(TestPagedRequest query, CancellationToken ct) => Execute(query, ct);
}

// Test implementation for QueryEndpointBase without custom request
public class TestSimpleQueryEndpoint : QueryEndpointBase<TestQueryResult>
{
    private readonly System.Func<PagedRequest, CancellationToken, Task<IGenericResult<PagedResponse<TestQueryResult>>>> _executeFunc;

    public TestSimpleQueryEndpoint(
        System.Func<PagedRequest, CancellationToken, Task<IGenericResult<PagedResponse<TestQueryResult>>>> executeFunc)
    {
        _executeFunc = executeFunc;
    }

    protected override Task<IGenericResult<PagedResponse<TestQueryResult>>> ExecuteQuery(
        PagedRequest query,
        CancellationToken ct)
    {
        return _executeFunc(query, ct);
    }

    // Public wrapper for testing protected method
    public Task<object> PublicExecute(PagedRequest query, CancellationToken ct) => Execute(query, ct);
}

public class TestPagedRequest : PagedRequest
{
    public string Filter { get; set; } = string.Empty;
}

public class TestQueryResult
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
}

public class QueryEndpointTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void QueryEndpoint_CanBeCreated()
    {
        // Arrange & Act
        var endpoint = new TestQueryEndpoint(
            (query, ct) => Task.FromResult<IGenericResult<PagedResponse<TestQueryResult>>>(
                GenericResult<PagedResponse<TestQueryResult>>.Success(new PagedResponse<TestQueryResult>())));

        // Assert
        endpoint.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void SimpleQueryEndpoint_CanBeCreated()
    {
        // Arrange & Act
        var endpoint = new TestSimpleQueryEndpoint(
            (query, ct) => Task.FromResult<IGenericResult<PagedResponse<TestQueryResult>>>(
                GenericResult<PagedResponse<TestQueryResult>>.Success(new PagedResponse<TestQueryResult>())));

        // Assert
        endpoint.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void GetDefaultPageSize_ReturnsDefault()
    {
        // Arrange
        var endpoint = new TestQueryEndpoint(
            (query, ct) => Task.FromResult<IGenericResult<PagedResponse<TestQueryResult>>>(
                GenericResult<PagedResponse<TestQueryResult>>.Success(new PagedResponse<TestQueryResult>())));

        // Act
        var pageSize = endpoint.PublicGetDefaultPageSize();

        // Assert
        pageSize.ShouldBe(50);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void GetDefaultPageSize_ReturnsCustomValue()
    {
        // Arrange
        var endpoint = new TestQueryEndpoint(
            (query, ct) => Task.FromResult<IGenericResult<PagedResponse<TestQueryResult>>>(
                GenericResult<PagedResponse<TestQueryResult>>.Success(new PagedResponse<TestQueryResult>())),
            defaultPageSize: 25);

        // Act
        var pageSize = endpoint.PublicGetDefaultPageSize();

        // Assert
        pageSize.ShouldBe(25);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void GetMaxPageSize_ReturnsDefault()
    {
        // Arrange
        var endpoint = new TestQueryEndpoint(
            (query, ct) => Task.FromResult<IGenericResult<PagedResponse<TestQueryResult>>>(
                GenericResult<PagedResponse<TestQueryResult>>.Success(new PagedResponse<TestQueryResult>())));

        // Act
        var maxPageSize = endpoint.PublicGetMaxPageSize();

        // Assert
        maxPageSize.ShouldBe(1000);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void GetMaxPageSize_ReturnsCustomValue()
    {
        // Arrange
        var endpoint = new TestQueryEndpoint(
            (query, ct) => Task.FromResult<IGenericResult<PagedResponse<TestQueryResult>>>(
                GenericResult<PagedResponse<TestQueryResult>>.Success(new PagedResponse<TestQueryResult>())),
            maxPageSize: 500);

        // Act
        var maxPageSize = endpoint.PublicGetMaxPageSize();

        // Assert
        maxPageSize.ShouldBe(500);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ExecuteAsync_NormalizesPageToOne_WhenLessThanOne()
    {
        // Arrange
        TestPagedRequest? capturedQuery = null;
        var endpoint = new TestQueryEndpoint(
            (query, ct) =>
            {
                capturedQuery = query;
                return Task.FromResult<IGenericResult<PagedResponse<TestQueryResult>>>(
                    GenericResult<PagedResponse<TestQueryResult>>.Success(new PagedResponse<TestQueryResult>()));
            });

        var request = new TestPagedRequest { Page = 0 };

        // Act
        await endpoint.PublicExecute(request, TestContext.Current.CancellationToken);

        // Assert
        capturedQuery.ShouldNotBeNull();
        capturedQuery.Page.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ExecuteAsync_NormalizesPageToOne_WhenNegative()
    {
        // Arrange
        TestPagedRequest? capturedQuery = null;
        var endpoint = new TestQueryEndpoint(
            (query, ct) =>
            {
                capturedQuery = query;
                return Task.FromResult<IGenericResult<PagedResponse<TestQueryResult>>>(
                    GenericResult<PagedResponse<TestQueryResult>>.Success(new PagedResponse<TestQueryResult>()));
            });

        var request = new TestPagedRequest { Page = -5 };

        // Act
        await endpoint.PublicExecute(request, TestContext.Current.CancellationToken);

        // Assert
        capturedQuery.ShouldNotBeNull();
        capturedQuery.Page.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ExecuteAsync_NormalizesPageSize_WhenLessThanOne()
    {
        // Arrange
        TestPagedRequest? capturedQuery = null;
        var endpoint = new TestQueryEndpoint(
            (query, ct) =>
            {
                capturedQuery = query;
                return Task.FromResult<IGenericResult<PagedResponse<TestQueryResult>>>(
                    GenericResult<PagedResponse<TestQueryResult>>.Success(new PagedResponse<TestQueryResult>()));
            });

        var request = new TestPagedRequest { PageSize = 0 };

        // Act
        await endpoint.PublicExecute(request, TestContext.Current.CancellationToken);

        // Assert
        capturedQuery.ShouldNotBeNull();
        capturedQuery.PageSize.ShouldBe(50);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ExecuteAsync_NormalizesPageSize_WhenGreaterThanMax()
    {
        // Arrange
        TestPagedRequest? capturedQuery = null;
        var endpoint = new TestQueryEndpoint(
            (query, ct) =>
            {
                capturedQuery = query;
                return Task.FromResult<IGenericResult<PagedResponse<TestQueryResult>>>(
                    GenericResult<PagedResponse<TestQueryResult>>.Success(new PagedResponse<TestQueryResult>()));
            },
            maxPageSize: 100);

        var request = new TestPagedRequest { PageSize = 200 };

        // Act
        await endpoint.PublicExecute(request, TestContext.Current.CancellationToken);

        // Assert
        capturedQuery.ShouldNotBeNull();
        capturedQuery.PageSize.ShouldBe(50);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ExecuteAsync_PreservesValidPageSize()
    {
        // Arrange
        TestPagedRequest? capturedQuery = null;
        var endpoint = new TestQueryEndpoint(
            (query, ct) =>
            {
                capturedQuery = query;
                return Task.FromResult<IGenericResult<PagedResponse<TestQueryResult>>>(
                    GenericResult<PagedResponse<TestQueryResult>>.Success(new PagedResponse<TestQueryResult>()));
            });

        var request = new TestPagedRequest { PageSize = 75 };

        // Act
        await endpoint.PublicExecute(request, TestContext.Current.CancellationToken);

        // Assert
        capturedQuery.ShouldNotBeNull();
        capturedQuery.PageSize.ShouldBe(75);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ExecuteAsync_CallsExecuteQueryAsync()
    {
        // Arrange
        var called = false;
        var endpoint = new TestQueryEndpoint(
            (query, ct) =>
            {
                called = true;
                return Task.FromResult<IGenericResult<PagedResponse<TestQueryResult>>>(
                    GenericResult<PagedResponse<TestQueryResult>>.Success(new PagedResponse<TestQueryResult>()));
            });

        var request = new TestPagedRequest();

        // Act
        await endpoint.PublicExecute(request, TestContext.Current.CancellationToken);

        // Assert
        called.ShouldBeTrue();
    }
}
