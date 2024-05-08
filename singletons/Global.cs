using Godot;

public partial class Global : Node
{
    [Export]
    public float AirProfileConstant = 1;
    [Export]
    public float LiftSlope = 2 * Mathf.Pi;

    public string IpAddr = "127.0.0.1";
    public int Port = 8500;
    public ENetMultiplayerPeer Peer;
}
