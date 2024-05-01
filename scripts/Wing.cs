using Godot;

public partial class Wing : Node3D
{
    // constants related to flight
    [Export]
    private const float LiftCurveSlope = .5f;
    [Export]
    private const float SkinFrictionCoeff = .01f;
    [Export]
    private const float ZeroAOA = .02f;
    [Export]
    private const float AspectRatio = 2f;
    [Export]
    private const float CLAlpha = LiftCurveSlope * (AspectRatio / (AspectRatio + 2 * (AspectRatio + 4)/(AspectRatio + 2)));
    [Export]
    private const float ChordRatio = .2f;
    [Export]
    private const float DeltaFlapAngle = .3f;

    private Player _aircraft;
    private Global _global;
    private Vector3 _velocity;
    private Vector3 _wind = Vector3.Zero;
    private float _coeffOfLift;
    private float _coeffOfDrag;
    private float _coeffOfTorque;
    private float _flapAngle;

    public override void _Ready()
    {
        _aircraft = GetOwner<Player>();
        _global = GetNode<Global>("/root/Global");
    }

    public override void _PhysicsProcess(double delta)
    {
        _velocity = _aircraft.LinearVelocity + _aircraft.AngularVelocity.Cross(Position) + _wind;
        UpdateCoefficients();
        ResetFlap();
    }

    private void UpdateCoefficients()
    {
        float alpha, cT, cN;

        alpha = (_velocity.Z == 0 && _velocity.X == 0) ? 0 : Mathf.Atan(_velocity.Z/_velocity.X);
        _coeffOfLift = CLAlpha * (alpha - ZeroAOA);
        alpha = alpha - ZeroAOA - _coeffOfLift/(Mathf.Pi * AspectRatio);
        cT = SkinFrictionCoeff * Mathf.Cos(alpha);
        cN = (_coeffOfLift + cT * Mathf.Sin(alpha))/Mathf.Cos(alpha);
        _coeffOfDrag = cN * Mathf.Sin(alpha) + cT * Mathf.Cos(alpha);
        _coeffOfTorque = -cN * (.25f - .175f * (1 - 2 * alpha / Mathf.Pi));

        if (_flapAngle > 0)
        {
            float theta = Mathf.Acos(2 * ChordRatio - 1);
            float tau = 1 - (theta - Mathf.Sin(theta))/Mathf.Pi;
            float deltaLiftCoeff = CLAlpha * tau * _global.Viscosity * _flapAngle;
            _coeffOfLift -= deltaLiftCoeff;
        }
    }

    public void FlapUp()
    {
        _flapAngle = DeltaFlapAngle;
    }

    public void FlapDown()
    {
        _flapAngle = -DeltaFlapAngle;
    }

    private void ResetFlap()
    {
        _flapAngle = 0;
    }

    public Vector3 CalculateLift()
    {
        return ToGlobal(_coeffOfLift * _global.AirDensity * _aircraft.LinearVelocity.Length() / 2 * Vector3.Up);
    }

    public Vector3 CalculateDrag()
    {
        return ToGlobal(_coeffOfDrag * _global.AirDensity * _aircraft.LinearVelocity.Length() / 2 * Vector3.Back);
    }

    public Vector3 CalculateRotatoryForce()
    {
        return ToGlobal(_coeffOfTorque * _global.AirDensity * _aircraft.LinearVelocity.Length() / 2 * Vector3.Up);
    }
}
