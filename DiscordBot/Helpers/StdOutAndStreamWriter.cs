
namespace Minecraft_Easy_Servers.Helpers
{
    public class StdOutAndStreamWriter : StreamWriter
    {
        private readonly TextWriter oldConsoleOut;
        private readonly StreamWriter stream;

        public StdOutAndStreamWriter(StreamWriter stream) : base(Console.OpenStandardOutput())
        {
            this.oldConsoleOut = Console.Out;
            this.stream = stream;
            stream.AutoFlush = true;
            AutoFlush = true;
        }

        public override ValueTask DisposeAsync()
        {
            return base.DisposeAsync();
        }

        public override void Write(char value)
        {
            base.Write(value);
            stream.Write(value);
        }
        public override void Write(string? value)
        {
            base.Write(value);
            stream.Write(value);
        }
        public override void WriteLine(string? value)
        {
            base.WriteLine(value);
            stream.WriteLine(value);
        }
        public override void WriteLine()
        {
            base.WriteLine();
            stream.WriteLine();
        }

        protected override void Dispose(bool disposing)
        {
            Console.SetOut(oldConsoleOut);
            base.Dispose(disposing);
        }
    }
}
