using Fdw.Services.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Tags.Configuration;

namespace Fdw.Services.Tags;

/// <summary>Supplies the configured tags: a member whole, or a page of them as domain records.</summary>
public interface ITagConfigurationProvider
    : IDomainConfigurationProvider<ITagImplementationConfiguration>,
      IDomainRecordProvider<TagDomainConfiguration>
{
}
