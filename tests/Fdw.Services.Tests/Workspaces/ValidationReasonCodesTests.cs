using Fdw.Results;
using Fdw.Services.Workspaces.Results;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Workspaces;

/// <summary>
/// FDW-1046: the workspace refusals written for the caller of a request show their reason in the response. A validation code
/// that has not opted in shows nothing, so its message (written for the log) never reaches a caller.
/// </summary>
[Collection(nameof(ServicesTestCollection))]
public class ValidationReasonCodesTests
{
    [Fact]
    public void AVariationRefusalShowsWhichVariationAndWhy()
        => WorkspacesResultCodes.ByName("DataSetVariationInvalid")
            .FormatClientMessage(ResultDetails.Create("name", "Fall", "reason", "its parent view no longer exists"))
            .ShouldBe("Variation 'Fall' rejected: its parent view no longer exists");

    [Fact]
    public void ADataViewRefusalShowsWhichViewAndWhy()
        => WorkspacesResultCodes.ByName("DataViewSpecInvalid")
            .FormatClientMessage(ResultDetails.Create("name", "Weekly", "reason", "its chart type is not known"))
            .ShouldBe("Data view 'Weekly' rejected: its chart type is not known");

    [Fact]
    public void AMapRequestRefusalShowsWhy()
        => WorkspacesResultCodes.ByName("MapRequestInvalid")
            .FormatClientMessage(ResultDetails.Create("reason", "organization and workspace cannot both be given"))
            .ShouldBe("Map request rejected: organization and workspace cannot both be given");

    [Fact]
    public void ALifecycleValueRefusalShowsTheFieldAndTheValue()
        => WorkspacesResultCodes.ByName("WorkspaceLifecycleValueInvalid")
            .FormatClientMessage(ResultDetails.Create("name", "Quakes", "field", "relationship", "value", "Sells"))
            .ShouldBe("Workspace 'Quakes' rejected: relationship 'Sells' is not a registered option");

    [Fact]
    public void AnUnknownResourceKindRefusalNamesTheKindsTheHostRegisters()
        => WorkspacesResultCodes.ByName("WorkspaceResourceKindUnknown")
            .FormatClientMessage(ResultDetails.Create("name", "Quakes", "value", "Spreadsheet", "kinds", "DataSet, DataView"))
            .ShouldBe("Workspace 'Quakes' rejected: resourceType 'Spreadsheet' is not a resource kind this host knows; the kinds are DataSet, DataView");

    [Fact]
    public void AResourceKindThatIsNotWiredIsRefusedAsNotWiredAndNotAsUnregistered()
        => WorkspacesResultCodes.ByName("WorkspaceResourceKindNotAttachable")
            .FormatClientMessage(ResultDetails.Create("name", "Quakes", "kind", "Pipeline"))
            .ShouldBe("Workspace 'Quakes' rejected: a Pipeline cannot be attached yet; its kind is registered but no label resolver is wired for it");

    [Fact]
    public void AnUnknownMemberSubjectRefusalNamesTheSubject()
        => WorkspacesResultCodes.ByName("WorkspaceMemberSubjectUnknown")
            .FormatClientMessage(ResultDetails.Create("name", "Quakes", "subjectType", "Role", "subjectId", "0197a3c4-5b6d-7e8f-9a0b-2d3e4f5a6b7c"))
            .ShouldBe("Workspace 'Quakes' member refused: Role '0197a3c4-5b6d-7e8f-9a0b-2d3e4f5a6b7c' does not resolve");

    [Fact]
    public void AnOwnerMembershipRefusalSaysWhatWasRefused()
        => WorkspacesResultCodes.ByName("WorkspaceOwnerMembershipProtected")
            .FormatClientMessage(ResultDetails.Create("name", "Quakes", "action", "removed"))
            .ShouldBe("Workspace 'Quakes' refused: the owner's membership cannot be removed");

    [Fact]
    public void AnAutoApproveRoleRefusalSaysWhy()
        => WorkspacesResultCodes.ByName("WorkspaceAutoApproveRoleInvalid")
            .FormatClientMessage(ResultDetails.Create("name", "Quakes", "value", "Owner", "reason", "manages members, and is never granted to whoever asks"))
            .ShouldBe("Workspace 'Quakes' rejected: autoApproveRole 'Owner' manages members, and is never granted to whoever asks");

    [Fact]
    public void AValidationCodeThatDidNotOptInShowsNothing()
        => WorkspacesResultCodes.ByName("WorkspaceNoteInvalid")
            .FormatClientMessage(ResultDetails.Create("value", "x"))
            .ShouldBeNull();
}
