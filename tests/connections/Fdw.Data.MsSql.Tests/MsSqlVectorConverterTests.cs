using System;
using Fdw.Data.MsSql;
using Microsoft.Data.SqlTypes;

namespace Fdw.Data.MsSql.Tests;

// FDW-1354: a vector column reads as float[] whichever way SqlClient hands it back, and is written as its JSON text.
public sealed class MsSqlVectorConverterTests
{
    private readonly MsSqlVectorConverter _sut = new();

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IsTheConverterForTheVectorType()
    {
        var converter = MsSqlConverters.BySourceType("vector");

        converter.ShouldNotBe(MsSqlConverters.NotFound);
        converter.TargetClrType.ShouldBe(typeof(float[]));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ReadsSqlClientsVector()
        => _sut.ToClr(new SqlVector<float>(new[] { 0.6f, 0.8f, 0f })).ShouldBe(new[] { 0.6f, 0.8f, 0f });

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ReadsTheVectorsJsonText()
        => _sut.ToClr("[6.0000002e-001,8.0000001e-001,0.0]").ShouldBe(new[] { 0.6f, 0.8f, 0f });

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ReadsANullAsNull()
    {
        _sut.ToClr(DBNull.Value).ShouldBeNull();
        _sut.ToClr(null).ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void RefusesTextThatIsNotAVector()
        => Should.Throw<InvalidOperationException>(() => _sut.ToClr("not a vector"));

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WritesTheVectorAsItsJsonText()
        => _sut.ToDb(new[] { 0.5f, -1f, 0f }).ShouldBe("[0.5,-1,0]");
}
