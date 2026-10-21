using Fdw.Data.Abstractions;

namespace Fdw.Data.Abstractions.Tests.VectorType;

// FDW-1354: a vector's text is a JSON array of numbers, the form SQL Server converts to and from VECTOR.
public sealed class VectorDataTypeTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ReadsAJsonArrayOfNumbers()
        => DataTypes.Vector.Parse(" [0.6, -0.8, 0 ] ").ShouldBe(new[] { 0.6f, -0.8f, 0f });

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("[]")]
    [InlineData("0.6,0.8")]
    [InlineData("[0.6,,0.8]")]
    [InlineData("[0.6,x]")]
    [InlineData("[NaN]")]
    public void ReadsNothingFromTextThatIsNotAVector(string text)
        => DataTypes.Vector.Parse(text).ShouldBeNull();

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WritesTheNumbersRoundTrippable()
    {
        var text = DataTypes.Vector.Format(new[] { 0.1f, -1f, 1e-7f });

        text.ShouldBe("[0.1,-1,1E-07]");
        DataTypes.Vector.Parse(text).ShouldBe(new[] { 0.1f, -1f, 1e-7f });
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IsReadAsFloatsAndNeverWrittenIntoAnExpression()
    {
        DataTypes.Vector.ClrType.ShouldBe(typeof(float[]));
        DataTypes.Vector.RendersInExpressions.ShouldBeFalse();
    }
}
