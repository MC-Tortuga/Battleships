using System.Net;
using System.Net.Sockets;
namespace BattleshipServer;
class Program
{
    private static Matchmaker _matchmaker = new();
    private static TcpListener? _server;
    private static CancellationTokenSource _cts = new();
    static async Task Main(string[] args)
    {
        Console.WriteLine("Battleship Server v1.0");
        Console.WriteLine("=====================");
        int port = 42069;
        _server = new TcpListener(IPAddress.Any, port);
        try
        {
            _server.Start();
            Console.WriteLine($"Server started on port {port}. Waiting for clients...");
            await AcceptClientsAsync(_cts.Token);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Server error: {ex.Message}");
        }
        finally
        {
            _server?.Stop();
            Console.WriteLine("Server stopped.");
        }
    }
    private static async Task AcceptClientsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var client = await _server!.AcceptTcpClientAsync();
                Console.WriteLine($"New client connected from {client.Client.RemoteEndPoint}");
                _matchmaker.AddClient(client);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accepting client: {ex.Message}");
            }
        }
    }
}