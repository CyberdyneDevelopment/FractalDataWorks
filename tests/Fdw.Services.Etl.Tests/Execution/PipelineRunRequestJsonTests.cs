using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.Abstractions;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Execution;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Execution;

/// <summary>
/// Tests for the text a pipeline run's request is kept as (FDW-3216): every type a run parameter can be declared as comes back as the same type with the
/// same value, a list comes back as a list, and a value of any other type is refused naming the parameter.
/// </summary>
public sealed class PipelineRunRequestJsonTests
{
    // One value for the CLR type of each data type; the test below fails when a data type is added without one.
    private static readonly Dictionary<Type, object> Samples = new()
    {
        [typeof(string)] = "héllo \"quoted\"",
        [typeof(int)] = 42,
        [typeof(long)] = 4_000_000_000L,
        [typeof(short)] = (short)7,
        [typeof(byte)] = (byte)3,
        [typeof(decimal)] = 3.125m,
        [typeof(double)] = 2.25d,
        [typeof(float)] = 1.5f,
        [typeof(bool)] = true,
        [typeof(DateTime)] = new DateTime(2026, 10, 29, 1, 2, 3, 456, DateTimeKind.Utc),
        [typeof(DateTimeOffset)] = new DateTimeOffset(2026, 10, 29, 1, 2, 3, TimeSpan.FromHours(-5)),
        [typeof(Guid)] = Guid.Parse("11111111-2222-3333-4444-555555555555"),
        [typeof(TimeSpan)] = TimeSpan.FromMinutes(90),
        [typeof(byte[])] = new byte[] { 1, 2, 3 },
        [typeof(float[])] = new[] { 0.5f, 1.5f },
    };

    private static PipelineExecutionRequest Request(IReadOnlyDictionary<string, object?>? values, DateTimeOffset? heldUntil = null) => new()
    {
        ExecutionId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001"),
        PipelineId = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002"),
        PipelineName = "monthly-sales",
        TriggerSource = "Schedule",
        ScheduleName = "Nightly",
        TenantId = Guid.Parse("cccccccc-0000-0000-0000-000000000003"),
        RunRoles = ["DataWriter", "DataReader"],
        Parameters = values,
        TestRowsPerSource = 25,
        HeldUntil = heldUntil
    };

    private static PipelineExecutionRequest RoundTrip(PipelineExecutionRequest request, DateTimeOffset? heldUntil = null)
    {
        var text = PipelineRunRequestJson.Write(request, NullLogger.Instance);
        text.IsSuccess.ShouldBeTrue(text.CurrentMessage);
        var read = PipelineRunRequestJson.Read(request.ExecutionId, text.Value!, heldUntil, NullLogger.Instance);
        read.IsSuccess.ShouldBeTrue(read.CurrentMessage);
        return read.Value!;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void EveryTypeADataTypeHoldsHasASampleHere()
    {
        var missing = DataTypes.All().Select(type => type.ClrType).Distinct().Where(type => !Samples.ContainsKey(type)).Select(type => type.FullName).ToList();

        missing.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void EveryTypeAParameterCanHaveComesBackAsTheSameTypeWithTheSameValueSingleOrAsAList()
    {
        foreach (var type in DataTypes.All().Select(type => type.ClrType).Distinct())
        {
            var sample = Samples[type];
            var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["One"] = sample,
                ["Many"] = new List<object> { sample, sample },
            };

            var back = RoundTrip(Request(values)).Parameters.ShouldNotBeNull();

            back["One"].ShouldNotBeNull().GetType().ShouldBe(type);
            back["One"].ShouldBeEquivalentTo(sample);
            var many = back["Many"].ShouldBeAssignableTo<IReadOnlyList<object>>().ShouldNotBeNull();
            many.Count.ShouldBe(2);
            many.ShouldAllBe(item => item.GetType() == type);
            many[1].ShouldBeEquivalentTo(sample);
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TheRestOfTheRequestAndTheTimeARunIsHeldUntilComeBack()
    {
        var due = new DateTimeOffset(2026, 10, 30, 2, 0, 0, TimeSpan.Zero);
        var original = Request(new Dictionary<string, object?> { ["Size"] = 5, ["Note"] = null });

        var back = RoundTrip(original, due);

        back.ExecutionId.ShouldBe(original.ExecutionId);
        back.PipelineId.ShouldBe(original.PipelineId);
        back.PipelineName.ShouldBe("monthly-sales");
        back.TriggerSource.ShouldBe("Schedule");
        back.ScheduleName.ShouldBe("Nightly");
        back.TenantId.ShouldBe(original.TenantId);
        back.RunRoles.ShouldBe(["DataWriter", "DataReader"]);
        back.TestRowsPerSource.ShouldBe(25);
        back.HeldUntil.ShouldBe(due);
        back.Parameters.ShouldNotBeNull()["Size"].ShouldBe(5);
        back.Parameters!.ContainsKey("Note").ShouldBeTrue();
        back.Parameters["Note"].ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ARequestWithNoParametersComesBackWithNoParametersAndNoHold()
    {
        var back = RoundTrip(Request(null));

        back.Parameters.ShouldBeNull();
        back.HeldUntil.ShouldBeNull();
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("object")]
    [InlineData("mixed list")]
    [InlineData("empty list")]
    public void AValueThatCannotBeKeptIsRefusedNamingTheParameterAndIsNeverTurnedIntoText(string which)
    {
        object value = which switch
        {
            "object" => new object(),
            "mixed list" => new List<object> { 1, "two" },
            _ => new List<object>(),
        };

        var written = PipelineRunRequestJson.Write(Request(new Dictionary<string, object?> { ["Odd"] = value }), NullLogger.Instance);

        written.IsFailure.ShouldBeTrue();
        written.CurrentMessage.ShouldNotBeNull().ShouldContain("Odd");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("not json")]
    [InlineData("null")]
    [InlineData("{\"ExecutionId\":\"aaaaaaaa-0000-0000-0000-000000000001\",\"PipelineId\":\"bbbbbbbb-0000-0000-0000-000000000002\",\"PipelineName\":\"p\",\"TriggerSource\":\"t\",\"RunRoles\":[],\"Values\":[{\"Name\":\"X\",\"Type\":\"System.Nope\",\"IsList\":false,\"Value\":1}]}")]
    public void ARecordThatCannotBeReadIsRefusedAndNeverReadAsAnEmptyRequest(string text)
    {
        var read = PipelineRunRequestJson.Read(Guid.NewGuid(), text, null, NullLogger.Instance);

        read.IsFailure.ShouldBeTrue();
    }
}
