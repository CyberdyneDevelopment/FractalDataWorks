using System.Reflection;

namespace Fdw.ApiSurface;

/// <summary>The words that tell an implementer or caller how a method behaves: static, abstract, virtual, override, sealed.</summary>
internal static class MemberModifiers
{
    public static string Of(MethodAttributes attributes)
    {
        string isStatic = attributes.HasFlag(MethodAttributes.Static) ? "static" : string.Empty;
        return Words.Join(isStatic, Dispatch(attributes));
    }

    private static string Dispatch(MethodAttributes attributes)
    {
        if (attributes.HasFlag(MethodAttributes.Abstract))
        {
            return "abstract";
        }

        if (!attributes.HasFlag(MethodAttributes.Virtual))
        {
            return string.Empty;
        }

        bool final = attributes.HasFlag(MethodAttributes.Final);
        bool newSlot = attributes.HasFlag(MethodAttributes.NewSlot);
        if (final)
        {
            // virtual + final + new slot is how the compiler marks a method that only implements an interface; it is not API.
            return newSlot ? string.Empty : "sealed override";
        }

        return newSlot ? "virtual" : "override";
    }
}
