using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Connections;

/// <summary>States that <see cref="DataContainerPagingSettingConfiguration"/> has no validator of its own, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "DataContainerPagingSettingConfiguration")]
public sealed class DataContainerPagingSettingConfigurationNoValidation : UnvalidatedConfiguration<DataContainerPagingSettingConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="DataContainerPagingSettingConfigurationNoValidation"/> class.</summary>
    public DataContainerPagingSettingConfigurationNoValidation()
        : base("DataContainerPagingSettingConfiguration", "A setting's name and value are valid only for the paging style of the container that states it, which a row cannot know; the store type's paging rules check every setting of the container against its effective style when the container is saved (FDW-5562).")
    {
    }
}
