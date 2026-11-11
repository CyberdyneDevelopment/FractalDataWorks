namespace Fdw.ApiSurface;

/// <summary>The type whose members are being written: its key in the surface, its generic parameters, and whether a subclass can see its protected members.</summary>
internal sealed record OwnerInfo(string Key, GenericNames Names, bool Inheritable);
