using System.Threading.Tasks;
using Fdw.Services.Etl.Projects.Abstractions;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;
using Fdw.Services.Etl.Projects.Policy;

namespace Fdw.Services.Etl.Projects.Tests.Policy;

/// <summary>
/// Tests for <see cref="EffectivePolicyResolver"/>.
/// Verifies NULL-means-inherit resolution: a NULL field on the child resolves to the
/// parent's already-resolved effective value; a non-null field on the child overrides it.
/// Covers Project (inherits from server defaults), Stage, Step, and the recursive
/// OrchestrationNode resolution, plus multi-level inheritance chains.
/// </summary>
public sealed class EffectivePolicyResolverTests
{
    private static Mock<IServerPolicyDefaultConfigurationProvider> CreateServerDefaultsMock(
        string stepFailurePolicy = "HaltStage",
        string stageFailurePolicy = "HaltProject",
        int maxParallelPipelines = 5,
        bool requireApprovalToRun = true,
        bool allowResume = false,
        bool allowCrossTenant = false,
        Guid? resiliencyPolicyId = null) =>
        ServerPolicyDefaultsStub.Returning(
            stepFailurePolicy, stageFailurePolicy, maxParallelPipelines, requireApprovalToRun, allowResume, allowCrossTenant, resiliencyPolicyId);

    private static ProjectConfiguration CreateProject(
        string? stepFailurePolicy = null,
        string? stageFailurePolicy = null,
        int? maxParallelPipelines = null,
        bool? requireApprovalToRun = null,
        bool? allowResume = null,
        bool? allowCrossTenant = null,
        Guid? resiliencyPolicyId = null) =>
        new()
        {
            Name = "Project1",
            StepFailurePolicy = stepFailurePolicy,
            StageFailurePolicy = stageFailurePolicy,
            MaxParallelPipelines = maxParallelPipelines,
            RequireApprovalToRun = requireApprovalToRun,
            AllowResume = allowResume,
            AllowCrossTenant = allowCrossTenant,
            ResiliencyPolicyId = resiliencyPolicyId,
        };

    private static StageConfiguration CreateStage(
        string? stepFailurePolicy = null,
        string? stageFailurePolicy = null,
        int? maxParallelPipelines = null,
        bool? requireApprovalToRun = null,
        bool? allowResume = null,
        bool? allowCrossTenant = null,
        Guid? resiliencyPolicyId = null) =>
        new()
        {
            Name = "Stage1",
            StepFailurePolicy = stepFailurePolicy,
            StageFailurePolicy = stageFailurePolicy,
            MaxParallelPipelines = maxParallelPipelines,
            RequireApprovalToRun = requireApprovalToRun,
            AllowResume = allowResume,
            AllowCrossTenant = allowCrossTenant,
            ResiliencyPolicyId = resiliencyPolicyId,
        };

    private static StepConfiguration CreateStep(
        string? stepFailurePolicy = null,
        string? stageFailurePolicy = null,
        int? maxParallelPipelines = null,
        bool? requireApprovalToRun = null,
        bool? allowResume = null,
        bool? allowCrossTenant = null,
        Guid? resiliencyPolicyId = null) =>
        new()
        {
            Name = "Step1",
            StepFailurePolicy = stepFailurePolicy,
            StageFailurePolicy = stageFailurePolicy,
            MaxParallelPipelines = maxParallelPipelines,
            RequireApprovalToRun = requireApprovalToRun,
            AllowResume = allowResume,
            AllowCrossTenant = allowCrossTenant,
            ResiliencyPolicyId = resiliencyPolicyId,
        };

    private static OrchestrationNodeImplementationConfiguration CreateNode(
        string? stepFailurePolicy = null,
        string? stageFailurePolicy = null,
        int? maxParallelPipelines = null,
        bool? requireApprovalToRun = null,
        bool? allowResume = null,
        bool? allowCrossTenant = null,
        Guid? resiliencyPolicyId = null) =>
        new()
        {
            Name = "Node1",
            StepFailurePolicy = stepFailurePolicy,
            StageFailurePolicy = stageFailurePolicy,
            MaxParallelPipelines = maxParallelPipelines,
            RequireApprovalToRun = requireApprovalToRun,
            AllowResume = allowResume,
            AllowCrossTenant = allowCrossTenant,
            ResiliencyPolicyId = resiliencyPolicyId,
        };

