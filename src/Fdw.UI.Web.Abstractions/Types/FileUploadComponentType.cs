using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Web.Abstractions;

/// <summary>
/// File upload component type.
/// </summary>
// Why: pure TypeOption leaf — literal constructor values only, no logic to test.
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ComponentTypes), "FileUpload", RestrictToCurrentCompilation = true)]
public sealed class FileUploadComponentType : ComponentTypeBase
{
    /// <summary>
    /// Gets the singleton instance of the file upload component type.
    /// </summary>
    public FileUploadComponentType() : base(16, "FileUpload", "File Upload", "Input", "Upload files") { }
}