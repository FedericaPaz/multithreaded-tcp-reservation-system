using Common;
using Common.Models;
using static Common.Protocol.StatusCode;

namespace Server.Repositories;

public class UserRepository
{
    private static readonly Lazy<UserRepository> _instance = new(() => new UserRepository());
    public static UserRepository Instance => _instance.Value;
    private readonly List<User> _users = new();
    private readonly object _lock = new();
    
    private UserRepository(){}
    
    public bool Exists(string userName)
    {
        lock (_lock)
        {
            return _users.Any(u => u.Username.Equals(userName, StringComparison.OrdinalIgnoreCase));
        }
    }
    
    public User? Get(string userName)
    {
        lock (_lock)
        {
            return _users.FirstOrDefault(u => u.Username.Equals(userName, StringComparison.OrdinalIgnoreCase));
        }
    }
    
    public void Add(User user)
    {
        lock (_lock)
        {
            user.ValidateForRegistration();
            if (Exists(user.Username))
                throw new EdisonException(Conflict, "Username already exists.");
            _users.Add(user);
        }
    }

}