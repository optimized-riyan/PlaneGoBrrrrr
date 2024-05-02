using Godot;

public partial class Global : Node
{
    [Export]
    public float AirDensity = .001f;
    [Export]
    public float Viscosity = 1.4f;
    [Export]
    public float LiftCurveSlope = 1.5f;
    [Export]
    public float AlphaStallPosBase = 1.48f;
    [Export]
    public float AlphaStallNegBase = 1.48f;

    public string IpAddr = "127.0.0.1";
    public int Port = 8500;
    public ENetMultiplayerPeer Peer;
}
