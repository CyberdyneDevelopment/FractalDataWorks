using Fdw.Services.Etl.Projects.Abstractions;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;
using Fdw.Services.Etl.Projects.Policy;

namespace Fdw.Services.Etl.Projects.Tests.Policy;

/// <summary>
/// Tests for <see cref="PolicyElevationValidator"/>.
/// Enforces least-privilege inheritance: a child policy field may only be equal to or
/// stricter than the parent's effective policy. The AllowCrossTenant field is the
/// tenant-isolation boundary — elevating it false-&gt;true on a child MUST be rejected.
/// </summary>
public sealed class PolicyElevationValidatorTests
{
    private readonly PolicyElevationValidator _sut = new();

    // Parent effective policy: every field at its strictest commonly-used baseline value,
    // except MaxParallelPipelines which needs headroom to test "exceeds parent" violations.
    private static ExecutionPolicySnapshot CreateParentEffective(
        string stepFailurePolicy = "HaltStage",
        string stageFailurePolicy = "HaltProject",
        int maxParallelPipelines = 5,
        bool requireApprovalToRun = true,
        bool allowResume = false,
        bool allowCrossTenant = false,
        Guid? resiliencyPolicyId = null) =>
        new(stepFailurePolicy, stageFailurePolicy, maxParallelPipelines, requireApprovalToRun,
            allowResume, allowCrossTenant, resiliencyPolicyId);

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

