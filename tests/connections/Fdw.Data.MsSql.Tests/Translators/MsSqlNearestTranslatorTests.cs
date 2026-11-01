using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Data.MsSql;

namespace Fdw.Data.MsSql.Tests.Translators;

// FDW-1354: the nearest search's T-SQL, and what it refuses before anything is sent.
[Collection(nameof(DataMsSqlTestCollection))]
public sealed class MsSqlNearestTranslatorTests
{
    private readonly MsSqlNearestTranslator _sut = new();

    private static IStorageContainer Container(params IField[] fields)
    {
        var schema = new Mock<IContainerSchema>();
        schema.Setup(s => s.Fields).Returns(fields);
        schema.Setup(s => s.GetReturnedFields(false, false, false)).Returns(fields);

        var container = new Mock<IStorageContainer>();
        container.Setup(c => c.Name).Returns("ConversationPassage");
        container.Setup(c => c.Path).Returns(new DatabasePath(null, "tc", "ConversationPassage"));
        container.Setup(c => c.Schema).Returns(schema.Object);
        return container.Object;
    }

    private static IField Field(string name, Type clrType, string typeName)
    {
        var fieldType = new Mock<IFieldType>();
        fieldType.Setup(t => t.ClrType).Returns(clrType);
        fieldType.Setup(t => t.TypeName).Returns(typeName);

        var field = new Mock<IField>();
        field.Setup(f => f.Name).Returns(name);
        field.Setup(f => f.FieldType).Returns(fieldType.Object);
        return field.Object;
    }

    private static IStorageContainer Passages() => Container(
        Field("Id", typeof(Guid), "uniqueidentifier"),
        Field("Text", typeof(string), "nvarchar"),
        Field("Vector", typeof(float[]), "vector"));

    private static NearestCommand<object> Nearest(
        string field = "Vector", IReadOnlyList<float>? vector = null, IVectorMetric? metric = null, int take = 5) => new()
        {
            VectorField = field,
            Vector = vector ?? [1f, 0f, 0f],
            Metric = metric ?? VectorMetrics.Cosine,
            Take = take,
        };

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task MeasuresTheVectorFieldByTheMetricNearestFirst()
    {
        var result = await _sut.Translate(Nearest(), Passages(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!.CommandText.ShouldBe(
            "SELECT TOP (@take) [Id], [Text], VECTOR_DISTANCE('Cosine', [Vector], CAST(@vector AS VECTOR(3))) AS [Distance] " +
            "FROM [tc].[ConversationPassage] WHERE [Vector] IS NOT NULL " +
            "ORDER BY VECTOR_DISTANCE('Cosine', [Vector], CAST(@vector AS VECTOR(3)))");
        result.Value.Parameters["@take"].Value.ShouldBe(5);
        result.Value.Parameters["@vector"].Value.ShouldBe("[1,0,0]");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task WritesTheMetricItIsGiven()
    {
        var result = await _sut.Translate(Nearest(metric: VectorMetrics.Euclidean), Passages(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!.CommandText.ShouldContain("VECTOR_DISTANCE('Euclidean', [Vector], CAST(@vector AS VECTOR(3)))");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task SendsTheVectorAsItsJsonText()
    {
        var result = await _sut.Translate(Nearest(vector: [0.6f, -0.8f]), Passages(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!.Parameters["@vector"].Value.ShouldBe("[0.6,-0.8]");
        result.Value.CommandText.ShouldContain("CAST(@vector AS VECTOR(2))");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task RefusesAFieldTheContainerDoesNotHave()
    {
        var result = await _sut.Translate(Nearest(field: "Embedding"), Passages(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Code!.Name.ShouldBe("InvalidNearestOptions");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task RefusesAFieldThatIsNotAVector()
    {
        var result = await _sut.Translate(Nearest(field: "Text"), Passages(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Code!.Name.ShouldBe("InvalidNearestOptions");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task RefusesAVectorWithNoNumbers()
    {
        var result = await _sut.Translate(Nearest(vector: []), Passages(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Code!.Name.ShouldBe("InvalidNearestOptions");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task RefusesASearchForNoRows()
    {
        var result = await _sut.Translate(Nearest(take: 0), Passages(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Code!.Name.ShouldBe("InvalidNearestOptions");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task RefusesAMetricThatNamesNoMetric()
    {
        var result = await _sut.Translate(Nearest(metric: VectorMetrics.NotFound), Passages(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Code!.Name.ShouldBe("InvalidNearestOptions");
    }

    // The metric's name is written into the statement, so a name that is not a plain word never reaches it.
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task RefusesAMetricWhoseNameIsNotAWord()
    {
        var result = await _sut.Translate(Nearest(metric: new InjectedMetric()), Passages(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Code!.Name.ShouldBe("InvalidNearestOptions");
    }

    private sealed class InjectedMetric() : VectorMetricBase("Cosine', [Vector], [Vector]); DROP TABLE x; --", "Injected", "A metric whose name would end the statement.");
}
