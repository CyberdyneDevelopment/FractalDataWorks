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
    /// An empty list means the capability has no configuration fields at the builder level.
    /// </summary>
    IReadOnlyList<ConfigurationFieldDescriptor> ConfigurationFields { get; }

}
