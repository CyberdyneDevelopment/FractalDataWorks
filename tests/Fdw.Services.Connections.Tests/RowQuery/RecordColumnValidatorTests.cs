using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Services.Connections.RowQuery;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Connections.Tests.RowQuery;

/// <summary>
/// Unit coverage for <see cref="RecordColumnValidator"/> — fix #2: a filter or join column that is NOT
/// a declared field on its target container is a schema/configuration error and must fail loud, never
/// silently resolve to null and exclude the row.
/// </summary>
public sealed class RecordColumnValidatorTests
{
    [Fact]
    [Trait("Category", "RowQuery")]
    [Trait("Category", "UndeclaredColumn")]
    public void ValidateFilterColumnsFailsWhenABareColumnIsNotDeclaredOnThePrimaryContainer()
    {
        var primary = ContainerStub.Build("EnvironmentVariableSecretManager", ("SecretManagerId", false));
        var condition = new FilterCondition { PropertyName = "Typo", Operator = new EqualOperator(), Value = true };

        var result = RecordColumnValidator.ValidateFilterColumns(condition, primary, null, null, NullLogger.Instance);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Category", "RowQuery")]
    public void ValidateFilterColumnsSucceedsWhenTheBareColumnIsDeclared()
    {
        var primary = ContainerStub.Build("EnvironmentVariableSecretManager", ("SecretManagerId", false));
        var condition = new FilterCondition { PropertyName = "SecretManagerId", Operator = new EqualOperator(), Value = true };

        var result = RecordColumnValidator.ValidateFilterColumns(condition, primary, null, null, NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Category", "RowQuery")]
    [Trait("Category", "UndeclaredColumn")]
    public void ValidateFilterColumnsFailsWhenADottedColumnIsNotDeclaredOnTheJoinedContainer()
    {
        // Why (fix #2 proof, joined side): "SecretManager.IsCurrent" is well-formed and qualifies to the
        // right container by name, but IsCurrent must still be a DECLARED field on that container.
        var primary = ContainerStub.Build("EnvironmentVariableSecretManager", ("SecretManagerRowId", false));
        var joined = ContainerStub.Build("SecretManager", ("RowId", false));
        var condition = new FilterCondition { PropertyName = "SecretManager.IsCurrent", Operator = new EqualOperator(), Value = true };

        var result = RecordColumnValidator.ValidateFilterColumns(condition, primary, joined, "SecretManager", NullLogger.Instance);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Category", "RowQuery")]
    public void ValidateFilterColumnsSucceedsWhenTheDottedColumnIsDeclaredOnTheJoinedContainer()
    {
        var primary = ContainerStub.Build("EnvironmentVariableSecretManager", ("SecretManagerRowId", false));
        var joined = ContainerStub.Build("SecretManager", ("RowId", false), ("IsCurrent", false));
        var condition = new FilterCondition { PropertyName = "SecretManager.IsCurrent", Operator = new EqualOperator(), Value = true };

        var result = RecordColumnValidator.ValidateFilterColumns(condition, primary, joined, "SecretManager", NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Category", "RowQuery")]
    [Trait("Category", "UnknownQualifier")]
    public void ValidateFilterColumnsFailsWhenTheQualifierNamesNeitherContainer()
    {
        var primary = ContainerStub.Build("EnvironmentVariableSecretManager", ("SecretManagerRowId", false));
        var joined = ContainerStub.Build("SecretManager", ("RowId", false));
        var condition = new FilterCondition { PropertyName = "UnknownContainer.IsCurrent", Operator = new EqualOperator(), Value = true };

        var result = RecordColumnValidator.ValidateFilterColumns(condition, primary, joined, "SecretManager", NullLogger.Instance);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Category", "RowQuery")]
    public void ValidateFilterColumnsWalksAnAndGroupAndFailsOnTheOffendingCondition()
    {
        var primary = ContainerStub.Build("SecretManager", ("IsCurrent", false), ("IsDeleted", false));
        var group = new FilterGroup
        {
            Operator = LogicalOperator.And,
            Nodes =
            [
                new FilterCondition { PropertyName = "IsCurrent", Operator = new EqualOperator(), Value = true },
                new FilterCondition { PropertyName = "Typo", Operator = new EqualOperator(), Value = false }
            ]
        };

        var result = RecordColumnValidator.ValidateFilterColumns(group, primary, null, null, NullLogger.Instance);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Category", "RowQuery")]
    [Trait("Category", "UndeclaredColumn")]
    public void ValidateJoinColumnsFailsWhenTheLeftFieldIsNotDeclaredOnThePrimaryContainer()
    {
        var primary = ContainerStub.Build("EnvironmentVariableSecretManager", ("Id", false));
        var joined = ContainerStub.Build("SecretManager", ("RowId", false));

        var result = RecordColumnValidator.ValidateJoinColumns("SecretManagerRowId", "RowId", primary, joined, NullLogger.Instance);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Category", "RowQuery")]
    [Trait("Category", "UndeclaredColumn")]
    public void ValidateJoinColumnsFailsWhenTheRightFieldIsNotDeclaredOnTheJoinedContainer()
    {
        var primary = ContainerStub.Build("EnvironmentVariableSecretManager", ("SecretManagerRowId", false));
        var joined = ContainerStub.Build("SecretManager", ("Id", false));

        var result = RecordColumnValidator.ValidateJoinColumns("SecretManagerRowId", "RowId", primary, joined, NullLogger.Instance);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Category", "RowQuery")]
    public void ValidateJoinColumnsSucceedsWhenBothFieldsAreDeclared()
    {
        var primary = ContainerStub.Build("EnvironmentVariableSecretManager", ("SecretManagerRowId", false));
        var joined = ContainerStub.Build("SecretManager", ("RowId", false));

        var result = RecordColumnValidator.ValidateJoinColumns("SecretManagerRowId", "RowId", primary, joined, NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue();
    }
}
