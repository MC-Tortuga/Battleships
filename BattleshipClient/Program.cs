using BattleshipClient;
namespace BattleshipClient;
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Battleship Client");
        Console.WriteLine("==================");
        Console.WriteLine();
        string server = "127.0.0.1";
        int port = 42069;
        try
        {
            var client = new GameClient(server, port);
            var ui = new ConsoleUI(client);
            await ui.StartAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to connect to server: {ex.Message}");
            Console.WriteLine("Make sure the server is running and try again.");
        }
    }
}