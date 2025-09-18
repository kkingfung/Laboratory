// Temporary stub for Unity Services until packages can be properly installed
namespace Unity.Services.Authentication
{
    public static class AuthenticationService
    {
        public static bool IsSignedIn => false;
        public static void SignInAnonymously() { }
    }
}

namespace Unity.Services.Lobby
{
    public class Lobby
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }
    
    public static class LobbyService
    {
        public static void CreateLobby(string name, int maxPlayers) { }
        public static void JoinLobby(string lobbyId) { }
    }
}

namespace Unity.Services.Core
{
    public static class UnityServices
    {
        public static void Initialize() { }
    }
}
