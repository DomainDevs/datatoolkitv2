namespace DataToolkit.Builder.Connections;

public interface IUserConnectionStore
{
    void Set(string userId, string connectionString);
    string? Get(string userId);
    void Remove(string userId);
}
