using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Pipelines.Clients.Models;

/// <summary>
/// Interface for data write modes.
/// </summary>
public interface IWriteMode : ITypeOption<int, WriteModeBase> { }
