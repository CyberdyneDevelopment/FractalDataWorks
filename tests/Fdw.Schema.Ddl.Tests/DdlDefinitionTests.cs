using Fdw.Schema.Ddl.Definitions;

namespace Fdw.Schema.Ddl.Tests;

public class DdlDefinitionTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DdlIndexDefinitionDefaultValues()
    {
        var index = new DdlIndexDefinition { Name = "IX_Test", Columns = ["Col1"] };

        index.IsUnique.ShouldBeFalse();
        index.IsClustered.ShouldBeFalse();
        index.IncludeColumns.ShouldBeNull();
        index.FilterPredicate.ShouldBeNull();
        index.FillFactor.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DdlIndexDefinitionWithAllOptions()
    {
        var index = new DdlIndexDefinition
        {
            Name = "IX_Filtered",
            Columns = ["Status", "CreatedDate"],
            IsUnique = true,
            IsClustered = false,
            IncludeColumns = ["Name", "Description"],
            FilterPredicate = "IsCurrent = 1",
            FillFactor = 90
        };

        index.Name.ShouldBe("IX_Filtered");
        index.Columns.Count.ShouldBe(2);
        index.IsUnique.ShouldBeTrue();
        index.IsClustered.ShouldBeFalse();
        index.IncludeColumns.ShouldNotBeNull();
        index.IncludeColumns!.Count.ShouldBe(2);
        index.FilterPredicate.ShouldBe("IsCurrent = 1");
        index.FillFactor.ShouldBe(90);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DdlForeignKeyDefinitionDefaultActions()
    {
        var fk = new DdlForeignKeyDefinition
        {
            Name = "FK_Child_Parent",
            ColumnName = "ParentId",
            ReferencedSchema = "cfg",
            ReferencedTable = "Parent",
            ReferencedColumn = "Id"
        };

        fk.OnDelete.ShouldBe(DdlForeignKeyActions.NoAction);
        fk.OnUpdate.ShouldBe(DdlForeignKeyActions.NoAction);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DdlForeignKeyDefinitionWithCascade()
    {
        var fk = new DdlForeignKeyDefinition
        {
            Name = "FK_OrderItem_Order",
            ColumnName = "OrderId",
            ReferencedSchema = "dbo",
            ReferencedTable = "Orders",
            ReferencedColumn = "Id",
            OnDelete = DdlForeignKeyActions.Cascade,
            OnUpdate = DdlForeignKeyActions.SetNull
        };

        fk.OnDelete.ShouldBe(DdlForeignKeyActions.Cascade);
        fk.OnUpdate.ShouldBe(DdlForeignKeyActions.SetNull);
    }

}
