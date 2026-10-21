using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Documents.Abstractions;
using Fdw.Documents.Abstractions.Commands;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;

namespace Fdw.Services.Connections.Tests;

/// <summary>
/// Tests for <see cref="DocumentStores"/> (FDW-1581): an organization's store is the granted connection narrowed to the grant's folder plus
/// the assignment's base folder, and every missing link in the chain is a clear refusal.
/// </summary>
public sealed class DocumentStoresTests
{
    private static readonly Guid Finance = new("44444444-0000-0000-0000-000000000001");
    private static readonly Guid Legal = new("44444444-0000-0000-0000-000000000002");
    private static readonly Guid Rayburn = new("11111111-0000-0000-0000-000000000003");
    private static readonly Guid GrantId = new("55555555-0000-0000-0000-000000000001");

    private sealed class Rig
    {
        public Mock<IConnectionAssignmentConfigurationProvider> Assignments { get; } = new();
        public Mock<IConnectionGrantConfigurationProvider> Grants { get; } = new();
        public Mock<IDocumentConnectionProvider> Connections { get; } = new();
        public Mock<IDocumentConnection> Connection { get; } = new();
        public List<ConnectionAssignmentImplementationConfiguration> Assigned { get; } = [];
        public List<ConnectionGrantImplementationConfiguration> Granted { get; } = [];

        public Rig()
        {
            Assignments.Setup(a => a.Find(It.IsAny<Func<ConnectionAssignmentImplementationConfiguration, bool>>(), It.IsAny<CancellationToken>()))
                .Returns((Func<ConnectionAssignmentImplementationConfiguration, bool> predicate, CancellationToken _) =>
                    Task.FromResult(GenericResult<IReadOnlyList<ConnectionAssignmentImplementationConfiguration>>.Success([.. Assigned.Where(predicate)])));
            Grants.Setup(g => g.Get(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Returns((Guid id, CancellationToken _) =>
                    Task.FromResult(Granted.FirstOrDefault(grant => grant.Id == id) is { } found
                        ? GenericResult<IConnectionGrantImplementationConfiguration>.Success(found)
                        : GenericResult<IConnectionGrantImplementationConfiguration>.Failure(new GenericMessage("not found"))));
            Connections.Setup(c => c.Get("rayburn-files", It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<IDocumentConnection>.Success(Connection.Object));
        }

        public Rig Assign(Guid organization, string purpose, string? baseFolder, Guid? grant = null)
        {
            Assigned.Add(new ConnectionAssignmentImplementationConfiguration { OrganizationId = organization, Purpose = purpose, GrantId = grant ?? GrantId, BaseFolder = baseFolder });
            return this;
        }

        public Rig Grant(string? folder, string connection = "rayburn-files")
        {
            Granted.Add(new ConnectionGrantImplementationConfiguration { Id = GrantId, GranteeTenantId = Rayburn, ConnectionName = connection, Purpose = "Documents", FolderPath = folder });
            return this;
        }

        public DocumentStores Stores() =>
            new(Assignments.Object, Grants.Object, Connections.Object, null, NullLogger<DocumentStores>.Instance);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task FinancesStoreCanOnlyReachTheGrantsFolderPlusItsBase()
    {
        var rig = new Rig().Grant("rayburn").Assign(Finance, "Documents", "finance");
        DeleteCommand? sent = null;
        rig.Connection.Setup(c => c.Execute(It.IsAny<IDocumentCommand<bool>>(), It.IsAny<CancellationToken>()))
            .Callback((IDocumentCommand<bool> command, CancellationToken _) => sent = command as DeleteCommand)
            .ReturnsAsync(GenericResult<bool>.Success(true));

        var store = await rig.Stores().For(Finance, "documents", TestContext.Current.CancellationToken);
        await store.Value!.Execute(new DeleteCommand(DocumentPath.Parse("contracts/a.pdf").Value), TestContext.Current.CancellationToken);

        store.IsSuccess.ShouldBeTrue();
        sent.ShouldNotBeNull().Path.Value.ShouldBe("rayburn/finance/contracts/a.pdf");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AGrantWithNoFolderAndAnAssignmentWithNoBaseGiveTheWholeConnection()
    {
        var rig = new Rig().Grant(null).Assign(Finance, "Documents", null);
        GetInfoCommand? sent = null;
        rig.Connection.Setup(c => c.Execute(It.IsAny<IDocumentCommand<Fdw.Documents.Abstractions.DocumentInfo>>(), It.IsAny<CancellationToken>()))
            .Callback((IDocumentCommand<Fdw.Documents.Abstractions.DocumentInfo> command, CancellationToken _) => sent = command as GetInfoCommand)
            .ReturnsAsync(GenericResult<Fdw.Documents.Abstractions.DocumentInfo>.Success(new Fdw.Documents.Abstractions.DocumentInfo(
                DocumentPath.Parse("a.pdf").Value, 1, null, DateTimeOffset.UnixEpoch, null, null, new Dictionary<string, string>())));

        var store = await rig.Stores().For(Finance, "Documents", TestContext.Current.CancellationToken);
        await store.Value!.Execute(new GetInfoCommand(DocumentPath.Parse("a.pdf").Value), TestContext.Current.CancellationToken);

        sent.ShouldNotBeNull().Path.Value.ShouldBe("a.pdf");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AnOrganizationWithNoAssignmentIsRefusedAndNeverGivenAnotherOnesStore()
    {
        var rig = new Rig().Grant("rayburn").Assign(Finance, "Documents", "finance");

        var legal = await rig.Stores().For(Legal, "Documents", TestContext.Current.CancellationToken);
        var otherPurpose = await rig.Stores().For(Finance, "Images", TestContext.Current.CancellationToken);

        legal.IsSuccess.ShouldBeFalse();
        otherPurpose.IsSuccess.ShouldBeFalse();
        rig.Connections.Verify(c => c.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AnAssignmentWhoseGrantIsGoneIsRefused()
    {
        var rig = new Rig().Assign(Finance, "Documents", "finance");

        var result = await rig.Stores().For(Finance, "Documents", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Theory]
    [InlineData("rayburn/../legal", null)]
    [InlineData("rayburn", "../legal")]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AFolderThatClimbsIsRefusedWhereverItIsStated(string? grantFolder, string? baseFolder)
    {
        var rig = new Rig().Grant(grantFolder).Assign(Finance, "Documents", baseFolder);

        var result = await rig.Stores().For(Finance, "Documents", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task AConnectionThatCannotBeOpenedAndAMissingPurposeAreRefused()
    {
        var rig = new Rig().Grant("rayburn", connection: "gone").Assign(Finance, "Documents", null);
        rig.Connections.Setup(c => c.Get("gone", It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<IDocumentConnection>.Failure(new GenericMessage("no such connection")));

        var closed = await rig.Stores().For(Finance, "Documents", TestContext.Current.CancellationToken);
        var blank = await rig.Stores().For(Finance, "  ", TestContext.Current.CancellationToken);

        closed.IsSuccess.ShouldBeFalse();
        blank.IsSuccess.ShouldBeFalse();
    }
}
