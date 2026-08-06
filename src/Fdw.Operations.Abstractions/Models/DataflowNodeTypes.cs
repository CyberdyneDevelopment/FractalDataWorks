using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Clients.Models;

/// <summary>TypeCollection for dataflow node types.</summary>
[TypeCollection(typeof(DataflowNodeTypeBase), typeof(IDataflowNodeType), typeof(DataflowNodeTypes))]
[ExcludeFromCodeCoverage]
public abstract partial class DataflowNodeTypes : TypeCollectionBase<DataflowNodeTypeBase, IDataflowNodeType> { }
