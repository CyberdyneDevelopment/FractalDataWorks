using Fdw.Services.Tags.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Catalog.Endpoints;
using Fdw.Services.Tags;
using Fdw.Services.Tags.Configuration;
using Fdw.Services.Tags.Results;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Catalog;

/// <summary>
/// FDW-3763: the tags an annotation shows and accepts are the tags of the data set it names, found by name; a text with no tag becomes a tenant
/// tag, a tag the request leaves out is taken off, and a missing data set or tenant is a refusal that changes nothing.
/// </summary>
[Collection(nameof(ServicesTestCollection))]
public sealed class DataSetAnnotationTagsTests
{
    private static readonly Guid Tenant = Guid.Parse("00000000-0000-0000-0000-0000000000a1");
    private static readonly Guid DataSet = Guid.Parse("00000000-0000-0000-0000-0000000000d1");

    private sealed class Accessor : IAuthenticationContextAccessor
    {
        public IAuthenticationContext? Current { get; set; }
    }

    private sealed class Rig
    {
        public Mock<ITagService> Tags { get; } = new();

        public Mock<IDataSetConfigurationProvider> DataSets { get; } = new();

        public Accessor Authentication { get; } = new();

        public Rig(IReadOnlyList<string> current, bool dataSetExists = true)
        {
            var caller = new Mock<IAuthenticationContext>();
            caller.SetupGet(c => c.ActiveTenantId).Returns(Tenant);
            Authentication.Current = caller.Object;

            var dataSet = new Mock<IDataSetImplementationConfiguration>();
            dataSet.SetupGet(d => d.Id).Returns(DataSet);
            DataSets.Setup(d => d.Get("NflTeams", It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult<IDataSetImplementationConfiguration>.Success(dataSetExists ? dataSet.Object : null!));

            Tags.Setup(t => t.TagsOf("DataSet", DataSet, It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult<IReadOnlyList<string>>.Success(current));
            Tags.Setup(t => t.Link("DataSet", DataSet, It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());
            Tags.Setup(t => t.Unlink("DataSet", DataSet, It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());
        }

        public Task<IGenericResult> Replace(params string[] wanted) =>
            DataSetAnnotationTags.Replace(Tags.Object, DataSets.Object, Authentication, "NflTeams", wanted, NullLogger.Instance, TestContext.Current.CancellationToken);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Catalog")]
    public async Task TheTagsOfTheDataSetTheAnnotationNamesAreRead()
    {
        var rig = new Rig(["finance", "q3"]);

        var read = await DataSetAnnotationTags.Read(rig.Tags.Object, rig.DataSets.Object, "NflTeams", NullLogger.Instance, TestContext.Current.CancellationToken);

        read.IsSuccess.ShouldBeTrue();
        read.Value.ShouldBe(["finance", "q3"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Catalog")]
    public async Task ADataSetThatIsNotThereIsNotAnEmptyList()
    {
        var rig = new Rig([], dataSetExists: false);

        var read = await DataSetAnnotationTags.Read(rig.Tags.Object, rig.DataSets.Object, "NflTeams", NullLogger.Instance, TestContext.Current.CancellationToken);

        read.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Catalog")]
    public async Task TagsTheRequestAddsArePutOnAndTagsItLeavesOutAreTakenOff()
    {
        var rig = new Rig(["finance", "q3"]);

        var replaced = await rig.Replace("finance", "q4");

        replaced.IsSuccess.ShouldBeTrue();
        rig.Tags.Verify(t => t.Unlink("DataSet", DataSet, "q3", It.IsAny<CancellationToken>()), Times.Once);
        rig.Tags.Verify(t => t.Link("DataSet", DataSet, "q4", It.IsAny<CancellationToken>()), Times.Once);
        rig.Tags.Verify(t => t.Link("DataSet", DataSet, "finance", It.IsAny<CancellationToken>()), Times.Never);
        rig.Tags.Verify(t => t.Unlink("DataSet", DataSet, "finance", It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Catalog")]
    public async Task ATagThatDiffersOnlyByCaseIsTheSameTag()
    {
        var rig = new Rig(["Finance"]);

        var replaced = await rig.Replace("finance", " FINANCE ");

        replaced.IsSuccess.ShouldBeTrue();
        rig.Tags.Verify(t => t.Link(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        rig.Tags.Verify(t => t.Unlink(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Catalog")]
    public async Task ATextWithNoTagBecomesATenantTagOfTheCallersTenantAndIsPutOn()
    {
        var rig = new Rig([]);
        rig.Tags.SetupSequence(t => t.Link("DataSet", DataSet, "brand-new", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(TagsResultCodes.ByName("TagNotFound"), NullLogger.Instance, ResultDetails.Create("name", "brand-new")))
            .ReturnsAsync(GenericResult.Success());
        rig.Tags.Setup(t => t.Create("brand-new", TagOwnerKinds.Tenant.Name, Tenant, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<TagDomainConfiguration>.Success(new TagDomainConfiguration { Name = "brand-new" }));

        var replaced = await rig.Replace("brand-new");

        replaced.IsSuccess.ShouldBeTrue();
        rig.Tags.Verify(t => t.Create("brand-new", TagOwnerKinds.Tenant.Name, Tenant, null, It.IsAny<CancellationToken>()), Times.Once);
        rig.Tags.Verify(t => t.Link("DataSet", DataSet, "brand-new", It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Catalog")]
    public async Task ACallerActingInNoTenantIsRefusedAndNothingIsChanged()
    {
        var rig = new Rig(["finance"]);
        rig.Authentication.Current = null;

        var replaced = await rig.Replace("q4");

        replaced.IsFailure.ShouldBeTrue();
        rig.Tags.Verify(t => t.Link(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        rig.Tags.Verify(t => t.Unlink(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Catalog")]
    public async Task ADataSetThatIsNotThereIsRefusedAndNothingIsChanged()
    {
        var rig = new Rig([], dataSetExists: false);

        var replaced = await rig.Replace("q4");

        replaced.IsFailure.ShouldBeTrue();
        rig.Tags.Verify(t => t.Link(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
