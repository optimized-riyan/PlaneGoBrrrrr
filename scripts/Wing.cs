using Godot;

public partial class Wing : Node3D
{
    [ExportGroup("Wing Properties")]
    [Export]
    private float LiftCurveSlope = .5f;
    [Export]
    private float SkinFrictionCoeff = .01f;
    [Export]
    private float ZeroAOABase = .02f;
    [Export]
    private float AspectRatio = 2f;
    [Export]
    private float ChordRatio = .2f;
    [Export]
    private float DeltaFlapAngle = .3f;
    [Export]
    private float SurfaceArea = 25f;
    [Export]
    private float StallAngle = .26179f;

    [ExportGroup("Other Properties")]
    [Export]
    private float IndicatorScale = .3f;

    private float CLAlpha;
    private Player _aircraft;
    private Global _global;
    private MeshInstance3D _indicator;
    private Vector3 _velocity;
    private Vector3 _wind = Vector3.Zero;
    private float _coeffOfLift;
    private float _coeffOfDrag;
    private float _coeffOfTorque;
    private float _lift;
    private float _drag;
    private float _rotatoryForce;
    private float _flapAngle;
    private float _cNu;

    public Wing()
    {
        CLAlpha = LiftCurveSlope * (AspectRatio / (AspectRatio + 2 * (AspectRatio + 4)/(AspectRatio + 2)));
        _cNu = 1 - Mathf.Exp(-17/AspectRatio);
    }

    public override void _Ready()
    {
        _aircraft = GetOwner<Player>();
        _global = GetNode<Global>("/root/Global");
        _indicator = GetNode<MeshInstance3D>("Indicator");
    }

    public override void _PhysicsProcess(double delta)
    {
        _velocity = -_aircraft.LinearVelocity - _aircraft.AngularVelocity.Cross(Position) + _wind;
        UpdateCoefficients();
        UpdateIndicator();
        ResetFlap();
    }

    private void UpdateCoefficients()
    {
        float alpha, cT, cN, ZeroAOA;
        float theta = Mathf.Acos(2 * ChordRatio - 1);
        float tau = 1 - (theta - Mathf.Sin(theta))/Mathf.Pi;

        ZeroAOA = ZeroAOABase - tau * _global.Viscosity * _flapAngle;

        alpha = (_velocity.Y == 0 && _velocity.Z == 0) ? 0 : Mathf.Atan2(_velocity.Y, _velocity.Z);
        _coeffOfLift = CLAlpha * (alpha - ZeroAOABase);
        if (Mathf.Abs(alpha) < StallAngle)
        {
            alpha = alpha - ZeroAOA - _coeffOfLift/(Mathf.Pi * AspectRatio);
            cT = SkinFrictionCoeff * Mathf.Cos(alpha);
            cN = (_coeffOfLift + cT * Mathf.Sin(alpha))/Mathf.Cos(alpha);
            _coeffOfDrag = cN * Mathf.Sin(alpha) + cT * Mathf.Cos(alpha);
            _coeffOfTorque = -cN * (.25f - .175f * (1 - 2 * alpha / Mathf.Pi));
        }
        else
        {
            cN = SkinFrictionCoeff * Mathf.Sin(alpha) * (1/(.56f + .44f * Mathf.Sin(alpha)) - .41f * _cNu);
            cT = .5f * SkinFrictionCoeff * Mathf.Cos(alpha);
            _coeffOfLift = cN * Mathf.Cos(alpha) - cT * Mathf.Sin(alpha);
            _coeffOfDrag = cN * Mathf.Sin(alpha) + cT * Mathf.Cos(alpha);
            _coeffOfTorque = -cN * (.25f - .175f * (1f - 2f * alpha / Mathf.Pi));
        }

        if (Mathf.Abs(_flapAngle) > 0)
        {
            float deltaLiftCoeff = CLAlpha * tau * _global.Viscosity * _flapAngle;
            _coeffOfLift -= deltaLiftCoeff;
        }
        GD.PrintS(Name, _lift, _drag, _rotatoryForce);
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
        _lift = _coeffOfLift * _global.AirDensity * _aircraft.LinearVelocity.Length() / 2 * SurfaceArea;
        return _lift * Vector3.Up;
    }

    public Vector3 CalculateDrag()
    {
        _drag = _coeffOfDrag * _global.AirDensity * _aircraft.LinearVelocity.Length() / 2 * SurfaceArea;
        return _drag * Vector3.Back;
    }

    public Vector3 CalculateRotatoryForce()
    {
        _rotatoryForce = _coeffOfTorque * _global.AirDensity * _aircraft.LinearVelocity.Length() / 2 * SurfaceArea;
        return _rotatoryForce * Vector3.Up;
    }

    private void UpdateIndicator()
    {
        RibbonTrailMesh mesh = (RibbonTrailMesh)_indicator.Mesh;
        _indicator.Position = new Vector3(0, IndicatorScale * _lift, 0);
        mesh.SectionLength = _indicator.Position.Y;
    }
}
