using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Documents.Records.Abstractions;
using Fdw.Documents.Records.Abstractions.Capabilities;
using Fdw.Documents.Records.Abstractions.Commands;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;

namespace Fdw.Services.Connections.Tests;

/// <summary>
/// Tests for <see cref="DocumentRecordStores"/> (FDW-3673): an organization's document records live on the DocumentRecord connection its
/// DocumentRecords assignment names through its tenant's grant, and every missing or unfit link is a refusal naming the organization.
/// </summary>
public sealed class DocumentRecordStoresTests
{
    private static readonly Guid Finance = new("44444444-0000-0000-0000-000000000001");
    private static readonly Guid Legal = new("44444444-0000-0000-0000-000000000002");
    private static readonly Guid Rayburn = new("11111111-0000-0000-0000-000000000003");
    private static readonly Guid GrantId = new("55555555-0000-0000-0000-000000000009");

    private sealed class Rig
    {
        public Mock<IConnectionAssignmentConfigurationProvider> Assignments { get; } = new();
        public Mock<IConnectionGrantConfigurationProvider> Grants { get; } = new();
        public Mock<IDocumentRecordConnectionProvider> Connections { get; } = new();
        public Mock<IDocumentRecordConnection> Connection { get; } = new();
        public List<ConnectionAssignmentImplementationConfiguration> Assigned { get; } = [];
        public List<ConnectionGrantImplementationConfiguration> Granted { get; } = [];

        public Rig(bool atomic = true)
        {
            Assignments.Setup(a => a.Find(It.IsAny<Func<ConnectionAssignmentImplementationConfiguration, bool>>(), It.IsAny<CancellationToken>()))
                .Returns((Func<ConnectionAssignmentImplementationConfiguration, bool> predicate, CancellationToken _) =>
                    Task.FromResult(GenericResult<IReadOnlyList<ConnectionAssignmentImplementationConfiguration>>.Success([.. Assigned.Where(predicate)])));
            Grants.Setup(g => g.Get(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Returns((Guid id, CancellationToken _) =>
                    Task.FromResult(Granted.FirstOrDefault(grant => grant.Id == id) is { } found
                        ? GenericResult<IConnectionGrantImplementationConfiguration>.Success(found)
                        : GenericResult<IConnectionGrantImplementationConfiguration>.Failure(new GenericMessage("not found"))));
            Connection.Setup(c => c.RecordCapabilities).Returns(atomic ? [DocumentRecordCapabilities.ByName("AtomicRevisionWrites")] : []);
            Connections.Setup(c => c.Get("rayburn-records", It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult<IDocumentRecordConnection>.Success(Connection.Object));
        }

        public Rig Assign(Guid organization, string purpose = "DocumentRecords")
        {
            Assigned.Add(new ConnectionAssignmentImplementationConfiguration { OrganizationId = organization, Purpose = purpose, GrantId = GrantId });
            return this;
        }

        public Rig Grant()
        {
            Granted.Add(new ConnectionGrantImplementationConfiguration
            {
                Id = GrantId, GranteeTenantId = Rayburn, ConnectionName = "rayburn-records", Purpose = "DocumentRecords",
            });
            return this;
        }

        public DocumentRecordStores Stores() =>
            new(Assignments.Object, Grants.Object, Connections.Object, null, NullLogger<DocumentRecordStores>.Instance);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AnOrganizationsRecordsGoToTheConnectionItsAssignmentAndGrantName()
    {
        var rig = new Rig().Grant().Assign(Finance);
        GetOrganizationRevisionCommand? sent = null;
        rig.Connection.Setup(c => c.Execute(It.IsAny<IDocumentRecordCommand<IOrganizationRevision>>(), It.IsAny<CancellationToken>()))
            .Callback((IDocumentRecordCommand<IOrganizationRevision> command, CancellationToken _) => sent = command as GetOrganizationRevisionCommand)
            .ReturnsAsync(GenericResult<IOrganizationRevision>.Success(Mock.Of<IOrganizationRevision>()));

        var store = await rig.Stores().For(Finance, TestContext.Current.CancellationToken);
        var revision = await store.Value.ShouldNotBeNull().Revisions.Current(Finance, TestContext.Current.CancellationToken);

        store.IsSuccess.ShouldBeTrue(store.CurrentMessage);
        revision.IsSuccess.ShouldBeTrue(revision.CurrentMessage);
        sent.ShouldNotBeNull().OrganizationId.ShouldBe(Finance);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AnOrganizationWithNoDocumentRecordsAssignmentIsRefusedEvenWithAFilesAssignment()
    {
        var rig = new Rig().Grant().Assign(Finance, "Documents");

        var result = await rig.Stores().For(Finance, TestContext.Current.CancellationToken);
        var other = await rig.Stores().For(Legal, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        other.IsSuccess.ShouldBeFalse();
        rig.Connections.Verify(c => c.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AnAssignmentWhoseGrantIsGoneIsRefused()
    {
        var result = await new Rig().Assign(Finance).Stores().For(Finance, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AConnectionThatDoesNotWriteRevisionsAtomicallyIsRefusedAtUse()
    {
        var result = await new Rig(atomic: false).Grant().Assign(Finance).Stores().For(Finance, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("DocumentRecords");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AStoreFoundForOneOrganizationRefusesACallForAnotherAndSendsNothing()
    {
        var rig = new Rig().Grant().Assign(Finance);

        var store = await rig.Stores().For(Finance, TestContext.Current.CancellationToken);
        var otherOrganization = await store.Value.ShouldNotBeNull().Revisions.Current(Legal, TestContext.Current.CancellationToken);

        otherOrganization.IsSuccess.ShouldBeFalse();
        rig.Connection.Verify(c => c.Execute(It.IsAny<IDocumentRecordCommand<IOrganizationRevision>>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
