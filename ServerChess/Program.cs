using System.Net;
using System.Net.Sockets;

namespace ServerChess
{
    internal class Program
    {
        private static TcpListener listen = new TcpListener(IPAddress.Parse("192.168.138.113"), 2024);
        private static ICollection<TcpClient> clients = new List<TcpClient>();
        static async Task Main(string[] args)
        {
            Console.WriteLine("Это сервер))");


            listen.Start();
             while (true)
            {
                Socket socket = await listen.AcceptSocketAsync();
                TcpClient client = await listen.AcceptTcpClientAsync();

                lock (clients)
                {
                    clients.Add(client);
                }

                await Console.Out.WriteLineAsync($"{client.Client.RemoteEndPoint} joined");

                if (clients.Count == 2)
                {
                    await Console.Out.WriteLineAsync("Достаточное количество игроков для игры");
                    //тут начинаем игру
                }
            }

             listen.Stop();
        }
    }
}
