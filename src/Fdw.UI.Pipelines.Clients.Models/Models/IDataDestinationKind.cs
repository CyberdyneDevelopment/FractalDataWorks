using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Pipelines.Clients.Models;

/// <summary>
/// Interface for data destination kinds.
/// </summary>
public interface IDataDestinationKind : ITypeOption<int, DataDestinationKindBase> { }
