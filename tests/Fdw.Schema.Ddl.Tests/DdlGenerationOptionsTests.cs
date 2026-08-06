namespace Fdw.Schema.Ddl.Tests;

public class DdlGenerationOptionsTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DefaultValuesAreCorrect()
    {
        var options = new DdlGenerationOptions();

        options.SchemaName.ShouldBe("dbo");
        options.IfNotExists.ShouldBeTrue();
        options.IncludeIndexes.ShouldBeTrue();
        options.IncludeForeignKeys.ShouldBeTrue();
        options.IncludeDropStatements.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CustomValuesOverrideDefaults()
    {
        var options = new DdlGenerationOptions
        {
            SchemaName = "cfg",
            IfNotExists = false,
            IncludeIndexes = false,
            IncludeForeignKeys = false,
            IncludeDropStatements = true
        };

        options.SchemaName.ShouldBe("cfg");
        options.IfNotExists.ShouldBeFalse();
        options.IncludeIndexes.ShouldBeFalse();
        options.IncludeForeignKeys.ShouldBeFalse();
        options.IncludeDropStatements.ShouldBeTrue();
    }
}
