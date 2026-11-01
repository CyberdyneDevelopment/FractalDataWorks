using System;
using System.Collections.Generic;
using Fdw.Services.Etl.Predicates;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Predicates;

/// <summary>
/// Tests for <see cref="RecordPredicates"/> (FDW-1120): the pipeline's condition language, read once and strictly. What it
/// reads it evaluates the way the Filter stage always did; what it cannot read it refuses, naming the text.
/// </summary>
public sealed class RecordPredicatesTests
{
    private static Dictionary<string, object?> Record() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["Name"] = "Alice",
        ["City"] = "New York",
        ["Age"] = 30,
        ["Active"] = true,
        ["Tag"] = null,
        ["Joined"] = new DateTimeOffset(2026, 3, 1, 12, 0, 0, TimeSpan.Zero),
    };

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    [InlineData("Age >= 18", true)]
    [InlineData("Age >= 31", false)]
    [InlineData("Age <= 30", true)]
    [InlineData("Age <= 29", false)]
    [InlineData("Age > 29", true)]
    [InlineData("Age > 30", false)]
    [InlineData("Age < 31", true)]
    [InlineData("Age < 30", false)]
    [InlineData("Age == 30", true)]
    [InlineData("Age != 30", false)]
    [InlineData("Age>=18", true)]
    [InlineData("Name == 'Alice'", true)]
    [InlineData("Name == \"Alice\"", true)]
    [InlineData("Name != 'Alice'", false)]
    [InlineData("Name == Alice", true)]
    [InlineData("City == New York", true)]
    [InlineData("City == 'New York' and Age > 1", true)]
    [InlineData("Name contains 'lic'", true)]
    [InlineData("Name CONTAINS 'zzz'", false)]
    [InlineData("Name startswith 'Al'", true)]
    [InlineData("Name endswith 'ce'", true)]
    [InlineData("Name endswith 'zz'", false)]
    [InlineData("Tag == null", true)]
    [InlineData("Tag != null", false)]
    [InlineData("MissingField == null", true)]
    [InlineData("MissingField != null", false)]
    [InlineData("Active", true)]
    [InlineData("!Active", false)]
    [InlineData("NOT Active", false)]
    [InlineData("not Active", false)]
    [InlineData("(Age >= 18)", true)]
    [InlineData("Age >= 18 && Name == 'Alice'", true)]
    [InlineData("Age >= 99 || Name == 'Alice'", true)]
    [InlineData("Age >= 99 AND Name == 'Alice'", false)]
    [InlineData("Age >= 99 and Name == 'Alice'", false)]
    [InlineData("Age >= 99 OR Name == 'Alice'", true)]
    [InlineData("Active == true", true)]
    [InlineData("Age >= 18 or Age < 5 and Name == 'Bob'", true)]
    [InlineData("(Age >= 18 or Age < 5) and Name == 'Bob'", false)]
    [InlineData("not (Age >= 18 and Name == 'Alice')", false)]
    [InlineData("Joined > 2026-03-01T11:00:00+00:00", true)]
    [InlineData("Joined > 2026-03-01T13:00:00+00:00", false)]
    [InlineData("Name == '{{Region}}'", false)]
    public void ARecordIsJudgedByTheConditionRead(string expression, bool expected)
    {
        // Act
        var parsed = RecordPredicates.Parse(expression, NullLogger.Instance);

        // Assert
        parsed.IsSuccess.ShouldBeTrue(parsed.CurrentMessage);
        parsed.Value!.Evaluate(Record()).ShouldBe(expected);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AFieldTheRecordLacksIsNotTrueAndNotAPass()
    {
        // Act
        var parsed = RecordPredicates.Parse("UnknownField", NullLogger.Instance);

        // Assert: it used to pass every record; a field the record lacks is simply not true
        parsed.IsSuccess.ShouldBeTrue();
        parsed.Value!.Evaluate(Record()).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AComparisonOnAFieldTheRecordLacksIsFalse()
    {
        // Act
        var greater = RecordPredicates.Parse("Missing > 1", NullLogger.Instance).Value!;
        var equal = RecordPredicates.Parse("Missing == 'x'", NullLogger.Instance).Value!;
        var notEqual = RecordPredicates.Parse("Missing != 'x'", NullLogger.Instance).Value!;

        // Assert: a missing value is never greater, less or equal; it is "not equal" to anything
        greater.Evaluate(Record()).ShouldBeFalse();
        equal.Evaluate(Record()).ShouldBeFalse();
        notEqual.Evaluate(Record()).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void AnEmptyQuotedTextEqualsANullValue()
    {
        // Act
        var parsed = RecordPredicates.Parse("Tag == ''", NullLogger.Instance);

        // Assert
        parsed.IsSuccess.ShouldBeTrue(parsed.CurrentMessage);
        parsed.Value!.Evaluate(Record()).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ANumberFieldIsTrueWhenItHoldsAnythingEvenZero()
    {
        // Arrange
        var record = new Dictionary<string, object?> { ["Count"] = 0 };

        // Act
        var parsed = RecordPredicates.Parse("Count", NullLogger.Instance);

        // Assert: a bare field asks whether the field holds a value, as the Filter stage always did
        parsed.Value!.Evaluate(record).ShouldBeTrue();
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Age >")]
    [InlineData("Age >=")]
    [InlineData("Age =")]
    [InlineData("Age = 5")]
    [InlineData("Age 5")]
    [InlineData("garbage expression")]
    [InlineData("(Age > 1")]
    [InlineData("Age > 1)")]
    [InlineData("()")]
    [InlineData("Age > 1 &&")]
    [InlineData("&& Age > 1")]
    [InlineData("Age > 1 or")]
    [InlineData("not")]
    [InlineData("Name == 'abc")]
    [InlineData("Name == 'a' x")]
    [InlineData("Name contains")]
    [InlineData("Name contains 'a' 'b'")]
    public void AConditionThatCannotBeReadIsRefusedNamingTheText(string expression)
    {
        // Act
        var parsed = RecordPredicates.Parse(expression, NullLogger.Instance);

        // Assert: never a condition that every record meets
        parsed.IsSuccess.ShouldBeFalse();
        parsed.Code!.Id.ShouldBe(21000);
        parsed.Details!.GetValue<string>("Expression").ShouldBe(expression);
        parsed.Details!.GetValue<string>("Reason").ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void TheRefusalSaysWhereTheTextWentWrong()
    {
        // Act
        var parsed = RecordPredicates.Parse("Age >= 18 && Name", NullLogger.Instance);
        var wrong = RecordPredicates.Parse("Age 5", NullLogger.Instance);

        // Assert: "Name" alone is a field; "Age 5" has a stray value, and the reason names it and the position
        parsed.IsSuccess.ShouldBeTrue();
        wrong.IsSuccess.ShouldBeFalse();
        wrong.Details!.GetValue<string>("Reason").ShouldNotBeNull().ShouldContain("5");
        wrong.Details!.GetValue<string>("Reason").ShouldNotBeNull().ShouldContain("position");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void AConditionReadOnceIsAskedOfEveryRecord()
    {
        // Arrange
        var parsed = RecordPredicates.Parse("Age >= 18", NullLogger.Instance).Value!;
        var young = new Dictionary<string, object?> { ["Age"] = 10 };
        var old = new Dictionary<string, object?> { ["Age"] = 40 };

        // Act and assert
        parsed.Evaluate(young).ShouldBeFalse();
        parsed.Evaluate(old).ShouldBeTrue();
    }
}
