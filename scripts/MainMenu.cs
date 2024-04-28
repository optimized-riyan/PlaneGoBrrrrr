using Godot;

public partial class MainMenu : Control
{
    private PackedScene _gamePS;
    private Global _global;

    public override void _Ready()
    {
        _gamePS = GD.Load<PackedScene>("res://scenes/game.tscn");
        _global = GetNode<Global>("/root/Global");
    }

    private void OnHostPressed()
    {
        ENetMultiplayerPeer peer = new ENetMultiplayerPeer();
        Error error = peer.CreateServer(_global.Port, 2);
        if (error != Error.Ok)
        {
            GD.Print(error);
            GetTree().Quit();
        }
        _global.Peer = peer;
        GetTree().ChangeSceneToPacked(_gamePS);
    }

    private void OnJoinPressed()
    {
        ENetMultiplayerPeer peer = new ENetMultiplayerPeer();
        Error error = peer.CreateClient(_global.IpAddr, _global.Port);
        if (error != Error.Ok)
        {
            GD.Print(error);
            GetTree().Quit();
        }
        _global.Peer = peer;
        GetTree().ChangeSceneToPacked(_gamePS);
    }

    private void OnQuitPressed()
    {
        GetTree().Quit();
    }
}