    // ============================================================
    // Constructor guard
    // ============================================================

    // ============================================================
    // ResolveForProject — inherits from server defaults
    // ============================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task ResolveForProjectInheritsServerDefaultsWhenAllFieldsNull()
    {
        // Arrange
        var resiliencyId = Guid.NewGuid();
        var defaults = CreateServerDefaultsMock(resiliencyPolicyId: resiliencyId);
        var sut = new EffectivePolicyResolver(defaults.Object);
        var project = CreateProject();

        // Act
        var effective = (await sut.ResolveForProject(project, TestContext.Current.CancellationToken)).Value.ShouldNotBeNull();

        // Assert
        effective.StepFailurePolicy.ShouldBe("HaltStage");
        effective.StageFailurePolicy.ShouldBe("HaltProject");
        effective.MaxParallelPipelines.ShouldBe(5);
        effective.RequireApprovalToRun.ShouldBeTrue();
        effective.AllowResume.ShouldBeFalse();
        effective.AllowCrossTenant.ShouldBeFalse();
        effective.ResiliencyPolicyId.ShouldBe(resiliencyId);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task ResolveForProjectUsesExplicitValuesWhenAllFieldsSet()
    {
        // Arrange
        var defaults = CreateServerDefaultsMock();
        var sut = new EffectivePolicyResolver(defaults.Object);
        var resiliencyId = Guid.NewGuid();
        var project = CreateProject(
            stepFailurePolicy: "ContinueStage",
            stageFailurePolicy: "ContinueProject",
            maxParallelPipelines: 99,
            requireApprovalToRun: false,
            allowResume: true,
            allowCrossTenant: true,
            resiliencyPolicyId: resiliencyId);

        // Act
        var effective = (await sut.ResolveForProject(project, TestContext.Current.CancellationToken)).Value.ShouldNotBeNull();

        // Assert
        effective.StepFailurePolicy.ShouldBe("ContinueStage");
        effective.StageFailurePolicy.ShouldBe("ContinueProject");
        effective.MaxParallelPipelines.ShouldBe(99);
        effective.RequireApprovalToRun.ShouldBeFalse();
        effective.AllowResume.ShouldBeTrue();
        effective.AllowCrossTenant.ShouldBeTrue();
        effective.ResiliencyPolicyId.ShouldBe(resiliencyId);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task ResolveForProjectMixesExplicitAndInheritedFields()
    {
        // Arrange — only StepFailurePolicy and AllowCrossTenant are explicit; the rest inherit
        var defaults = CreateServerDefaultsMock(
            stageFailurePolicy: "HaltProject",
            maxParallelPipelines: 5,
            requireApprovalToRun: true,
            allowResume: false);
        var sut = new EffectivePolicyResolver(defaults.Object);
        var project = CreateProject(stepFailurePolicy: "ContinueStage", allowCrossTenant: true);

        // Act
        var effective = (await sut.ResolveForProject(project, TestContext.Current.CancellationToken)).Value.ShouldNotBeNull();

        // Assert
        effective.StepFailurePolicy.ShouldBe("ContinueStage");
        effective.AllowCrossTenant.ShouldBeTrue();
        effective.StageFailurePolicy.ShouldBe("HaltProject");
        effective.MaxParallelPipelines.ShouldBe(5);
        effective.RequireApprovalToRun.ShouldBeTrue();
        effective.AllowResume.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ResolveForProjectFailsWhenTheServerDefaultsCannotBeRead()
    {
        // Arrange: the server's defaults are configuration, so a failed read is the resolution's failure (FDW-3580).
        var sut = new EffectivePolicyResolver(ServerPolicyDefaultsStub.Failing().Object);

        // Act
        var effective = await sut.ResolveForProject(CreateProject(), TestContext.Current.CancellationToken);

        // Assert
        effective.IsFailure.ShouldBeTrue();
    }

    // ============================================================
    // ResolveForStage — inherits from parent project's effective policy
    // ============================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ResolveForStageInheritsParentProjectEffectiveWhenAllFieldsNull()
    {
        // Arrange
        var sut = new EffectivePolicyResolver(CreateServerDefaultsMock().Object);
        var parentEffective = new ExecutionPolicySnapshot(
            "ContinueStage", "ContinueProject", 7, false, true, true, Guid.NewGuid());
        var stage = CreateStage();

        // Act
        var effective = sut.ResolveForStage(stage, parentEffective);

        // Assert
        effective.ShouldBe(parentEffective);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ResolveForStageUsesExplicitValuesOverParentEffective()
    {
        // Arrange
        var sut = new EffectivePolicyResolver(CreateServerDefaultsMock().Object);
        var parentEffective = new ExecutionPolicySnapshot(
            "ContinueStage", "ContinueProject", 7, false, true, true, null);
        var resiliencyId = Guid.NewGuid();
        var stage = CreateStage(
            stepFailurePolicy: "HaltStage",
            stageFailurePolicy: "HaltProject",
            maxParallelPipelines: 1,
            requireApprovalToRun: true,
            allowResume: false,
            allowCrossTenant: false,
            resiliencyPolicyId: resiliencyId);

        // Act
        var effective = sut.ResolveForStage(stage, parentEffective);

        // Assert
        effective.StepFailurePolicy.ShouldBe("HaltStage");
        effective.StageFailurePolicy.ShouldBe("HaltProject");
        effective.MaxParallelPipelines.ShouldBe(1);
        effective.RequireApprovalToRun.ShouldBeTrue();
        effective.AllowResume.ShouldBeFalse();
        effective.AllowCrossTenant.ShouldBeFalse();
        effective.ResiliencyPolicyId.ShouldBe(resiliencyId);
    }

    // ============================================================
    // ResolveForStep — inherits from parent stage's effective policy
    // ============================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ResolveForStepInheritsParentStageEffectiveWhenAllFieldsNull()
    {
        // Arrange
        var sut = new EffectivePolicyResolver(CreateServerDefaultsMock().Object);
        var parentEffective = new ExecutionPolicySnapshot(
            "HaltStage", "HaltProject", 2, true, false, false, Guid.NewGuid());
        var step = CreateStep();

        // Act
        var effective = sut.ResolveForStep(step, parentEffective);

        // Assert
        effective.ShouldBe(parentEffective);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ResolveForStepUsesExplicitValuesOverParentEffective()
    {
        // Arrange
        var sut = new EffectivePolicyResolver(CreateServerDefaultsMock().Object);
        var parentEffective = new ExecutionPolicySnapshot(
            "HaltStage", "HaltProject", 5, true, false, false, null);
        var step = CreateStep(maxParallelPipelines: 1, allowResume: false);

        // Act
        var effective = sut.ResolveForStep(step, parentEffective);

        // Assert
        effective.MaxParallelPipelines.ShouldBe(1);
        effective.AllowResume.ShouldBeFalse();
        effective.StepFailurePolicy.ShouldBe(parentEffective.StepFailurePolicy);
        effective.StageFailurePolicy.ShouldBe(parentEffective.StageFailurePolicy);
    }

    // ============================================================
    // ResolveForNode — recursive node resolution
    // ============================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ResolveForNodeInheritsParentEffectiveWhenAllFieldsNull()
    {
        // Arrange
        var sut = new EffectivePolicyResolver(CreateServerDefaultsMock().Object);
        var parentEffective = new ExecutionPolicySnapshot(
            "HaltStage", "HaltProject", 3, true, false, false, Guid.NewGuid());
        var node = CreateNode();

        // Act
        var effective = sut.ResolveForNode(node, parentEffective);

        // Assert
        effective.ShouldBe(parentEffective);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ResolveForNodeUsesExplicitValuesOverParentEffective()
    {
        // Arrange
        var sut = new EffectivePolicyResolver(CreateServerDefaultsMock().Object);
        var parentEffective = new ExecutionPolicySnapshot(
            "HaltStage", "HaltProject", 3, true, false, false, null);
        var node = CreateNode(allowCrossTenant: true, maxParallelPipelines: 10);

        // Act
        var effective = sut.ResolveForNode(node, parentEffective);

        // Assert
        effective.AllowCrossTenant.ShouldBeTrue();
        effective.MaxParallelPipelines.ShouldBe(10);
        effective.StepFailurePolicy.ShouldBe(parentEffective.StepFailurePolicy);
    }

    // ============================================================
    // Multi-level inheritance chain: Server -> Project -> Stage -> Step
    // ============================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task ResolveChainPropagatesServerDefaultThroughAllLevelsWhenNeverOverridden()
    {
        // Arrange — nothing is set anywhere in the chain; the leaf must resolve to the
        // server default that was set three levels above it.
        var defaults = CreateServerDefaultsMock(allowCrossTenant: false);
        var sut = new EffectivePolicyResolver(defaults.Object);
        var project = CreateProject();
        var stage = CreateStage();
        var step = CreateStep();

        // Act
        var projectEffective = (await sut.ResolveForProject(project, TestContext.Current.CancellationToken)).Value.ShouldNotBeNull();
        var stageEffective = sut.ResolveForStage(stage, projectEffective);
        var stepEffective = sut.ResolveForStep(step, stageEffective);

        // Assert
        stepEffective.AllowCrossTenant.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task ResolveChainStopsInheritingAtTheLevelThatSetsAnExplicitValue()
    {
        // Arrange — Project sets MaxParallelPipelines=2; Stage leaves it null (inherits the
        // Project's 2); Step leaves it null too (inherits the Stage's resolved 2).
        var defaults = CreateServerDefaultsMock(maxParallelPipelines: 99);
        var sut = new EffectivePolicyResolver(defaults.Object);
        var project = CreateProject(maxParallelPipelines: 2);
        var stage = CreateStage();
        var step = CreateStep();

        // Act
        var projectEffective = (await sut.ResolveForProject(project, TestContext.Current.CancellationToken)).Value.ShouldNotBeNull();
        var stageEffective = sut.ResolveForStage(stage, projectEffective);
        var stepEffective = sut.ResolveForStep(step, stageEffective);

        // Assert
        projectEffective.MaxParallelPipelines.ShouldBe(2);
        stageEffective.MaxParallelPipelines.ShouldBe(2);
        stepEffective.MaxParallelPipelines.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task ResolveChainAllowsEachLevelToOverrideIndependently()
    {
        // Arrange — Project sets StepFailurePolicy; Stage overrides StageFailurePolicy;
        // Step overrides MaxParallelPipelines. Each override is independent of the others.
        var defaults = CreateServerDefaultsMock(
            stepFailurePolicy: "ContinueStage",
            stageFailurePolicy: "ContinueProject",
            maxParallelPipelines: 50);
        var sut = new EffectivePolicyResolver(defaults.Object);
        var project = CreateProject(stepFailurePolicy: "HaltStage");
        var stage = CreateStage(stageFailurePolicy: "HaltProject");
        var step = CreateStep(maxParallelPipelines: 4);

        // Act
        var projectEffective = (await sut.ResolveForProject(project, TestContext.Current.CancellationToken)).Value.ShouldNotBeNull();
        var stageEffective = sut.ResolveForStage(stage, projectEffective);
        var stepEffective = sut.ResolveForStep(step, stageEffective);

        // Assert
        stepEffective.StepFailurePolicy.ShouldBe("HaltStage");       // from Project
        stepEffective.StageFailurePolicy.ShouldBe("HaltProject");    // from Stage
        stepEffective.MaxParallelPipelines.ShouldBe(4);              // from Step
    }

    // ============================================================
    // ResolveForRootNode — a node with no parent node inherits from the server defaults (FDW-1740)
    // ============================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task ResolveForRootNodeInheritsServerDefaultApprovalWhenNodeStatesNone()
    {
        // Arrange
        var sut = new EffectivePolicyResolver(CreateServerDefaultsMock(requireApprovalToRun: true).Object);

        // Act
        var effective = (await sut.ResolveForRootNode(CreateNode(), TestContext.Current.CancellationToken)).Value.ShouldNotBeNull();

        // Assert
        effective.RequireApprovalToRun.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task ResolveForRootNodeUsesTheNodesOwnApprovalOverTheServerDefault()
    {
        // Arrange
        var sut = new EffectivePolicyResolver(CreateServerDefaultsMock(requireApprovalToRun: false).Object);

        // Act
        var effective = (await sut.ResolveForRootNode(CreateNode(requireApprovalToRun: true), TestContext.Current.CancellationToken)).Value.ShouldNotBeNull();

        // Assert
        effective.RequireApprovalToRun.ShouldBeTrue();
        effective.MaxParallelPipelines.ShouldBe(5);
    }
}
