using System;
using System.Linq.Expressions;
using Shouldly;
using Xunit;

namespace Fdw.Data.DataSets.Abstractions.Tests;

public class SelectProjectionTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorInitializesWithDefaults()
    {
        // Arrange & Act
        var sut = new SelectProjection();

        // Assert
        sut.Fields.ShouldNotBeNull();
        sut.Fields.ShouldBeEmpty();
        sut.OriginalExpression.ShouldNotBeNull();
        sut.OriginalExpression.ShouldBeOfType<DefaultExpression>();
        sut.ResultType.ShouldBe(typeof(object));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CanInitializeFieldsWithValues()
    {
        // Arrange
        var fields = new[]
        {
            new ProjectedField { SourceField = "Id", Alias = "Id", FieldType = typeof(int) },
            new ProjectedField { SourceField = "Name", Alias = "Name", FieldType = typeof(string) }
        };

        // Act
        var sut = new SelectProjection { Fields = fields };

        // Assert
        sut.Fields.Count.ShouldBe(2);
        sut.Fields[0].SourceField.ShouldBe("Id");
        sut.Fields[1].SourceField.ShouldBe("Name");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CanSetOriginalExpression()
    {
        // Arrange
        Expression<Func<int, string>> expr = x => x.ToString();

        // Act
        var sut = new SelectProjection { OriginalExpression = expr };

        // Assert
        sut.OriginalExpression.ShouldBe(expr);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CanSetResultType()
    {
        // Arrange & Act
        var sut = new SelectProjection { ResultType = typeof(string) };

        // Assert
        sut.ResultType.ShouldBe(typeof(string));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CanInitializeAllPropertiesTogether()
    {
        // Arrange
        var fields = new[]
        {
            new ProjectedField { SourceField = "Id", Alias = "Id", FieldType = typeof(int) }
        };
        Expression<Func<int, string>> expr = x => x.ToString();

        // Act
        var sut = new SelectProjection
        {
            Fields = fields,
            OriginalExpression = expr,
            ResultType = typeof(string)
        };

        // Assert
        sut.Fields.Count.ShouldBe(1);
        sut.OriginalExpression.ShouldBe(expr);
        sut.ResultType.ShouldBe(typeof(string));
    }
}
