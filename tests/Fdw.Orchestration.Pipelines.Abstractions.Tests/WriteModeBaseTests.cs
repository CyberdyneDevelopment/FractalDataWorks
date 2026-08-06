using Fdw.Configuration;
using Fdw.Orchestration.Pipelines.Abstractions.TypeCollections.WriteModeOptions;
using Fdw.Results;

namespace Fdw.Orchestration.Pipelines.Abstractions.Tests;

public class WriteModeBaseTests
{
    private sealed class TestWriteMode : WriteModeBase
    {
        public TestWriteMode(
            int id,
            string name,
            bool requiresExistenceCheck,
            bool preservesExistingData,
            bool canCreate = true,
            bool canUpdate = false,
            bool canDelete = false)
            : base(id, name, requiresExistenceCheck, preservesExistingData, canCreate, canUpdate, canDelete)
        {
        }

        public override Task<IGenericResult> Validate(
            IGenericConfiguration stageConfiguration,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IGenericResult>(GenericResult.Success());
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ConstructorSetsIdAndName()
    {
        var sut = new TestWriteMode(1, "Insert",
            requiresExistenceCheck: false,
            preservesExistingData: true);

        sut.Id.ShouldBe(1);
        sut.Name.ShouldBe("Insert");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ConstructorSetsRequiresExistenceCheck()
    {
        var sut = new TestWriteMode(2, "Upsert",
            requiresExistenceCheck: true,
            preservesExistingData: true);

        sut.RequiresExistenceCheck.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ConstructorSetsPreservesExistingData()
    {
        var sut = new TestWriteMode(3, "Truncate",
            requiresExistenceCheck: false,
            preservesExistingData: false);

        sut.PreservesExistingData.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void CanCreateDefaultsToTrue()
    {
        var sut = new TestWriteMode(1, "Insert",
            requiresExistenceCheck: false,
            preservesExistingData: true);

        sut.CanCreate.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void CanUpdateDefaultsToFalse()
    {
        var sut = new TestWriteMode(1, "Insert",
            requiresExistenceCheck: false,
            preservesExistingData: true);

        sut.CanUpdate.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void CanDeleteDefaultsToFalse()
    {
        var sut = new TestWriteMode(1, "Insert",
            requiresExistenceCheck: false,
            preservesExistingData: true);

        sut.CanDelete.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void UpsertModeAllowsCreateAndUpdate()
    {
        var sut = new TestWriteMode(2, "Upsert",
            requiresExistenceCheck: true,
            preservesExistingData: true,
            canCreate: true,
            canUpdate: true,
            canDelete: false);

        sut.CanCreate.ShouldBeTrue();
        sut.CanUpdate.ShouldBeTrue();
        sut.CanDelete.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void SyncModeAllowsAllOperations()
    {
        var sut = new TestWriteMode(4, "FullSync",
            requiresExistenceCheck: true,
            preservesExistingData: false,
            canCreate: true,
            canUpdate: true,
            canDelete: true);

        sut.CanCreate.ShouldBeTrue();
        sut.CanUpdate.ShouldBeTrue();
        sut.CanDelete.ShouldBeTrue();
        sut.RequiresExistenceCheck.ShouldBeTrue();
        sut.PreservesExistingData.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task ValidateCanBeInvoked()
    {
        var sut = new TestWriteMode(1, "Insert",
            requiresExistenceCheck: false,
            preservesExistingData: true);

        var config = new Mock<IGenericConfiguration>();
        var result = await sut.Validate(config.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void InsertModeTypicalConfiguration()
    {
        var sut = new TestWriteMode(1, "Insert",
            requiresExistenceCheck: false,
            preservesExistingData: true,
            canCreate: true,
            canUpdate: false,
            canDelete: false);

        sut.RequiresExistenceCheck.ShouldBeFalse();
        sut.PreservesExistingData.ShouldBeTrue();
        sut.CanCreate.ShouldBeTrue();
        sut.CanUpdate.ShouldBeFalse();
        sut.CanDelete.ShouldBeFalse();
    }
}
