using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Conventions.Commands;
using Fdw.Roslyn.Commands.Conventions.Results;
using Fdw.Roslyn.Commands.Conventions.Translators;

namespace Fdw.Roslyn.Commands.Tests;

/// <summary>
/// Tests for <see cref="GuideImplementationTranslator"/>.
/// </summary>
public sealed class GuideImplementationTranslatorTests
{
    private static Task<Fdw.Results.IGenericResult<QueryResult<ImplementationGuideData>>> Run(
        GuideImplementationCommand command,
        string[]? references = null) =>
        new GuideImplementationTranslator().Translate(
            command,
            GuideTestWorld.Build(references ?? Array.Empty<string>()),
            TestContext.Current.CancellationToken);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task ATypeThatImplementsTheAbstractionGetsItsGuide()
    {
        var folder = GuideTestWorld.GuidesFolder();

        var result = await Run(new GuideImplementationCommand { GuidesPath = folder, TypeName = "Gadget" });

        result.IsSuccess.ShouldBeTrue();
        var guide = result.Value!.Data.Guides.ShouldHaveSingleItem();
        guide.Id.ShouldBe("widget");
        guide.AppliesVia.ShouldContain("Acme.Contracts.IWidget");
        guide.Provenance.Commit.ShouldBe("abc123");
        guide.Must.ShouldHaveSingleItem().Head.ShouldBe("Implements Run");
        guide.MustNot.ShouldHaveSingleItem().Head.ShouldBe("No registration reference");
        guide.References.Count.ShouldBe(2);
        guide.Documentation.ShouldNotBeNull().ShouldContain("A widget.");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task AMemberTheTypeLeavesOutIsReportedAsUnimplemented()
    {
        var folder = GuideTestWorld.GuidesFolder();

        var result = await Run(new GuideImplementationCommand { GuidesPath = folder, TypeName = "Gadget" });

        var guide = result.Value!.Data.Guides.Single();
        guide.UnimplementedMembers.ShouldHaveSingleItem().ShouldContain("Run");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task ACompleteTypeHasNothingUnimplemented()
    {
        var folder = GuideTestWorld.GuidesFolder();

        var result = await Run(new GuideImplementationCommand { GuidesPath = folder, TypeName = "Complete" });

        result.Value!.Data.Guides.Single().UnimplementedMembers.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task AProjectIsMatchedByTheAbstractionsItReferences()
    {
        var folder = GuideTestWorld.GuidesFolder();

        var result = await Run(new GuideImplementationCommand { GuidesPath = folder, ProjectName = "Acme.Impl" });

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Data.Guides.ShouldHaveSingleItem().UnimplementedMembers.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task TheExemplarIsLocatedThroughItsCitedFile()
    {
        var folder = GuideTestWorld.GuidesFolder();

        var result = await Run(new GuideImplementationCommand { GuidesPath = folder, TypeName = "Gadget" });

        var exemplar = result.Value!.Data.Guides.Single().Exemplars.ShouldHaveSingleItem();
        exemplar.Resolution.ShouldBe(GuideExemplarResolution.Resolved);
        exemplar.Line.ShouldNotBeNull();
        exemplar.Summary.ShouldBe("The widget to copy.");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task AnExemplarOutsideTheSolutionIsReportedAsNotInTheSolution()
    {
        var folder = GuideTestWorld.GuidesFolder(guide =>
            guide["exemplars"] = new[] { new { type = "Acme.Elsewhere.Thing", role = "far away", cite = "other/src/Thing.cs:3" } });

        var result = await Run(new GuideImplementationCommand { GuidesPath = folder, TypeName = "Gadget" });

        var exemplar = result.Value!.Data.Guides.Single().Exemplars.Single();
        exemplar.Resolution.ShouldBe(GuideExemplarResolution.FileNotInSolution);
        exemplar.FilePath.ShouldBeNull();
        exemplar.StampedCite.ShouldBe("other/src/Thing.cs:3");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task AProjectWithNoMatchingAbstractionFailsLoudly()
    {
        var folder = GuideTestWorld.GuidesFolder();

        var result = await Run(new GuideImplementationCommand { GuidesPath = folder, ProjectName = "Acme.Unrelated" });

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull().Name.ShouldBe("NoImplementationGuide");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task AMissingGuidesPathFailsLoudly()
    {
        var result = await Run(new GuideImplementationCommand { TypeName = "Gadget" });

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull().Name.ShouldBe("GuidesPathRequired");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task NeitherTypeNorProjectFailsLoudly()
    {
        var result = await Run(new GuideImplementationCommand { GuidesPath = GuideTestWorld.GuidesFolder() });

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull().Name.ShouldBe("EitherTypeNameOrProjectNameRequired");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task AFolderWithNoGuideDocumentFailsLoudly()
    {
        var empty = Path.Combine(Path.GetTempPath(), "fdw-guides-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(empty);

        var result = await Run(new GuideImplementationCommand { GuidesPath = empty, TypeName = "Gadget" });

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull().Name.ShouldBe("GuideDocumentNotFound");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task AGuideDocumentMissingARequiredKeyFailsLoudly()
    {
        var folder = GuideTestWorld.GuidesFolder();
        File.WriteAllText(
            Path.Combine(folder, "implementing.json"),
            "[{\"id\":\"widget\",\"label\":\"W\",\"title\":\"T\",\"guide\":{\"implements\":[]}}]");

        var result = await Run(new GuideImplementationCommand { GuidesPath = folder, TypeName = "Gadget" });

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull().Name.ShouldBe("GuideDocumentInvalid");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task TheRendererKeysOnAPageAndTheStampAreNotUnknownKeys()
    {
        var folder = GuideTestWorld.GuidesFolder();
        var pages = File.ReadAllText(Path.Combine(folder, "implementing.json"))
            .Replace("\"title\":", "\"kind\":\"k\",\"lede\":\"l\",\"blocks\":[],\"title\":", StringComparison.Ordinal);
        File.WriteAllText(Path.Combine(folder, "implementing.json"), pages);
        File.WriteAllText(
            Path.Combine(folder, "implementing.meta.json"),
            "{\"repo\":\"acme\",\"commit\":\"abc123\",\"verified\":\"2026-10-13\",\"ref\":\"internal\",\"also\":\"x\",\"note\":\"n\"}");

        var result = await Run(new GuideImplementationCommand { GuidesPath = folder, TypeName = "Gadget" });

        result.IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    [InlineData("guide", "[{\"id\":\"w\",\"label\":\"W\",\"title\":\"T\",\"guide\":{\"implements\":[],\"exemplars\":[],\"references\":{\"require\":[],\"forbid\":[]},\"must\":[],\"mustNot\":[],\"see\":[],\"typo\":1}}]")]
    [InlineData("item", "[{\"id\":\"w\",\"label\":\"W\",\"title\":\"T\",\"guide\":{\"implements\":[{\"type\":\"A.B\",\"cite\":\"x:1\",\"typo\":1}],\"exemplars\":[],\"references\":{\"require\":[],\"forbid\":[]},\"must\":[],\"mustNot\":[],\"see\":[]}}]")]
    public async Task AnUnknownKeyInsideTheGuideFailsLoudly(string level, string document)
    {
        var folder = GuideTestWorld.GuidesFolder();
        File.WriteAllText(Path.Combine(folder, "implementing.json"), document);

        var result = await Run(new GuideImplementationCommand { GuidesPath = folder, TypeName = "Gadget" });

        result.IsSuccess.ShouldBeFalse($"an unknown key at the {level} level of the guide must not be ignored");
        result.Code.ShouldNotBeNull().Name.ShouldBe("GuideDocumentInvalid");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task AnUnknownTypeFailsLoudly()
    {
        var result = await Run(new GuideImplementationCommand
        {
            GuidesPath = GuideTestWorld.GuidesFolder(),
            TypeName = "NoSuchType"
        });

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull().Name.ShouldBe("TypeNotFound");
    }
}
