using Godot;

public partial class Player : RigidBody3D
{
    [Signal]
    public delegate void ResurrectedEventHandler();

    private const float MaxSpeed = 50f;
    private const float MinSpeed = 0f;
    private const float Thrust = 1500f;

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
    private Wing _frontLeftWing;
    private Wing _frontRightWing;
    private Wing _backLeftWing;
    private Wing _backRightWing;
    private Wing _rudder;

    public bool IsFiring = false;


    public override void _Ready()
    {
        _jetModel = GetNode<Node3D>("Jet");
        _thirdPersonCam = GetNode<Camera3D>("SpringArm3D/ThirdPersonCam");
        _initialCameraPos = _thirdPersonCam.Position;
        _explosionPS = GD.Load<PackedScene>("res://scenes/explosion.tscn");

        _frontLeftWing = GetNode<Wing>("FrontLeftWing");
        _frontRightWing = GetNode<Wing>("FrontRightWing");
        _backLeftWing = GetNode<Wing>("BackLeftWing");
        _backRightWing = GetNode<Wing>("BackRightWing");
        _rudder = GetNode<Wing>("Rudder");

        ApplyCentralImpulse(new Vector3(0, 0, -200));
    }

    public override void _Process(double delta)
    {
        // GD.Print(AngularVelocity.Cross(_backLeftWing.Position));
        if (!_isAlive) return;
        _rollLeft = Input.IsActionPressed("roll_left");
        _rollRight = Input.IsActionPressed("roll_right");
        _pitchUp = Input.IsActionPressed("pitch_up");
        _pitchDown = Input.IsActionPressed("pitch_down");
        _yawLeft = Input.IsActionPressed("yaw_left");
        _yawRight = Input.IsActionPressed("yaw_right");
        _accelerate = Input.IsActionPressed("accelerate");
        _decelerate = Input.IsActionPressed("decelerate");

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
        if (_rollLeft)
        {
            _frontLeftWing.FlapUp();
            _frontRightWing.FlapDown();
        }
        if (_rollRight)
        {
            _frontLeftWing.FlapDown();
            _frontRightWing.FlapUp();
        }
        if (_pitchUp)
        {
            _backLeftWing.FlapDown();
            _backRightWing.FlapDown();
        }
        if (_pitchDown)
        {
            _backLeftWing.FlapUp();
            _backRightWing.FlapUp();
        }
        if (_yawLeft)
        {
            _rudder.FlapDown();
        }
        if (_yawRight)
        {
            _rudder.FlapUp();
        }

        Vector3[] forceArray;
        Vector3 totalForce = Vector3.Zero;
        Vector3 totalTorque = Vector3.Zero;
        forceArray = _frontLeftWing.GetForces(_frontLeftWing.Position - CenterOfMass);
        totalForce += forceArray[0];
        totalTorque += forceArray[1];
        forceArray = _frontRightWing.GetForces(_frontRightWing.Position - CenterOfMass);
        totalForce += forceArray[0];
        totalTorque += forceArray[1];
        forceArray = _backLeftWing.GetForces(_backLeftWing.Position - CenterOfMass);
        totalForce += forceArray[0];
        totalTorque += forceArray[1];
        forceArray = _backRightWing.GetForces(_backRightWing.Position - CenterOfMass);
        totalForce += forceArray[0];
        totalTorque += forceArray[1];
        forceArray = _rudder.GetForces(_rudder.Position - CenterOfMass);
        totalForce += forceArray[0];
        totalTorque += forceArray[1];
        ApplyCentralForce(Basis * totalForce);
        ApplyTorque(Basis * totalTorque);

        if (_accelerate)
        {
            if (LinearVelocity.Length() < MaxSpeed) ApplyCentralForce(-Thrust * Basis.Z);
        }
        if (_decelerate)
        {
            if (LinearVelocity.Length() < MinSpeed) ApplyCentralForce(Thrust * Basis.Z);
        }
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
