public interface INetworkChatTransport
{
    void SendChatMessage(string sender, string message);
    bool TryReceiveChatMessage(out string sender, out string message);
}
