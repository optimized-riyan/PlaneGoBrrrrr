using Godot;

public partial class Player : CharacterBody3D
{
    [Signal]
    public delegate void ResurrectedEventHandler();

    private const float MaxSpeed = 50f;
    private const float MinSpeed = 0f;
    private const float RollSpeed = 2f;
    private const float PitchSpeed = 2f;
    private const float YawSpeed = 1f;
    private const float Accel = 15f;

    private Node3D _jetModel;
    private Camera3D _thirdPersonCam;
    private Vector3 _initialCameraPos;
    private PackedScene _explosionPS;
    private bool _rollLeft = false;
    private bool _rollRight = false;
    private bool _pitchUp = false;
    private bool _pitchDown = false;
    private bool _yawLeft = false;
    private bool _yawRight = false;
    private bool _accelerate = false;
    private bool _decelerate = false;
    private bool _isAlive = true;
    private float _speed = MinSpeed;

    public bool IsFiring = false;


    public override void _Ready()
    {
        _jetModel = GetNode<Node3D>("Jet");
        _thirdPersonCam = GetNode<Camera3D>("ThirdPersonCam");
        _initialCameraPos = _thirdPersonCam.Position;
        _explosionPS = GD.Load<PackedScene>("res://scenes/explosion.tscn");
    }

    public override void _Process(double delta)
    {
        if (!_isAlive) return;
        if (Input.IsActionPressed("roll_left")) _rollLeft = true;
        if (Input.IsActionPressed("roll_right")) _rollRight = true;
        if (Input.IsActionPressed("pitch_up")) _pitchUp = true;
        if (Input.IsActionPressed("pitch_down")) _pitchDown = true;
        if (Input.IsActionPressed("yaw_left")) _yawLeft = true;
        if (Input.IsActionPressed("yaw_right")) _yawRight = true;
        if (Input.IsActionPressed("accelerate")) _accelerate = true;
        if (Input.IsActionPressed("decelerate")) _decelerate = true;

        if (Input.IsActionJustPressed("change_camera"))
        {
            if (GetViewport().GetCamera3D() == _thirdPersonCam)
                GetNode<Camera3D>("Jet/NoseCam").MakeCurrent();
            else
                _thirdPersonCam.MakeCurrent();
        }

        if (Input.IsActionPressed("fire"))
        {
            IsFiring = true;
            GetNode<Gun>("Jet/Gun").Fire();
        }
        else
            IsFiring = false;
    }

    public override void _PhysicsProcess(double delta)
    {
        float deltaF = (float)delta;
        if (_rollLeft)
        {
            _rollLeft = false;
            Rotate(Transform.Basis.Z, RollSpeed * deltaF);
            _jetModel.Rotation = _jetModel.Rotation.Lerp(new Vector3(0, 0, .6f), deltaF);
        }
        if (_rollRight)
        {
            _rollRight = false;
            Rotate(Transform.Basis.Z, -RollSpeed * deltaF);
            _jetModel.Rotation = _jetModel.Rotation.Lerp(new Vector3(0, 0, -.6f), deltaF);
        }
        if (_pitchUp)
        {
            _pitchUp = false;
            Rotate(Transform.Basis.X, PitchSpeed * deltaF);
            _jetModel.Rotation = _jetModel.Rotation.Lerp(new Vector3(.4f, 0, 0), deltaF);
        }
        if (_pitchDown)
        {
            _pitchDown = false;
            Rotate(Transform.Basis.X, -PitchSpeed * deltaF);
            _jetModel.Rotation = _jetModel.Rotation.Lerp(new Vector3(-.4f, 0, 0), deltaF);
        }
        if (_yawLeft)
        {
            _yawLeft = false;
            Rotate(Transform.Basis.Y, YawSpeed * deltaF);
            _jetModel.Rotation = _jetModel.Rotation.Lerp(new Vector3(0, .4f, .8f), deltaF);
        }
        if (_yawRight)
        {
            _yawRight = false;
            Rotate(Transform.Basis.Y, -YawSpeed * deltaF);
            _jetModel.Rotation = _jetModel.Rotation.Lerp(new Vector3(0, -.4f, -.8f), deltaF);
        }

        if (_accelerate)
        {
            _accelerate = false;
            _speed = Mathf.Min(_speed + Accel * deltaF, MaxSpeed);
        }
        if (_decelerate)
        {
            _decelerate = false;
            _speed = Mathf.Max(_speed - Accel * deltaF, MinSpeed);
        }

        Velocity = -Transform.Basis.Z * _speed;
        _jetModel.Rotation = _jetModel.Rotation.Lerp(Vector3.Zero, deltaF * 4);

        MoveAndSlide();
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferChannel = 0, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
    private void Die()
    {
        _isAlive = false;
        _jetModel.Visible = false;
        GetNode<CollisionShape3D>("CollisionShape3D").Disabled = true;
        SceneTreeTimer timer = GetTree().CreateTimer(3f);
        timer.Timeout += Resurrect;
    }

    private void Resurrect()
    {
        Position = new Vector3(0, 3, 0);
        Rotation = Vector3.Zero;
        _isAlive = true;
        _jetModel.Visible = true;
        GetNode<CollisionShape3D>("CollisionShape3D").Disabled = true;
        EmitSignal(SignalName.Resurrected);
    }

    public void KillOtherPlayer(long peerId)
    {
        RpcId(peerId, MethodName.Die);
    }
}
