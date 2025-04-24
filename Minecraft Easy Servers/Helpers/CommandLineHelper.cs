using CommandLine;
using Minecraft_Easy_Servers.Commands.Abstract;
using System.Reflection;

namespace Minecraft_Easy_Servers.Helpers
{
    public static class CommandLineHelper
    {
        public static Type[] LoadVerbs()
        {
            return Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t.GetCustomAttribute<VerbAttribute>() != null).ToArray();
        }
        public static IEnumerable<Type> GetRunnerTypes(Type type)
        {
            return type.GetInterfaces()
                       .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRunner<>))
                       .Select(i => i.GetGenericArguments()[0]);
        }
    }
}
