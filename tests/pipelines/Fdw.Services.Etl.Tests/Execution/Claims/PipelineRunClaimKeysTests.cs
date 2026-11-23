using System;
using System.Linq;
using Fdw.Services.Etl.Execution.Claims;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Execution.Claims;

/// <summary>
/// FDW-6629: the keys a run claims. A stated key always begins with the tenant's id, so no template can leave the tenant out and two tenants never meet on the same text; a write key is the
/// address of what the run writes; a cap key names the pipeline. A key built from a missing part is a coded failure and is never completed with a guess.
/// </summary>
public sealed class PipelineRunClaimKeysTests
{
    private static readonly Guid Tenant = Guid.Parse("22222222-0000-0000-0000-000000000002");
    private static readonly Guid OtherTenant = Guid.Parse("22222222-0000-0000-0000-000000000003");
    private static readonly Guid Pipeline = Guid.Parse("44444444-0000-0000-0000-000000000004");

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AStatedKeyBeginsWithTheTenantsIdWhateverTheTemplateSays()
    {
        var key = PipelineRunClaimKeys.Stated(Tenant, "order-17", NullLogger.Instance);

        key.IsSuccess.ShouldBeTrue(key.CurrentMessage);
        key.Value.ShouldNotBeNull().Type.Name.ShouldBe("StatedKey");
        key.Value.ClaimKey.ShouldBe($"{Tenant:D}:order-17");
        key.Value.Cap.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TwoTenantsWithTheSameTextHaveDifferentKeysAndOneTenantsSameTextIsTheSameKey()
    {
        var first = PipelineRunClaimKeys.Stated(Tenant, "order-17", NullLogger.Instance).Value.ShouldNotBeNull();
        var again = PipelineRunClaimKeys.Stated(Tenant, "order-17", NullLogger.Instance).Value.ShouldNotBeNull();
        var other = PipelineRunClaimKeys.Stated(OtherTenant, "order-17", NullLogger.Instance).Value.ShouldNotBeNull();

        again.ClaimKey.ShouldBe(first.ClaimKey);
        other.ClaimKey.ShouldNotBe(first.ClaimKey);
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("")]
    [InlineData("   ")]
    public void AStatedKeyWithNoTextIsRefusedWithItsCode(string text)
    {
        var key = PipelineRunClaimKeys.Stated(Tenant, text, NullLogger.Instance);

        key.IsFailure.ShouldBeTrue();
        key.Messages.Select(message => message.Code).ShouldContain("PIPELINERUNCLAIM-60861");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AStatedKeyForNoTenantIsRefusedWithItsCode()
    {
        var key = PipelineRunClaimKeys.Stated(Guid.Empty, "order-17", NullLogger.Instance);

        key.IsFailure.ShouldBeTrue();
        key.Messages.Select(message => message.Code).ShouldContain("PIPELINERUNCLAIM-60862");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AWriteKeyIsTheAddressOfWhatTheRunWrites()
    {
        var key = PipelineRunClaimKeys.Write("NflDb", "dbo", "Teams", NullLogger.Instance);

        key.IsSuccess.ShouldBeTrue(key.CurrentMessage);
        key.Value.ShouldNotBeNull().Type.Name.ShouldBe("WriteKey");
        key.Value.ClaimKey.ShouldBe("NflDb/dbo/Teams");
        key.Value.Cap.ShouldBeNull();
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("", "dbo", "Teams")]
    [InlineData("NflDb", " ", "Teams")]
    [InlineData("NflDb", "dbo", "")]
    public void AWriteKeyWithAPartMissingIsRefusedWithItsCode(string connection, string path, string container)
    {
        var key = PipelineRunClaimKeys.Write(connection, path, container, NullLogger.Instance);

        key.IsFailure.ShouldBeTrue();
        key.Messages.Select(message => message.Code).ShouldContain("PIPELINERUNCLAIM-60863");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ACapKeyNamesThePipelineAndStatesHowManyRunsMayBeClaimed()
    {
        var key = PipelineRunClaimKeys.PipelineCap(Pipeline, 3, NullLogger.Instance);

        key.IsSuccess.ShouldBeTrue(key.CurrentMessage);
        key.Value.ShouldNotBeNull().Type.Name.ShouldBe("PipelineCap");
        key.Value.ClaimKey.ShouldBe(Pipeline.ToString("D"));
        key.Value.Cap.ShouldBe(3);
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData(0)]
    [InlineData(-1)]
    public void ACapBelowOneIsRefusedWithItsCode(int cap)
    {
        var key = PipelineRunClaimKeys.PipelineCap(Pipeline, cap, NullLogger.Instance);

        key.IsFailure.ShouldBeTrue();
        key.Messages.Select(message => message.Code).ShouldContain("PIPELINERUNCLAIM-60864");
    }
}
