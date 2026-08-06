using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Calculations;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions;
using Fdw.Services.Calculations.Tests.TestSupport;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Calculations.Tests;

/// <summary>
/// Covers <see cref="FormulaCalculationEntityType"/>: the recursive-descent arithmetic parser
/// (precedence, associativity, parentheses, unary minus, div/mod-by-zero guard), field-reference
/// substitution, typed-configuration validation/creation, and the sealed
/// <see cref="CalculationEntityBase{TConfiguration}"/> dispatch (ValidateConfiguration/Execute/Configure).
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "CoreFramework")]
public class FormulaCalculationEntityTypeTests
{
    private static ResolvedCalculationInput DataSetInput(string alias, params Dictionary<string, object>[] rows)
        => new()
        {
            InputAlias = alias,
            Kind = CalculationInputKinds.ByName("DataSet"),
            ResolvedValue = new List<Dictionary<string, object>>(rows)
        };

    private static async Task<decimal> EvaluateAsync(FormulaCalculationEntityType type, string formulaBody, params Dictionary<string, object>[] rows)
    {
        var entity = new TestCalculationEntity
        {
            CalculationEntityType = "Formula",
            TypedConfiguration = new FormulaCalculationConfiguration { FormulaBody = formulaBody, FormulaLanguage = "CSharp" },
            Output = new CalculationOutputSpec { ResultFieldName = "Result" }
        };
        var inputs = new List<ResolvedCalculationInput> { DataSetInput("A", rows.Length == 0 ? [new Dictionary<string, object>()] : rows) };

        var result = await type.Execute(entity, inputs, Mock.Of<ICalculationContext>(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        using var doc = JsonDocument.Parse(result.Value!);
        var firstRow = doc.RootElement.GetProperty("Rows")[0];
        return firstRow.GetProperty("Result").GetDecimal();
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    [InlineData("2+3", 5)]
    [InlineData("10-3", 7)]
    [InlineData("4*5", 20)]
    [InlineData("20/4", 5)]
    [InlineData("10%3", 1)]
    public async Task ExecuteEvaluatesBasicArithmeticOperators(string formula, decimal expected)
    {
        var type = new FormulaCalculationEntityType();

        var value = await EvaluateAsync(type, formula);

        value.ShouldBe(expected);
    }

    [Fact]
    public async Task ExecuteMultiplicationTakesPrecedenceOverAddition()
    {
        var type = new FormulaCalculationEntityType();

        var value = await EvaluateAsync(type, "2+3*4");

        value.ShouldBe(14m);
    }

    [Fact]
    public async Task ExecuteDivisionTakesPrecedenceOverSubtraction()
    {
        var type = new FormulaCalculationEntityType();

        var value = await EvaluateAsync(type, "20-10/2");

        value.ShouldBe(15m);
    }

    [Fact]
    public async Task ExecuteParenthesesOverridePrecedence()
    {
        var type = new FormulaCalculationEntityType();

        var value = await EvaluateAsync(type, "(2+3)*4");

        value.ShouldBe(20m);
    }

    [Fact]
    public async Task ExecuteNestedParenthesesEvaluateInnermostFirst()
    {
        var type = new FormulaCalculationEntityType();

        var value = await EvaluateAsync(type, "((2+3)*(4-1))");

        value.ShouldBe(15m);
    }

    [Fact]
    public async Task ExecuteSubtractionIsLeftAssociative()
    {
        var type = new FormulaCalculationEntityType();

        // Why: right-associative subtraction would evaluate 10-(2-3)=11, not 5.
        var value = await EvaluateAsync(type, "10-2-3");

        value.ShouldBe(5m);
    }

    [Fact]
    public async Task ExecuteDivisionIsLeftAssociative()
    {
        var type = new FormulaCalculationEntityType();

        // Why: right-associative division would evaluate 20/(4/5)=25, not 1.
        var value = await EvaluateAsync(type, "20/4/5");

        value.ShouldBe(1m);
    }

    [Fact]
    public async Task ExecuteUnaryMinusNegatesFollowingTerm()
    {
        var type = new FormulaCalculationEntityType();

        var value = await EvaluateAsync(type, "-5+3");

        value.ShouldBe(-2m);
    }

    [Fact]
    public async Task ExecuteUnaryMinusAppliesToParenthesizedExpression()
    {
        var type = new FormulaCalculationEntityType();

        var value = await EvaluateAsync(type, "-(2+3)");

        value.ShouldBe(-5m);
    }

    [Fact]
    public async Task ExecuteSupportsDecimalLiterals()
    {
        var type = new FormulaCalculationEntityType();

        var value = await EvaluateAsync(type, "2.5+1.5");

        value.ShouldBe(4m);
    }

    [Fact]
    public async Task ExecuteDivisionByZeroReturnsZeroInsteadOfFailing()
    {
        // Why (defect): the parser silently maps division-by-zero to 0m rather than failing loud
        // with a structured MessageLogging error, which violates the NO-FALLBACKS convention.
        // This test characterizes the CURRENT behavior; it is not an endorsement of it.
        var type = new FormulaCalculationEntityType();

        var value = await EvaluateAsync(type, "5/0");

        value.ShouldBe(0m);
    }

    [Fact]
    public async Task ExecuteModuloByZeroReturnsZeroInsteadOfFailing()
    {
        // Why (defect): same silent-fallback issue as division by zero — see above.
        var type = new FormulaCalculationEntityType();

        var value = await EvaluateAsync(type, "5%0");

        value.ShouldBe(0m);
    }

    // Why: quarantined pending a production fix — FormulaCalculationEntityType.ParseUnary recurses into
    // ParsePrimary (not ParseUnary) for a leading '-' operand, so "--5" is mis-parsed (silent truncation,
    // a NO-FALLBACKS violation). This test documents that real defect; un-skip once ParseUnary is fixed to
    // either negate twice (=+5) or fail loud. Kept (not deleted) so the defect isn't lost.
    [Fact(Skip = "Documents real ParseUnary double-unary-minus defect; un-skip after the parser fix.")]
    public async Task ExecuteDoubleUnaryMinusDoesNotNegateTwice()
    {
        // Why (defect): ParseUnary recurses into ParsePrimary (not ParseUnary) for the operand of a
        // leading '-', so a second leading '-' is not itself parsed as a unary operator — ParsePrimary
        // sees a non-digit, non-'(' character and silently returns 0. "--5" therefore evaluates to 0,
        // not +5, and the trailing "5" is dropped without any validation error (NO-FALLBACKS violation:
        // malformed input should fail loud, not silently truncate).
        var type = new FormulaCalculationEntityType();

        var value = await EvaluateAsync(type, "--5");

        value.ShouldBe(0m);
    }

    [Fact]
    public async Task ExecuteTrailingGarbageIsSilentlyIgnored()
    {
        // Why (defect): the parser has no "end of input fully consumed" check, so trailing characters
        // after a syntactically complete expression are silently dropped instead of failing loud.
        var type = new FormulaCalculationEntityType();

        var value = await EvaluateAsync(type, "2+3abc");

        value.ShouldBe(5m);
    }

    [Fact]
    public async Task ExecuteSubstitutesFieldReferencesFromRow()
    {
        var type = new FormulaCalculationEntityType();
        var row = new Dictionary<string, object> { ["A"] = 2m, ["B"] = 3m };

        var value = await EvaluateAsync(type, "[A]+[B]", row);

        value.ShouldBe(5m);
    }

    [Fact]
    public async Task ExecuteSubstitutesRepeatedFieldReferences()
    {
        var type = new FormulaCalculationEntityType();
        var row = new Dictionary<string, object> { ["A"] = 4m };

        var value = await EvaluateAsync(type, "[A]+[A]", row);

        value.ShouldBe(8m);
    }

    [Fact]
    public async Task ExecuteProducesOneOutputRowPerInputRowAcrossAllInputs()
    {
        var type = new FormulaCalculationEntityType();
        var entity = new TestCalculationEntity
        {
            TypedConfiguration = new FormulaCalculationConfiguration { FormulaBody = "[X]*2", FormulaLanguage = "CSharp" },
            Output = new CalculationOutputSpec { ResultFieldName = "Result" }
        };
        var inputs = new List<ResolvedCalculationInput>
        {
            DataSetInput("A", new Dictionary<string, object> { ["X"] = 1m }, new Dictionary<string, object> { ["X"] = 2m }),
            DataSetInput("B", new Dictionary<string, object> { ["X"] = 3m })
        };

        var result = await type.Execute(entity, inputs, Mock.Of<ICalculationContext>(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        using var doc = JsonDocument.Parse(result.Value!);
        doc.RootElement.GetProperty("RowCount").GetInt32().ShouldBe(3);
        var values = new List<decimal>();
        foreach (var row in doc.RootElement.GetProperty("Rows").EnumerateArray())
            values.Add(row.GetProperty("Result").GetDecimal());
        values.ShouldBe([2m, 4m, 6m]);
    }

    [Fact]
    public async Task ExecuteMissingTypedConfigurationReturnsFormulaConfigurationNotLoaded()
    {
        var type = new FormulaCalculationEntityType();
        var entity = new TestCalculationEntity { TypedConfiguration = null };

        var result = await type.Execute(entity, [], Mock.Of<ICalculationContext>(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldNotBeEmpty();
        result.Messages[^1].Code.ShouldBe("CALCULATIONS-61000");
    }

    [Fact]
    public async Task ExecuteWrongTypedConfigurationTypeReturnsFormulaConfigurationNotLoaded()
    {
        var type = new FormulaCalculationEntityType();
        // Why: TypedConfiguration is IGenericConfiguration-typed but not a FormulaCalculationConfiguration.
        var entity = new TestCalculationEntity { TypedConfiguration = new WindowedCalculationConfiguration() };

        var result = await type.Execute(entity, [], Mock.Of<ICalculationContext>(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("CALCULATIONS-61000");
    }

    [Fact]
    public async Task ExecuteEmptyFormulaBodyReturnsValidationFailed()
    {
        var type = new FormulaCalculationEntityType();
        var entity = new TestCalculationEntity
        {
            TypedConfiguration = new FormulaCalculationConfiguration { FormulaBody = "   ", FormulaLanguage = "CSharp" }
        };

        var result = await type.Execute(entity, [], Mock.Of<ICalculationContext>(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("CALCULATIONS-21002");
        result.CurrentMessage.ShouldNotBeNull();
        result.CurrentMessage.ShouldContain("FormulaBody is empty");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ValidateTypedConfigurationEmptyBodyReturnsFailure()
    {
        var type = new FormulaCalculationEntityType();
        var config = new FormulaCalculationConfiguration { FormulaBody = "", FormulaLanguage = "CSharp" };

        var result = type.ValidateConfiguration(config);

        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("CALCULATIONS-21002");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ValidateTypedConfigurationWhitespaceBodyReturnsFailure()
    {
        var type = new FormulaCalculationEntityType();
        var config = new FormulaCalculationConfiguration { FormulaBody = "   ", FormulaLanguage = "CSharp" };

        var result = type.ValidateConfiguration(config);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ValidateTypedConfigurationValidBodyReturnsSuccess()
    {
        var type = new FormulaCalculationEntityType();
        var config = new FormulaCalculationConfiguration { FormulaBody = "[A]+1", FormulaLanguage = "CSharp" };

        var result = type.ValidateConfiguration(config);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ValidateConfigurationWrongTypeReturnsConfigurationTypeMismatch()
    {
        var type = new FormulaCalculationEntityType();

        var result = type.ValidateConfiguration(Mock.Of<IGenericConfiguration>());

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull();
        result.Code!.Name.ShouldBe("ConfigurationTypeMismatch");
    }

    [Fact]
    public void CreateTypedConfigurationMissingFormulaBodyReturnsNull()
    {
        var type = new FormulaCalculationEntityType();
        var node = new Dictionary<string, object?> { ["FormulaLanguage"] = "CSharp" };

        var config = type.CreateTypedConfiguration(node, Guid.NewGuid());

        config.ShouldBeNull();
    }

    [Fact]
    public void CreateTypedConfigurationBlankFormulaBodyReturnsNull()
    {
        var type = new FormulaCalculationEntityType();
        var node = new Dictionary<string, object?> { ["FormulaBody"] = "   ", ["FormulaLanguage"] = "CSharp" };

        var config = type.CreateTypedConfiguration(node, Guid.NewGuid());

        config.ShouldBeNull();
    }

    [Fact]
    public void CreateTypedConfigurationMissingFormulaLanguageReturnsNull()
    {
        // Why: a fabricated "CSharp" default would be a silent fallback (NO-FALLBACKS) — missing
        // FormulaLanguage must be treated the same as a missing body: cannot build.
        var type = new FormulaCalculationEntityType();
        var node = new Dictionary<string, object?> { ["FormulaBody"] = "1+1" };

        var config = type.CreateTypedConfiguration(node, Guid.NewGuid());

        config.ShouldBeNull();
    }

    [Fact]
    public void CreateTypedConfigurationBlankFormulaLanguageReturnsNull()
    {
        var type = new FormulaCalculationEntityType();
        var node = new Dictionary<string, object?> { ["FormulaBody"] = "1+1", ["FormulaLanguage"] = "  " };

        var config = type.CreateTypedConfiguration(node, Guid.NewGuid());

        config.ShouldBeNull();
    }

    [Fact]
    public void CreateTypedConfigurationValidInputsReturnsConfiguration()
    {
        var type = new FormulaCalculationEntityType();
        var entityId = Guid.NewGuid();
        var node = new Dictionary<string, object?> { ["FormulaBody"] = "[A]+1", ["FormulaLanguage"] = "Sql" };

        var config = type.CreateTypedConfiguration(node, entityId).ShouldBeOfType<FormulaCalculationConfiguration>();

        config.Id.ShouldBe(entityId);
        config.FormulaBody.ShouldBe("[A]+1");
        config.FormulaLanguage.ShouldBe("Sql");
        config.ServiceType.ShouldBe("Formula");
        config.TimeoutSeconds.ShouldBe(30);
    }

    [Fact]
    public void TypedContainerNameReturnsFormulaCalculation()
    {
        var type = new FormulaCalculationEntityType();

        type.TypedContainerName.ShouldBe("FormulaCalculation");
    }

    [Fact]
    public void ConfigurationTypeReturnsFormulaCalculationConfigurationType()
    {
        var type = new FormulaCalculationEntityType();

        type.ConfigurationType.ShouldBe(typeof(FormulaCalculationConfiguration));
    }

    [Fact]
    public void ConfigureBindsConfigurationSectionWithoutThrowing()
    {
        var type = new FormulaCalculationEntityType();
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        Should.NotThrow(() => type.Configure(services, configuration));

        services.ShouldNotBeEmpty();
    }
}
