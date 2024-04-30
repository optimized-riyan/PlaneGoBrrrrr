using Godot;

public partial class Global : Node
{
    [Export]
    public float AirDensity = .001f;
    [Export]
    public float Viscosity = 1.4f;

    public string IpAddr = "127.0.0.1";
    public int Port = 8500;
    public ENetMultiplayerPeer Peer;
}
