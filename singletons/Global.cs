using Godot;

public partial class Global : Node
{
    [Export]
    public float AirDensity = 1.225f;
    [Export]
    public float Viscosity = 14f;
    [Export]
    public float LiftCurveSlope = 1.5f;
    [Export]
    public float AlphaStallPosBase = .2617f;
    [Export]
    public float AlphaStallNegBase = .2617f;
    [Export]
    public float DeltaCLMax = .2f;

    public string IpAddr = "127.0.0.1";
    public int Port = 8500;
    public ENetMultiplayerPeer Peer;
}
