using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Graphs.Building;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Flow;

/// <summary>
/// Tests for parameters in the scope of a run (FDW-1549): a read-only one refuses a set, a mutable one takes it, and one the run left
/// blank is in scope with no value, so reading it is a different mistake from naming a variable nobody declared.
/// </summary>
public sealed class FlowParameterScopeTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AReadOnlyParameterRefusesASetAndKeepsItsValue()
    {
        var scope = new FlowVariables();
        scope.DeclareParameter("Region", "West", isReadOnly: true);

        scope.TrySet("Region", "East").ShouldBeFalse();

        scope.IsReadOnly("Region").ShouldBeTrue();
        scope.TryGet("Region", out var value).ShouldBeTrue();
        value.ShouldBe("West");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AMutableParameterTakesASetLikeAVariable()
    {
        var scope = new FlowVariables();
        scope.DeclareParameter("Region", "West", isReadOnly: false);

        scope.TrySet("Region", "East").ShouldBeTrue();

        scope.TryGet("Region", out var value).ShouldBeTrue();
        value.ShouldBe("East");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ABlankParameterIsInScopeWithNoValue()
    {
        var scope = new FlowVariables();
        scope.DeclareBlankParameter("Region", isReadOnly: false);

        scope.IsDeclared("Region").ShouldBeTrue();
        scope.IsBlank("Region").ShouldBeTrue();
        scope.TryGet("Region", out _).ShouldBeFalse();
        scope.Snapshot().ShouldContainKeyAndValue("Region", null);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ASetOnABlankMutableParameterGivesItItsFirstValue()
    {
        var scope = new FlowVariables();
        scope.DeclareBlankParameter("Region", isReadOnly: false);

        scope.TrySet("Region", "East").ShouldBeTrue();

        scope.IsBlank("Region").ShouldBeFalse();
        scope.TryGet("Region", out var value).ShouldBeTrue();
        value.ShouldBe("East");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ABlankReadOnlyParameterStaysBlank()
    {
        var scope = new FlowVariables();
        scope.DeclareBlankParameter("Region", isReadOnly: true);

        scope.TrySet("Region", "East").ShouldBeFalse();

        scope.IsBlank("Region").ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AChildScopeSeesAParameterAndCannotSetAReadOnlyOne()
    {
        var parent = new FlowVariables();
        parent.DeclareParameter("Region", "West", isReadOnly: true);
        parent.DeclareParameter("Limit", 5, isReadOnly: false);
        var child = parent.CreateChild();

        child.TryGet("Region", out var region).ShouldBeTrue();
        region.ShouldBe("West");
        child.TrySet("Region", "East").ShouldBeFalse();
        child.TrySet("Limit", 6).ShouldBeTrue();
        parent.TryGet("Limit", out var limit).ShouldBeTrue();
        limit.ShouldBe(6);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ASetVariableStepRefusesAReadOnlyParameterAndSaysSo()
    {
        var scope = new FlowVariables();
        scope.DeclareParameter("Region", "West", isReadOnly: true);
        var step = new FlowSetVariable("change", "Region", _ => "East");

        var ran = await step.Run(new FlowStepContext(scope, new FlowExecutor(), new FlowRunOptions { LinkCapacity = 2 }, null, null), TestContext.Current.CancellationToken);

        ran.IsFailure.ShouldBeTrue();
        ran.CurrentMessage.ShouldContain("read-only");
        scope.TryGet("Region", out var value).ShouldBeTrue();
        value.ShouldBe("West");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TextThatNamesABlankParameterFailsNamingTheParameter()
    {
        var scope = new FlowVariables();
        scope.DeclareBlankParameter("Region", isReadOnly: true);

        var rendered = Should.Throw<InvalidOperationException>(() => VariableText.Render("region is {{Region}}", scope));
        var typed = Should.Throw<InvalidOperationException>(() => VariableText.Typed("{{Region}}")(scope));

        rendered.Message.ShouldContain("parameter 'Region'");
        typed.Message.ShouldContain("parameter 'Region'");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TextThatNamesNothingDeclaredStillSaysTheNameIsNotDeclared()
    {
        var thrown = Should.Throw<InvalidOperationException>(() => VariableText.Render("{{Nope}}", new FlowVariables()));

        thrown.Message.ShouldContain("not declared");
    }
}
