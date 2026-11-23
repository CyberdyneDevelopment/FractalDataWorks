using System.Linq;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Graphs.Building;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Graphs.DeclaredSettings;

/// <summary>
/// FDW-6133: a component type states its own settings. A setting no column, child row or stage holds is stored as a declared setting row, judged against
/// the component type when the graph is saved (unknown, required, untyped, unreadable as its type, naming a variable it may not), and handed to the component type's builder by
/// name; text that names variables as {{Name}} is read when the step runs.
/// </summary>
public sealed class DeclaredSettingsTests
{
    private const string VariableMissing = "FLOW-30370";
    private const string NotStated = "FLOW-30371";
    private const string Undeclared = "FLOW-60370";
    private const string HeldOnComponent = "FLOW-60371";
    private const string DataTypeMissing = "FLOW-60372";
    private const string DataTypeNotDeclared = "FLOW-60358";

    private static readonly ModelCallComponentType Kind = new();

    private static PipelineComponentConfiguration Component(params PipelineComponentSettingConfiguration[] settings) =>
        new() { Name = "Ask", ComponentType = Kind.Name, DeclaredSettings = [.. settings] };

    private static PipelineComponentSettingConfiguration Value(string name, string text) => new() { SettingName = name, ValueText = text };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ADeclaredSettingStoredWithNoColumnCountsAsStatedAndIsSound()
    {
        var component = Component(Value("model", "large"), Value("temperature", "0.7"), Value("question", "{{Question}}"));

        ComponentSettings.Stated(component).ShouldContain("model");
        ComponentSettings.Problems(component, Kind).ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ARequiredDeclaredSettingThatIsNotStoredIsRefused()
    {
        var problem = ComponentSettings.Problems(Component(Value("temperature", "0.7")), Kind).ShouldHaveSingleItem();

        problem.Path.ShouldBe(new[] { "components", "Ask", "settings", "model" });
        problem.Reason.ShouldContain("requires");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AStoredSettingTheKindDoesNotDeclareIsRefused()
    {
        var problem = ComponentSettings.Problems(Component(Value("model", "large"), Value("mood", "cheerful")), Kind).ShouldHaveSingleItem();

        problem.Path.Last().ShouldBe("mood");
        problem.Reason.ShouldContain("does not take");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AValueThatDoesNotReadAsTheDeclaredTypeIsRefusedNamingTheType()
    {
        var problem = ComponentSettings.Problems(Component(Value("model", "large"), Value("temperature", "warm")), Kind).ShouldHaveSingleItem();

        problem.Path.Last().ShouldBe("temperature");
        problem.Reason.ShouldContain("Double");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TextThatNamesAVariableIsRefusedForASettingNotReadWhenTheStepRuns()
    {
        var problem = ComponentSettings.Problems(Component(Value("model", "{{Chosen}}")), Kind).ShouldHaveSingleItem();

        problem.Path.Last().ShouldBe("model");
        problem.Reason.ShouldContain("variable");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ADeclaredSettingWithNoDataTypeIsRefusedWhenTheGraphIsSaved()
    {
        var untyped = new UntypedSettingComponentType();
        var component = new PipelineComponentConfiguration { Name = "Feel", ComponentType = untyped.Name, DeclaredSettings = [Value("mood", "calm")] };

        var problem = ComponentSettings.Problems(component, untyped).ShouldHaveSingleItem();

        problem.Path.Last().ShouldBe("mood");
        problem.Reason.ShouldContain("no data type");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ADeclaredSettingWithNoDataTypeIsNamedWhenTheHostStarts()
    {
        var found = ComponentSettings.DataTypesNotDeclared([Kind, new UntypedSettingComponentType()], NullLogger.Instance).ShouldHaveSingleItem();

        found.Code.ShouldBe(DataTypeNotDeclared);
        found.Message.ShouldContain("Untyped");
        found.Message.ShouldContain("mood");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void TheBuilderGetsAStoredValueByNameAsItsDeclaredType()
    {
        var setting = ComponentSettings.Declared(Component(Value("model", "large"), Value("temperature", "0.7")), Kind, "temperature", NullLogger.Instance);

        setting.IsSuccess.ShouldBeTrue(setting.CurrentMessage);
        var read = setting.Value.ShouldNotBeNull().Read(new FlowVariables(), NullLogger.Instance);
        read.IsSuccess.ShouldBeTrue(read.CurrentMessage);
        read.Value.ShouldBe(0.7d);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void TextThatNamesAVariableIsReadFromTheVariablesWhenTheStepRuns()
    {
        var setting = ComponentSettings.Declared(Component(Value("model", "large"), Value("question", "{{Question}}")), Kind, "question", NullLogger.Instance);
        var variables = new FlowVariables();
        variables.Declare("Question", "What is the plan?");

        var value = setting.Value.ShouldNotBeNull();
        var read = value.Read(variables, NullLogger.Instance);

        value.ReadsWhenStepRuns.ShouldBeTrue();
        read.IsSuccess.ShouldBeTrue(read.CurrentMessage);
        read.Value.ShouldBe("What is the plan?");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AVariableThatIsNotThereWhenTheStepRunsIsRefusedAsComponentSettingVariableMissing()
    {
        var setting = ComponentSettings.Declared(Component(Value("model", "large"), Value("question", "{{Question}}")), Kind, "question", NullLogger.Instance);

        var read = setting.Value.ShouldNotBeNull().Read(new FlowVariables(), NullLogger.Instance);

        read.IsFailure.ShouldBeTrue();
        read.Messages.ShouldHaveSingleItem().Code.ShouldBe(VariableMissing);
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    [InlineData("temperature", NotStated)]
    [InlineData("mood", Undeclared)]
    [InlineData("stage", HeldOnComponent)]
    public void ABuilderThatAsksForASettingThatIsNotAStoredDeclaredSettingIsRefusedWithTheCodeOfWhy(string name, string code)
    {
        var setting = ComponentSettings.Declared(Component(Value("model", "large")), Kind, name, NullLogger.Instance);

        setting.IsFailure.ShouldBeTrue();
        setting.Messages.ShouldHaveSingleItem().Code.ShouldBe(code);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ABuilderThatAsksForASettingItsKindDeclaresWithNoDataTypeIsRefused()
    {
        var untyped = new UntypedSettingComponentType();
        var component = new PipelineComponentConfiguration { Name = "Feel", ComponentType = untyped.Name, DeclaredSettings = [Value("mood", "calm")] };

        ComponentSettings.Declared(component, untyped, "mood", NullLogger.Instance).Messages.ShouldHaveSingleItem().Code.ShouldBe(DataTypeMissing);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void NoneOfFdwsOwnComponentTypesDeclaresASettingWithoutADataType()
    {
        ComponentSettings.DataTypesNotDeclared(ComponentTypes.All(), NullLogger.Instance).ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void TheNotFoundComponentTypeStillConstructsAndDeclaresNoSettingOfItsOwn()
    {
        var notFound = ComponentTypes.NotFound;

        notFound.ShouldNotBeNull();
        notFound.Settings.ShouldNotBeNull();
        notFound.Settings.ShouldAllBe(setting => setting.DataType == null);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void FdwsOwnComponentTypesHoldEverySettingThemselvesAndStateNoDataType()
    {
        foreach (var kind in ComponentTypes.All())
        {
            kind.Settings.ShouldAllBe(setting => setting.DataType == null, kind.Name);
            kind.Settings.ShouldNotContain(setting => ComponentSettingBindings.IsDeclared(kind, setting), kind.Name);
        }
    }
}
