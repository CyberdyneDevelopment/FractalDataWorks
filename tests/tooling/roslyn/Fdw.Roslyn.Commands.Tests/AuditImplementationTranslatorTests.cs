using System;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Conventions.Commands;
using Fdw.Roslyn.Commands.Conventions.Results;
using Fdw.Roslyn.Commands.Conventions.Translators;

namespace Fdw.Roslyn.Commands.Tests;

/// <summary>
/// Tests for <see cref="AuditImplementationTranslator"/>.
/// </summary>
public sealed class AuditImplementationTranslatorTests
{
    private static Task<Fdw.Results.IGenericResult<QueryResult<AuditImplementationData>>> Run(
        AuditImplementationCommand command,
        params string[] references) =>
        new AuditImplementationTranslator().Translate(
            command,
            GuideTestWorld.Build(references),
            TestContext.Current.CancellationToken);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task AForbiddenReferenceIsReportedWithTheRuleCitation()
    {
        var folder = GuideTestWorld.GuidesFolder();

        var result = await Run(new AuditImplementationCommand { GuidesPath = folder, ProjectName = "Acme.Impl" }, "legacy");

        result.IsSuccess.ShouldBeTrue();
        var finding = result.Value!.Data.Findings.ShouldHaveSingleItem();
        finding.Kind.ShouldBe(AuditFindingKind.ForbiddenReference);
        finding.Subject.ShouldBe("Acme.Legacy.Registration");
        finding.Cite.ShouldBe("acme/Impl.csproj:9");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task ACleanProjectHasNoFindingsAndListsTheRulesItCannotCheck()
    {
        var folder = GuideTestWorld.GuidesFolder();

        var result = await Run(new AuditImplementationCommand { GuidesPath = folder, ProjectName = "Acme.Impl" });

        var data = result.Value!.Data;
        data.Findings.ShouldBeEmpty();
        data.GuideCount.ShouldBe(1);
        data.ManualChecks.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task ARuleScopedToOtherProjectsDoesNotApply()
    {
        var folder = GuideTestWorld.GuidesFolder(guide =>
            ((System.Collections.Generic.Dictionary<string, object?>)guide["references"]!)["forbid"] =
                new[] { new { assembly = "*.Registration", projects = "Other.*", why = "scoped elsewhere", cite = "acme/Impl.csproj:9" } });

        var result = await Run(new AuditImplementationCommand { GuidesPath = folder, ProjectName = "Acme.Impl" }, "legacy");

        result.Value!.Data.Findings.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task AMissingRequiredReferenceIsReported()
    {
        var folder = GuideTestWorld.GuidesFolder(guide =>
            ((System.Collections.Generic.Dictionary<string, object?>)guide["references"]!)["require"] =
                new[] { new { assembly = "Acme.Absent", projects = "Acme.Impl", why = "needed", cite = "acme/Impl.csproj:3" } });

        var result = await Run(new AuditImplementationCommand { GuidesPath = folder, ProjectName = "Acme.Impl" });

        var finding = result.Value!.Data.Findings.ShouldHaveSingleItem();
        finding.Kind.ShouldBe(AuditFindingKind.MissingReference);
        finding.Subject.ShouldBe("Acme.Absent");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task AnUnimplementedMemberIsReportedWhenATypeIsNamed()
    {
        var folder = GuideTestWorld.GuidesFolder();

        var result = await Run(new AuditImplementationCommand
        {
            GuidesPath = folder,
            ProjectName = "Acme.Impl",
            TypeName = "Gadget"
        });

        var finding = result.Value!.Data.Findings.ShouldHaveSingleItem();
        finding.Kind.ShouldBe(AuditFindingKind.UnimplementedMember);
        finding.Cite.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task AProjectNameIsRequired()
    {
        var result = await Run(new AuditImplementationCommand { GuidesPath = GuideTestWorld.GuidesFolder() });

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull().Name.ShouldBe("ProjectNameRequired");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task AProjectNoGuideAppliesToFailsLoudly()
    {
        var result = await Run(new AuditImplementationCommand
        {
            GuidesPath = GuideTestWorld.GuidesFolder(),
            ProjectName = "Acme.Unrelated"
        });

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull().Name.ShouldBe("NoImplementationGuide");
    }
}
