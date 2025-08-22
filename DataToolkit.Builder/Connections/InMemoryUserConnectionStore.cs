namespace DataToolkit.Builder.Connections;

public class InMemoryUserConnectionStore : IUserConnectionStore
{
    private readonly Dictionary<string, string> _store = new();

    public void Set(string userId, string connectionString) => _store[userId] = connectionString;

    public string? Get(string userId) => _store.TryGetValue(userId, out var conn) ? conn : null;

    public void Remove(string userId) => _store.Remove(userId);
}
