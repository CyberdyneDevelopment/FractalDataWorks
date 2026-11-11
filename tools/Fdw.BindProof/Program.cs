using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Fdw.BindProof;

/// <summary>
/// Loads a type of an OLD published package through the runtime's default binding and creates one, which calls into its base type in the
/// dependency, and says which version of each Fdw.* assembly the runtime really bound.
/// <c>Fdw.BindProof --assembly Fdw.Results --type Fdw.Results.AuthCategory</c>. The dependency in the app folder is the one under test:
/// the proof swaps the rebuilt dll in after publishing. Exit 0: bound and ran. Exit 1: the runtime refused (the message says how).
/// </summary>
internal static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            string assemblyName = Value(args, "--assembly");
            string typeName = Value(args, "--type");
            Assembly assembly = Assembly.Load(new AssemblyName(assemblyName));
            Type? type = assembly.GetType(typeName, throwOnError: true);
            if (type is null)
            {
                throw new InvalidOperationException($"{typeName} was not found.");
            }

            object? instance = Activator.CreateInstance(type);
            if (instance is null)
            {
                throw new InvalidOperationException($"{typeName} could not be created.");
            }

            Console.WriteLine("created " + instance.GetType().FullName + " (base " + instance.GetType().BaseType?.FullName + ")");
            foreach (Assembly loaded in AppDomain.CurrentDomain.GetAssemblies().Where(a => a.GetName().Name?.StartsWith("Fdw.", StringComparison.Ordinal) == true).OrderBy(a => a.GetName().Name, StringComparer.Ordinal))
            {
                Console.WriteLine("bound " + loaded.GetName().Name + " " + loaded.GetName().Version + " " + loaded.Location);
            }

            Console.WriteLine("BIND OK");
            return 0;
        }
        catch (Exception exception) when (exception is FileLoadException or FileNotFoundException or TypeLoadException or BadImageFormatException or MissingMethodException or TargetInvocationException or ArgumentException or InvalidOperationException)
        {
            Console.Error.WriteLine("BIND FAILED: " + exception.GetType().Name + ": " + exception.Message);
            return 1;
        }
    }

    private static string Value(string[] args, string name)
    {
        int index = Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : throw new ArgumentException($"{name} <value> is required.", nameof(args));
    }
}
