namespace Minecraft_Easy_Servers
{
    public class MinecraftCommandLineRunner : ICommandLineRunner
    {
        public MinecraftCommandLineRunner()
        {
        }

        public void Run(Options o)
        {
            if (o.Verbose)
            {
                Console.WriteLine($"Verbose output enabled. Current Arguments: -v {o.Verbose}");
                Console.WriteLine("Quick Start Example! App is in Verbose mode!");
            }
            else
            {
                Console.WriteLine($"Current Arguments: -v {o.Verbose}");
                Console.WriteLine("Quick Start Example!");
            }
        }
    }
}
