using System.Threading.Channels;

namespace GrpcSocks.Extension
{
    public static class Logger
    {
        public static Channel<string> MainChannel { get; set; } = Channel.CreateUnbounded<string>();
        public static Task MainTask { get; set; }
        static Logger()
        {
            MainTask = Task.Run(async() =>
            {
                while(true)
                {
                    Console.WriteLine(await MainChannel.Reader.ReadAsync());
                }
            });
        }
        public static async void WriteLine(string str)
        {
            await MainChannel.Writer.WriteAsync(str);
        }
    }
}
