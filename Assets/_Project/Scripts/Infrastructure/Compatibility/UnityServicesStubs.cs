namespace Unity.Services.Authentication
{
    public static class AuthenticationService
    {
        public static void SignInAnonymously()
        {
            // Stub implementation
        }
    }
}

namespace Unity.Services.Lobbies
{
    public class Lobby { }
    public class LobbyService { }
}

namespace Unity.Services.Relay
{
    public class Allocation
    {
        public string ConnectionData { get; set; }
        public string AllocationId { get; set; }
    }
    
    public class JoinAllocation
    {
        public string ConnectionData { get; set; }
        public string AllocationId { get; set; }
    }
    
    public static class RelayService
    {
        public static Allocation CreateAllocationAsync(int maxConnections)
        {
            return new Allocation();
        }
    }
}