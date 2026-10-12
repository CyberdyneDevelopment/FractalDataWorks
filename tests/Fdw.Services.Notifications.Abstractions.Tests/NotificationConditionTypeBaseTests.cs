using Fdw.Services.Notifications.Abstractions;
using Fdw.Results;

namespace Fdw.Services.Notifications.Abstractions.Tests;

/// <summary>
/// Tests for NotificationConditionTypeBase class.
/// </summary>
public class NotificationConditionTypeBaseTests
{
    [ExcludeFromCodeCoverage]
    private sealed class TestCondition : NotificationConditionTypeBase
    {
        public TestCondition(string name, string icon, string color)
            : base(name, icon, color)
        {
        }

        public override IGenericResult<bool> Evaluate(NotificationContext context)
        {
            return GenericResult<bool>.Success(true);
        }
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorSetsAllProperties()
    {
        // Arrange & Act
        var condition = new TestCondition("TestCondition", "test_icon", "Primary");
        condition.Name.ShouldBe("TestCondition");
        condition.Icon.ShouldBe("test_icon");
        condition.Color.ShouldBe("Primary");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void EvaluateCanBeOverridden()
    {
        // Arrange
        var condition = new TestCondition("TestCondition", "icon", "color");
        var context = new NotificationContext();

        // Act
        var result = condition.Evaluate(context);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeTrue();
    }
}
