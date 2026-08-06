using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.MsSql;
using Fdw.Results;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Connections.Sql.Tests;

/// <summary>
/// Proves the fail-loud contract of the shared base:
/// when a concrete per-command translator receives a container whose Path is NOT an IDatabasePath,
/// it must return a structured failure — never a default dialect, never an exception bubble.
/// Uses MsSqlQueryTranslator as the in-process concrete translator.
/// </summary>
[Collection(nameof(SqlTranslatorTestCollection))]
public sealed class SqlTranslatorBaseFailLoudTests
{
    private readonly MsSqlQueryTranslator _sut = new();

    private static Mock<IStorageContainer> CreateContainerWithNonDatabasePath()
    {
        // Why: supply a plain IPath (not IDatabasePath) so the translator's
        // `container.Path is not IDatabasePath` guard fires.
        var nonDbPath = new Mock<IPath>();
        var schema = new Mock<IContainerSchema>();
        schema.Setup(s => s.Fields).Returns([]);

        var container = new Mock<IStorageContainer>();
        container.Setup(c => c.Path).Returns(nonDbPath.Object);
        container.Setup(c => c.Schema).Returns(schema.Object);
        return container;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public async Task TranslateReturnsFailureWhenContainerPathIsNotIDatabasePath()
    {
        // Why: the dialect is ALWAYS derived from IDatabasePath — never defaulted.
        // This test is the direct proof of the "no fallback dialect" contract documented in
        // SqlDataCommandTranslatorBase's XML comment: "if the container's path is not an
        // IDatabasePath, the translator returns a fail-loud error result."
        var container = CreateContainerWithNonDatabasePath();
        var command = new Mock<IQueryCommand>();
        command.Setup(q => q.Filter).Returns((IFilterExpression?)null);
        command.Setup(q => q.Projection).Returns((IProjectionExpression?)null);
        command.Setup(q => q.Ordering).Returns((IOrderingExpression?)null);
        command.Setup(q => q.Paging).Returns((IPagingExpression?)null);

        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public async Task TranslateReturnsFailureWhenContainerIsNull()
    {
        var command = new Mock<IQueryCommand>();
        command.Setup(q => q.Filter).Returns((IFilterExpression?)null);
        command.Setup(q => q.Projection).Returns((IProjectionExpression?)null);
        command.Setup(q => q.Ordering).Returns((IOrderingExpression?)null);
        command.Setup(q => q.Paging).Returns((IPagingExpression?)null);

        var result = await _sut.Translate(command.Object, null!, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task TranslateSucceedsWhenContainerPathIsIDatabasePath()
    {
        // Why: contrast test — proves the guard only fires for non-IDatabasePath, not for
        // valid paths. Uses DatabasePath (MsSql concrete IDatabasePath impl).
        var dbPath = new DatabasePath(string.Empty, "dbo", "customers");
        var idField = new Mock<IField>();
        idField.Setup(f => f.Name).Returns("Id");
        idField.Setup(f => f.IsIdentity).Returns(false);
        idField.Setup(f => f.IsComputed).Returns(false);

        var schema = new Mock<IContainerSchema>();
        schema.Setup(s => s.Fields).Returns([idField.Object]);

        var container = new Mock<IDataContainer>();
        container.As<IStorageContainer>().Setup(c => c.Path).Returns(dbPath);
        container.Setup(c => c.Schema).Returns(schema.Object);
        container.Setup(c => c.ReferencingKeys)
            .Returns(GenericResult<IReadOnlyList<ReferencingKeyBinding>>.Success([]));
        container.Setup(c => c.Keys).Returns(new List<IContainerKey>());

        var command = new Mock<IQueryCommand>();
        command.Setup(q => q.Filter).Returns((IFilterExpression?)null);
        command.Setup(q => q.Projection).Returns((IProjectionExpression?)null);
        command.Setup(q => q.Ordering).Returns((IOrderingExpression?)null);
        command.Setup(q => q.Paging).Returns((IPagingExpression?)null);

        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }
}
