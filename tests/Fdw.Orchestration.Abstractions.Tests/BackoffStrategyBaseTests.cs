using System;
using Fdw.Orchestration.Abstractions.TypeCollections.BackoffStrategyOptions;
using Fdw.Orchestration.Abstractions.TypeCollections;

namespace Fdw.Orchestration.Abstractions.Tests;

public class BackoffStrategyBaseTests
{
    private sealed class ConstantBackoffStrategy : BackoffStrategyBase
    {
        public ConstantBackoffStrategy(
            int id,
            string name,
            TimeSpan initialDelay,
            TimeSpan maxDelay,
            double multiplier = 2.0,
            double jitterFactor = 0.0)
            : base(id, name, initialDelay, maxDelay, multiplier, jitterFactor)
        {
        }

        public override TimeSpan GetDelay(int attemptNumber) => InitialDelay;

        public override string GetPollyBackoffTypeName() => "Constant";

        // Expose protected methods for testing
        public TimeSpan TestApplyJitter(TimeSpan delay) => ApplyJitter(delay);
        public TimeSpan TestClampToMax(TimeSpan delay) => ClampToMax(delay);
    }

    private sealed class ExponentialBackoffStrategy : BackoffStrategyBase
    {
        public ExponentialBackoffStrategy(
            int id,
            string name,
            TimeSpan initialDelay,
            TimeSpan maxDelay,
            double multiplier = 2.0,
            double jitterFactor = 0.0)
            : base(id, name, initialDelay, maxDelay, multiplier, jitterFactor)
        {
        }

        public override TimeSpan GetDelay(int attemptNumber)
        {
            var delay = TimeSpan.FromMilliseconds(
                InitialDelay.TotalMilliseconds * Math.Pow(Multiplier, attemptNumber - 1));
            return ClampToMax(delay);
        }

