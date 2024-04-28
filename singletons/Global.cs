using Godot;

public partial class Global : Node
{
    public string IpAddr = "127.0.0.1";
    public int Port = 8500;
    public ENetMultiplayerPeer Peer;
}
