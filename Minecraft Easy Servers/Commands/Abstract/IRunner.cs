namespace Minecraft_Easy_Servers.Commands.Abstract
{
    public interface IRunner<in T> where T : BaseOptions
    {
        void Run(T options);
    }
}