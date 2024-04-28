using System;
using Godot;
using Godot.Collections;

public partial class Game : Node3D
{
    [Export]
    private Vector3 _enemySpawnPos;

    private const float TickRate = 20f;
    private const float GunDamage = 20f;

    private PackedScene _enemyPS = GD.Load<PackedScene>("res://scenes/enemy.tscn");
    private PackedScene _mainMenuPS = GD.Load<PackedScene>("res://scenes/main_menu.tscn");
    private long _enemyPeerId = 0;
    private Enemy _enemy;
    private bool _enemyIsAlive = false;
    private Player _player;
    private float _tick = 0f;
    private float _enemyHealth = 100f;
    private Timer _gameTimer;
    private Label _timeLeft;
    private Global _global;

    public override void _Ready()
    {
        _player = GetNode<Player>("Player");
        _gameTimer = GetNode<Timer>("GameTimer");
        _timeLeft = GetNode<Label>("UIRoot/TimeLeft");
        _global = GetNode<Global>("/root/Global");

        Multiplayer.MultiplayerPeer = _global.Peer;
        Multiplayer.PeerConnected += OnPlayerConnected;
        Multiplayer.PeerDisconnected += OnPlayerDisconnected;

        if (!Multiplayer.IsServer())
            Multiplayer.ServerDisconnected += OnServerDisconnected;
        else
            _gameTimer.Start();

        _player.GetNode<Gun>("Jet/Gun").EnemyHit += OnEnemyHit;
    }

    public override void _Process(double delta)
    {
        if (_enemyPeerId != 0)
        {
            _tick += (float)(TickRate / delta);
            if (_tick >= 1000f)
            {
                Sync();
                _tick -= 1000f;
            }
        }

        if (_gameTimer.IsStopped())
            _timeLeft.Text = "00:00";
        else
            _timeLeft.Text = SecondsToFormat(_gameTimer.TimeLeft);

        if (_enemyHealth <= 0)
        {
            _enemyIsAlive = false;
            // _player.Die.RpcId(_enemyPeerId);
            RpcId(_enemyPeerId, "player:die");
            _enemy.Visible = false;
        }
    }

    private static string SecondsToFormat(double timeLeft)
    {
        int time = (int)timeLeft;
        int minutes = time / 60;
        time %= 60;
        return $"{minutes}:{time}";
    }

    private void OnPlayerConnected(long playerId)
    {
        _enemyPeerId = playerId;
        _enemy = _enemyPS.Instantiate<Enemy>();
        _enemy.Position = _enemySpawnPos;
        _enemy.GetNode<CharacterBody3D>("Hitbox").Name = "Enemy";
        AddChild(_enemy);
        if (Multiplayer.IsServer())
            RpcId(_enemyPeerId, "sync_time", _gameTimer.TimeLeft);
    }

    private void OnPlayerDisconnected(long playerId)
    {
        _enemy.QueueFree();
        _enemyPeerId = 0;
        _enemyIsAlive = false;
    }

    private void OnServerDisconnected()
    {
        _global.Peer = null;
        GetTree().ChangeSceneToPacked(_mainMenuPS);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferChannel = 0, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
    private void UpdateEnemy(Dictionary<string, Variant> values)
    {
        Tween tween = CreateTween();
        tween.TweenProperty(_enemy, "position", (Vector3)values["position"], .1);
        tween.TweenProperty(_enemy, "rotation", (Vector3)values["rotation"], .1);
        tween.TweenProperty(_enemy.GetNode<Node3D>("Jet"), "rotation", (Vector3)values["model_rotation"], .1);
        _enemy.IsFiring = (float)values["fire"] > 0;
    }

    private void Sync()
    {
        Dictionary<string, Variant> values = new Dictionary<string, Variant>
        {
            { "position", _player.Position },
            { "rotation", _player.Rotation },
            { "model_rotation", _enemy.GetNode<Node3D>("Jet").Rotation },
            { "fire", _player.IsFiring }
        };
        RpcId(_enemyPeerId, "update_enemy", values);
    }

    private void OnEnemyHit()
    {
        _enemyHealth -= GunDamage;
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferChannel = 0, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
    private void SyncTime(int time)
    {
        _gameTimer.WaitTime = time;
        _gameTimer.Start();
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferChannel = 0, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
    private void ResurrectEnemy()
    {
        _enemyIsAlive = true;
        _enemyHealth = 100f;
        _enemy.Position = _enemySpawnPos;
        _enemy.Visible = true;
    }

    private void OnPlayerResurrected()
    {
        RpcId(_enemyPeerId, "resurrect_enemy");
    }
}