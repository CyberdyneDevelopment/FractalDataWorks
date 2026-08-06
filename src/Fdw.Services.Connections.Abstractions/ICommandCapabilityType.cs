using System;
using System.Collections.Generic;
using Fdw.Collections;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// Describes a command capability that a connection type can execute.
/// Each capability declares the configuration fields the pipeline builder should render
/// when this capability is selected.
/// </summary>
/// <remarks>
/// Why TypeOption not IServiceType: command capabilities are not services — they carry no
/// factory, no DI registration, and no lifecycle. They are open-ended metadata descriptors
/// that connection types declare to tell the builder what inputs they accept.
/// External assemblies add capabilities via module initializers without framework changes.
/// </remarks>
public interface ICommandCapabilityType : ITypeOption<int, CommandCapabilityTypeBase>
{
    /// <summary>
    /// Gets the field descriptors that drive the properties-panel input form in the pipeline
    /// builder when this capability is selected.
    /// An empty list means the capability has no configuration fields at the builder level
    /// (e.g., <c>QueryCapability</c> defers to the structured <see cref="BuilderComponentType"/>).
    /// </summary>
    IReadOnlyList<ConfigurationFieldDescriptor> ConfigurationFields { get; }

    /// <summary>
    /// Gets the optional <see cref="Type"/> of a Blazor component that renders a richer composite
    /// widget for this capability. When non-null the builder renders that component instead of
    /// iterating over <see cref="ConfigurationFields"/>.
    /// Must implement <c>ICommandCapabilityBuilder</c> and accept a <c>TaskConfiguration</c>
    /// cascading parameter that carries the current task's Configuration dictionary.
    /// </summary>
    /// <remarks>
    /// Why optional: most capabilities (RawQuery, Execute, BulkInsert, …) are simple field lists.
    /// QueryCapability needs a structured container+field+filter+sort+paging composite — setting
    /// <c>BuilderComponentType = typeof(QueryCommandBuilder)</c> keeps that complexity in a
    /// dedicated component without forcing every capability to carry a component reference.
    /// </remarks>
    Type? BuilderComponentType { get; }

}