    // ============================================================
    // Stage: null-field (full inherit) and equal-value cases
    // ============================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ValidateStageReturnsSuccessWhenAllFieldsAreNull()
    {
        // Arrange
        var stage = CreateStage();
        var parent = CreateParentEffective();

        // Act
        var result = _sut.Validate(stage, parent);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ValidateStageReturnsSuccessWhenAllFieldsEqualParent()
    {
        // Arrange
        var parent = CreateParentEffective();
        var stage = CreateStage(
            stepFailurePolicy: parent.StepFailurePolicy,
            stageFailurePolicy: parent.StageFailurePolicy,
            maxParallelPipelines: parent.MaxParallelPipelines,
            requireApprovalToRun: parent.RequireApprovalToRun,
            allowResume: parent.AllowResume,
            allowCrossTenant: parent.AllowCrossTenant);

        // Act
        var result = _sut.Validate(stage, parent);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    // ============================================================
    // Stage: StepFailurePolicy (HaltStage > ContinueStage)
    // ============================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ValidateStageReturnsFailureWhenStepFailurePolicyContinueAndParentHalt()
    {
        // Arrange
        var parent = CreateParentEffective(stepFailurePolicy: "HaltStage");
        var stage = CreateStage(stepFailurePolicy: "ContinueStage");

        // Act
        var result = _sut.Validate(stage, parent);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage!.ShouldContain("StepFailurePolicy");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ValidateStageReturnsFailureWhenStepFailurePolicyContinueMatchedCaseInsensitively()
    {
        // Arrange — comparisons use StringComparison.OrdinalIgnoreCase
        var parent = CreateParentEffective(stepFailurePolicy: "HALTSTAGE");
        var stage = CreateStage(stepFailurePolicy: "continuestage");

        // Act
        var result = _sut.Validate(stage, parent);

        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ValidateStageReturnsSuccessWhenStepFailurePolicyHaltAndParentContinue()
    {
        // Arrange — child is stricter than parent: always allowed
        var parent = CreateParentEffective(stepFailurePolicy: "ContinueStage");
        var stage = CreateStage(stepFailurePolicy: "HaltStage");

        // Act
        var result = _sut.Validate(stage, parent);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    // ============================================================
    // Stage: StageFailurePolicy (HaltProject > ContinueProject)
    // ============================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ValidateStageReturnsFailureWhenStageFailurePolicyContinueAndParentHalt()
    {
        // Arrange
        var parent = CreateParentEffective(stageFailurePolicy: "HaltProject");
        var stage = CreateStage(stageFailurePolicy: "ContinueProject");

        // Act
        var result = _sut.Validate(stage, parent);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage!.ShouldContain("StageFailurePolicy");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ValidateStageReturnsSuccessWhenStageFailurePolicyHaltAndParentContinue()
    {
        // Arrange
        var parent = CreateParentEffective(stageFailurePolicy: "ContinueProject");
        var stage = CreateStage(stageFailurePolicy: "HaltProject");

        // Act
        var result = _sut.Validate(stage, parent);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    // ============================================================
    // Stage: MaxParallelPipelines (lower is stricter)
    // ============================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ValidateStageReturnsFailureWhenMaxParallelPipelinesExceedsParent()
    {
        // Arrange
        var parent = CreateParentEffective(maxParallelPipelines: 5);
        var stage = CreateStage(maxParallelPipelines: 6);

        // Act
        var result = _sut.Validate(stage, parent);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage!.ShouldContain("MaxParallelPipelines");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ValidateStageReturnsSuccessWhenMaxParallelPipelinesEqualsParent()
    {
        // Arrange
        var parent = CreateParentEffective(maxParallelPipelines: 5);
        var stage = CreateStage(maxParallelPipelines: 5);

        // Act
        var result = _sut.Validate(stage, parent);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ValidateStageReturnsSuccessWhenMaxParallelPipelinesLowerThanParent()
    {
        // Arrange
        var parent = CreateParentEffective(maxParallelPipelines: 5);
        var stage = CreateStage(maxParallelPipelines: 1);

        // Act
        var result = _sut.Validate(stage, parent);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    // ============================================================
    // Stage: RequireApprovalToRun (true is stricter)
    // ============================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ValidateStageReturnsFailureWhenRequireApprovalToRunFalseAndParentTrue()
    {
        // Arrange
        var parent = CreateParentEffective(requireApprovalToRun: true);
        var stage = CreateStage(requireApprovalToRun: false);

        // Act
        var result = _sut.Validate(stage, parent);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage!.ShouldContain("RequireApprovalToRun");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ValidateStageReturnsSuccessWhenRequireApprovalToRunTrueAndParentFalse()
    {
        // Arrange
        var parent = CreateParentEffective(requireApprovalToRun: false);
        var stage = CreateStage(requireApprovalToRun: true);

        // Act
        var result = _sut.Validate(stage, parent);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    // ============================================================
    // Stage: AllowResume (false is stricter)
    // ============================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ValidateStageReturnsFailureWhenAllowResumeTrueAndParentFalse()
    {
        // Arrange
        var parent = CreateParentEffective(allowResume: false);
        var stage = CreateStage(allowResume: true);

        // Act
        var result = _sut.Validate(stage, parent);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage!.ShouldContain("AllowResume");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ValidateStageReturnsSuccessWhenAllowResumeFalseAndParentTrue()
    {
        // Arrange
        var parent = CreateParentEffective(allowResume: true);
        var stage = CreateStage(allowResume: false);

        // Act
        var result = _sut.Validate(stage, parent);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    // ============================================================
    // Stage: AllowCrossTenant — TENANT ISOLATION BOUNDARY (P0/Security)
    // ============================================================

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ValidateStageReturnsFailureWhenAllowCrossTenantElevatedFromFalseToTrue()
    {
        // Arrange — child attempts to enable cross-tenant composition the parent forbids
        var parent = CreateParentEffective(allowCrossTenant: false);
        var stage = CreateStage(allowCrossTenant: true);

        // Act
        var result = _sut.Validate(stage, parent);

        // Assert — fail loud, never silently permit a tenant-isolation bypass
        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage!.ShouldContain("AllowCrossTenant");
        result.Messages.ShouldContain(m => m.Code == "PROJECTS-41008");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ValidateStageReturnsSuccessWhenAllowCrossTenantNarrowedFromTrueToFalse()
    {
        // Arrange — child is stricter than parent: always allowed
        var parent = CreateParentEffective(allowCrossTenant: true);
        var stage = CreateStage(allowCrossTenant: false);

        // Act
        var result = _sut.Validate(stage, parent);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ValidateStageReturnsSuccessWhenAllowCrossTenantEqualsParentTrue()
    {
        // Arrange — equal value (true == true) is not an elevation
        var parent = CreateParentEffective(allowCrossTenant: true);
        var stage = CreateStage(allowCrossTenant: true);

        // Act
        var result = _sut.Validate(stage, parent);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ValidateStageReturnsSuccessWhenAllowCrossTenantEqualsParentFalse()
    {
        // Arrange — equal value (false == false) is not an elevation
        var parent = CreateParentEffective(allowCrossTenant: false);
        var stage = CreateStage(allowCrossTenant: false);

        // Act
        var result = _sut.Validate(stage, parent);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    // ============================================================
    // Stage: ResiliencyPolicyId — not ordered, any override allowed
    // ============================================================

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void ValidateStageReturnsSuccessWhenResiliencyPolicyIdOverridesParent()
    {
        // Arrange — ResiliencyPolicyId has no elevation ordering: any child value is valid
        var parent = CreateParentEffective(resiliencyPolicyId: Guid.NewGuid());
        var stage = CreateStage(resiliencyPolicyId: Guid.NewGuid());

        // Act
        var result = _sut.Validate(stage, parent);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    // ============================================================
    // Stage: multiple simultaneous violations
    // ============================================================

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ValidateStageReturnsFailureListingAllViolationsWhenMultipleFieldsElevated()
    {
        // Arrange
        var parent = CreateParentEffective(requireApprovalToRun: true, allowCrossTenant: false);
        var stage = CreateStage(requireApprovalToRun: false, allowCrossTenant: true);

        // Act
        var result = _sut.Validate(stage, parent);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage!.ShouldContain("RequireApprovalToRun");
        result.CurrentMessage!.ShouldContain("AllowCrossTenant");
    }

    // ============================================================
    // Stage: guard clauses
    // ============================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ValidateStageThrowsWhenStageIsNull()
    {
        // Arrange
        var parent = CreateParentEffective();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _sut.Validate((StageConfiguration)null!, parent));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ValidateStageThrowsWhenParentEffectiveIsNull()
    {
        // Arrange
        var stage = CreateStage();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _sut.Validate(stage, null!));
    }

    // ============================================================
    // Step: mirrors Stage semantics — representative coverage
    // ============================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ValidateStepReturnsSuccessWhenAllFieldsAreNull()
    {
        // Arrange
        var step = CreateStep();
        var parent = CreateParentEffective();

        // Act
        var result = _sut.Validate(step, parent);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ValidateStepReturnsFailureWhenAllowCrossTenantElevatedFromFalseToTrue()
    {
        // Arrange — same tenant-isolation boundary enforced at Step level
        var parent = CreateParentEffective(allowCrossTenant: false);
        var step = CreateStep(allowCrossTenant: true);

        // Act
        var result = _sut.Validate(step, parent);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage!.ShouldContain("AllowCrossTenant");
        result.Messages.ShouldContain(m => m.Code == "PROJECTS-41008");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ValidateStepReturnsSuccessWhenAllowCrossTenantNarrowedFromTrueToFalse()
    {
        // Arrange
        var parent = CreateParentEffective(allowCrossTenant: true);
        var step = CreateStep(allowCrossTenant: false);

        // Act
        var result = _sut.Validate(step, parent);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ValidateStepReturnsFailureWhenMaxParallelPipelinesExceedsParent()
    {
        // Arrange
        var parent = CreateParentEffective(maxParallelPipelines: 3);
        var step = CreateStep(maxParallelPipelines: 4);

        // Act
        var result = _sut.Validate(step, parent);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage!.ShouldContain("MaxParallelPipelines");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ValidateStepReturnsSuccessWhenAllFieldsNarrowerThanParent()
    {
        // Arrange — every field made stricter (or equal) than parent
        var parent = CreateParentEffective(
            stepFailurePolicy: "ContinueStage",
            stageFailurePolicy: "ContinueProject",
            maxParallelPipelines: 10,
            requireApprovalToRun: false,
            allowResume: true,
            allowCrossTenant: true);
        var step = CreateStep(
            stepFailurePolicy: "HaltStage",
            stageFailurePolicy: "HaltProject",
            maxParallelPipelines: 1,
            requireApprovalToRun: true,
            allowResume: false,
            allowCrossTenant: false);

        // Act
        var result = _sut.Validate(step, parent);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ValidateStepThrowsWhenStepIsNull()
    {
        // Arrange
        var parent = CreateParentEffective();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _sut.Validate((StepConfiguration)null!, parent));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ValidateStepThrowsWhenParentEffectiveIsNull()
    {
        // Arrange
        var step = CreateStep();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _sut.Validate(step, null!));
    }
}
