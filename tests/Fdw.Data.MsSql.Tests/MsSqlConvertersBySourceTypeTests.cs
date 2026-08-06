using Fdw.Data.MsSql;

namespace Fdw.Data.MsSql.Tests;

/// <summary>
/// Tests the BySourceType() lookup logic in MsSqlConverters.
/// This is custom logic (not source-generated) and needs test coverage.
/// </summary>
public class MsSqlConvertersBySourceTypeTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BySourceTypeShouldReturnNotFoundForNull()
    {
        // Act
        var result = MsSqlConverters.BySourceType(null!);

        // Assert
        result.ShouldBe(MsSqlConverters.NotFound);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BySourceTypeShouldReturnNotFoundForEmptyString()
    {
        // Act
        var result = MsSqlConverters.BySourceType(string.Empty);

        // Assert
        result.ShouldBe(MsSqlConverters.NotFound);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BySourceTypeShouldReturnNotFoundForWhitespace()
    {
        // Act
        var result = MsSqlConverters.BySourceType("   ");

        // Assert
        result.ShouldBe(MsSqlConverters.NotFound);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BySourceTypeShouldFindIntType()
    {
        // Act
        var result = MsSqlConverters.BySourceType("int");

        // Assert
        result.ShouldNotBe(MsSqlConverters.NotFound);
        result.SourceType.ShouldBe("int");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BySourceTypeShouldFindNvarcharType()
    {
        // Act
        var result = MsSqlConverters.BySourceType("nvarchar");

        // Assert
        result.ShouldNotBe(MsSqlConverters.NotFound);
        result.SourceType.ShouldBe("nvarchar");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BySourceTypeShouldReturnNotFoundForInvalidType()
    {
        // Act
        var result = MsSqlConverters.BySourceType("invalid-type");

        // Assert
        result.ShouldBe(MsSqlConverters.NotFound);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BySourceTypeShouldBeCaseSensitive()
    {
        // Act
        var result = MsSqlConverters.BySourceType("INT");

        // Assert
        result.ShouldBe(MsSqlConverters.NotFound);
    }
}
