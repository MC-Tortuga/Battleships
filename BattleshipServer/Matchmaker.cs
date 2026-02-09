using System.Net.Sockets;
using System.Collections.Concurrent;
namespace BattleshipServer;

public class Matchmaker
{
    private readonly ConcurrentQueue<TcpClient> _waitingClients = new();
    private readonly ConcurrentDictionary<int, GameSession> _activeSessions = new();
    private int _nextSessionId = 1;
    public void AddClient(TcpClient client)
    {
        _waitingClients.Enqueue(client);
        Console.WriteLine("Matchmaker: Client added to queue. Attempting to create session...");
        TryCreateSession();
    }
    private void TryCreateSession()
    {
        if (_waitingClients.TryDequeue(out var client1))
        {
            if (_waitingClients.TryDequeue(out var client2))
            {
                var sessionId = _nextSessionId++;
                var session = new GameSession(sessionId);
                session.TryAddPlayer(client1);
                session.TryAddPlayer(client2);
                _activeSessions[sessionId] = session;
                Console.WriteLine($"Matchmaker: Created session {sessionId} for 2 players.");
            }
            else
            {
                _waitingClients.Enqueue(client1);
                Console.WriteLine("Matchmaker: Not enough clients for a new session.");
            }
        }
    }
    public void CleanupSession(int sessionId)
    {
        if (_activeSessions.TryRemove(sessionId, out var session))
        {
            session.DisconnectAll();
            Console.WriteLine($"Matchmaker: Cleaned up session {sessionId}.");
        }
    }
}
