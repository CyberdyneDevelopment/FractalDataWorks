using Fdw.Collections;

namespace Fdw.Services.Tags;

/// <summary>Reads and writes the link rows of one taggable kind; its name is the kind's name.</summary>
public interface ITagLinkStore : ITypeOption<int, TagLinkStoreBase>
{
}