        public override string GetPollyBackoffTypeName() => "Exponential";
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ConstructorSetsAllProperties()
    {
        var strategy = new ConstantBackoffStrategy(
            1, "Constant",
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(30),
            multiplier: 1.5,
            jitterFactor: 0.25);

        strategy.Id.ShouldBe(1);
        strategy.Name.ShouldBe("Constant");
        strategy.InitialDelay.ShouldBe(TimeSpan.FromSeconds(1));
        strategy.MaxDelay.ShouldBe(TimeSpan.FromSeconds(30));
        strategy.Multiplier.ShouldBe(1.5);
        strategy.JitterFactor.ShouldBe(0.25);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void UsesJitterReturnsTrueWhenJitterFactorPositive()
    {
        var strategy = new ConstantBackoffStrategy(
            1, "Test",
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(30),
            jitterFactor: 0.1);

        strategy.UsesJitter.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void UsesJitterReturnsFalseWhenJitterFactorZero()
    {
        var strategy = new ConstantBackoffStrategy(
            1, "Test",
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(30),
            jitterFactor: 0.0);

        strategy.UsesJitter.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void JitterFactorClampedToZeroWhenNegative()
    {
        var strategy = new ConstantBackoffStrategy(
            1, "Test",
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(30),
            jitterFactor: -0.5);

        strategy.JitterFactor.ShouldBe(0.0);
        strategy.UsesJitter.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void JitterFactorClampedToOneWhenAboveOne()
    {
        var strategy = new ConstantBackoffStrategy(
            1, "Test",
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(30),
            jitterFactor: 2.5);

        strategy.JitterFactor.ShouldBe(1.0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void DefaultMultiplierIsTwo()
    {
        var strategy = new ConstantBackoffStrategy(
            1, "Test",
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(30));

        strategy.Multiplier.ShouldBe(2.0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void DefaultJitterFactorIsZero()
    {
        var strategy = new ConstantBackoffStrategy(
            1, "Test",
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(30));

        strategy.JitterFactor.ShouldBe(0.0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ClampToMaxReturnsMaxWhenDelayExceedsMax()
    {
        var strategy = new ConstantBackoffStrategy(
            1, "Test",
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(30));

        var result = strategy.TestClampToMax(TimeSpan.FromSeconds(60));

        result.ShouldBe(TimeSpan.FromSeconds(30));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ClampToMaxReturnsDelayWhenWithinMax()
    {
        var strategy = new ConstantBackoffStrategy(
            1, "Test",
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(30));

        var result = strategy.TestClampToMax(TimeSpan.FromSeconds(10));

        result.ShouldBe(TimeSpan.FromSeconds(10));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ClampToMaxReturnsMaxWhenEqual()
    {
        var strategy = new ConstantBackoffStrategy(
            1, "Test",
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(30));

        var result = strategy.TestClampToMax(TimeSpan.FromSeconds(30));

        result.ShouldBe(TimeSpan.FromSeconds(30));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ApplyJitterReturnsOriginalDelayWhenJitterIsZero()
    {
        var strategy = new ConstantBackoffStrategy(
            1, "Test",
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(30),
            jitterFactor: 0.0);

        var result = strategy.TestApplyJitter(TimeSpan.FromSeconds(5));

        result.ShouldBe(TimeSpan.FromSeconds(5));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ApplyJitterReturnsNonNegativeDelay()
    {
        var strategy = new ConstantBackoffStrategy(
            1, "Test",
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(30),
            jitterFactor: 1.0);

        // Run multiple times to exercise randomness
        for (int i = 0; i < 100; i++)
        {
            var result = strategy.TestApplyJitter(TimeSpan.FromMilliseconds(10));
            result.TotalMilliseconds.ShouldBeGreaterThanOrEqualTo(0);
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ApplyJitterProducesVariation()
    {
        var strategy = new ConstantBackoffStrategy(
            1, "Test",
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(30),
            jitterFactor: 0.5);

        var delays = new HashSet<double>();
        for (int i = 0; i < 50; i++)
        {
            var result = strategy.TestApplyJitter(TimeSpan.FromSeconds(10));
            delays.Add(Math.Round(result.TotalMilliseconds, 0));
        }

        // With 50% jitter on 10 seconds, we should get variation
        delays.Count.ShouldBeGreaterThan(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void GetDelayReturnsConstantForConstantStrategy()
    {
        var strategy = new ConstantBackoffStrategy(
            1, "Constant",
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(30));

        strategy.GetDelay(1).ShouldBe(TimeSpan.FromSeconds(2));
        strategy.GetDelay(2).ShouldBe(TimeSpan.FromSeconds(2));
        strategy.GetDelay(10).ShouldBe(TimeSpan.FromSeconds(2));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void GetPollyBackoffTypeNameReturnsCorrectValue()
    {
        var constant = new ConstantBackoffStrategy(
            1, "Constant",
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(30));

        var exponential = new ExponentialBackoffStrategy(
            2, "Exponential",
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(30));

        constant.GetPollyBackoffTypeName().ShouldBe("Constant");
        exponential.GetPollyBackoffTypeName().ShouldBe("Exponential");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ExponentialBackoffGrowsExponentially()
    {
        var strategy = new ExponentialBackoffStrategy(
            2, "Exponential",
            TimeSpan.FromSeconds(1),
            TimeSpan.FromMinutes(5),
            multiplier: 2.0);

        strategy.GetDelay(1).ShouldBe(TimeSpan.FromSeconds(1));
        strategy.GetDelay(2).ShouldBe(TimeSpan.FromSeconds(2));
        strategy.GetDelay(3).ShouldBe(TimeSpan.FromSeconds(4));
        strategy.GetDelay(4).ShouldBe(TimeSpan.FromSeconds(8));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ExponentialBackoffClampsToMax()
    {
        var strategy = new ExponentialBackoffStrategy(
            2, "Exponential",
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(10),
            multiplier: 2.0);

        // 2^9 = 512 seconds, should be clamped to 10
        strategy.GetDelay(10).ShouldBe(TimeSpan.FromSeconds(10));
    }
}
