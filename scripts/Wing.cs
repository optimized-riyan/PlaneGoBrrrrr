using System;
using Godot;

public partial class Wing : Node3D
{
    [ExportGroup("Wing Properties")]
    [Export]
    private float SkinFrictionCoeff = .02f;
    [Export]
    private float ZeroAOABase = 0;
    [Export]
    private float AspectRatio = 2f;
    [Export]
    private float Chord = 1f;
    [Export]
    private float FlapFraction = .1f;
    [Export]
    private float DeltaFlapAngle = .3f;

    private float _cLAlpha;
    private float _surfaceArea;
    private float _liftCoeffNegMaxBase;
    private float _liftCoeffPosMaxBase;

    private Player _aircraft;
    private Global _global;
    private Vector3 _positionFromCOM;
    private Vector3 _velocity;
    private Vector3 _wind;
    private float _dragCoeffAtStall;
    private float _coeffOfLift;
    private float _coeffOfDrag;
    private float _coeffOfTorque;
    private float _lift;
    private float _drag;
    private float _torque;
    private float _flapAngle;
    private float _cNu;

    public Wing()
    {
        _surfaceArea = Chord * Chord * AspectRatio;
        _cNu = 1 - Mathf.Exp(-17 / AspectRatio);
    }

    public override void _Ready()
    {
        _aircraft = GetOwner<Player>();
        _global = GetNode<Global>("/root/Global");
        _cLAlpha = _global.LiftCurveSlope * (AspectRatio / (AspectRatio + 2 * (AspectRatio + 4) / (AspectRatio + 2)));
        _liftCoeffPosMaxBase = _cLAlpha * (_global.AlphaStallPosBase - ZeroAOABase) + _global.DeltaCLMax;
        _liftCoeffNegMaxBase = _cLAlpha * (_global.AlphaStallNegBase - ZeroAOABase) + _global.DeltaCLMax;
        _positionFromCOM = Position - _aircraft.CenterOfMass;
    }

    public override void _Process(double delta)
    {
        // GD.PrintS(_coeffOfLift, _coeffOfDrag, _coeffOfTorque);
    }

    public override void _PhysicsProcess(double delta)
    {
        _velocity = -ToLocalBasis(_aircraft.LinearVelocity) + _aircraft.AngularVelocity.Cross(_positionFromCOM) + _wind;
        _velocity = new Vector3(0, _velocity.Y, _velocity.Z);
        UpdateCoefficients();
        ResetFlap();
    }

    private void UpdateCoefficients()
    {
        float alpha, zeroLiftAoA;
        float theta = Mathf.Acos(2 * FlapFraction - 1);
        float tau = 1 - (theta - Mathf.Sin(theta)) / Mathf.Pi;

        alpha = Mathf.Atan2(_velocity.Y, _velocity.Z);
        GD.PrintS(Name, alpha);

        zeroLiftAoA = ZeroAOABase - tau * FlapEffectivenessCorrection(_flapAngle) * _flapAngle;

        _dragCoeffAtStall = -4.26e-2f * _flapAngle * _flapAngle + 2.1e-1f * _flapAngle + 1.98f;

        float alphaStallPos = zeroLiftAoA + _liftCoeffPosMaxBase / _cLAlpha;
        float alphaStallNeg = zeroLiftAoA + _liftCoeffNegMaxBase / _cLAlpha;

        float paddingAnglePos = Mathf.DegToRad(Mathf.Lerp(15, 5, (Mathf.RadToDeg(_flapAngle) + 50) / 100));
        float paddingAngleNeg = Mathf.DegToRad(Mathf.Lerp(15, 5, (-Mathf.RadToDeg(_flapAngle) + 50) / 100));
        float paddedStallAnglePos = alphaStallPos + paddingAnglePos;
        float paddedStallAngleNeg = alphaStallNeg - paddingAngleNeg;

        if (alpha < alphaStallPos && alpha > alphaStallNeg)
        {
            Vector3 coeffs = CalculateCoeffsAtLowAoA(alpha, zeroLiftAoA);
            _coeffOfLift = coeffs.X;
            _coeffOfDrag = coeffs.Y;
            _coeffOfTorque = coeffs.Z;
        }
        else
        {
            if (alpha > paddedStallAnglePos || alpha < paddedStallAngleNeg)
            {
                Vector3 coeffs = CalculateCoeffsAtStall(alpha, zeroLiftAoA, alphaStallPos, alphaStallNeg);
                _coeffOfLift = coeffs.X;
                _coeffOfDrag = coeffs.Y;
                _coeffOfTorque = coeffs.Z;
            }
            else
            {
                float lerpParam;
                Vector3 aerodynamicCoefficientsLow;
                Vector3 aerodynamicCoefficientsStall;

                if (alpha > alphaStallPos)
                {
                    aerodynamicCoefficientsLow = CalculateCoeffsAtLowAoA(alphaStallPos, zeroLiftAoA);
                    aerodynamicCoefficientsStall = CalculateCoeffsAtStall(paddedStallAnglePos, zeroLiftAoA, alphaStallPos, alphaStallNeg);
                    lerpParam = (alpha - alphaStallPos) / (paddedStallAnglePos - alphaStallPos);
                }
                else
                {
                    aerodynamicCoefficientsLow = CalculateCoeffsAtLowAoA(alphaStallNeg, zeroLiftAoA);
                    aerodynamicCoefficientsStall = CalculateCoeffsAtStall(paddedStallAngleNeg, zeroLiftAoA, alphaStallPos, alphaStallNeg);
                    lerpParam = (alpha - alphaStallPos) / (paddedStallAnglePos - alphaStallPos);
                }
                Vector3 coeffs = aerodynamicCoefficientsLow.Lerp(aerodynamicCoefficientsStall, lerpParam);
                _coeffOfLift = coeffs.X;
                _coeffOfDrag = coeffs.Y;
                _coeffOfTorque = coeffs.Z;
            }
        }
        GD.PrintS(Name, _lift, _drag, _torque);
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
        _lift = _coeffOfLift * _global.AirDensity * _aircraft.LinearVelocity.Length() / 2 * _surfaceArea;
        return _lift * Vector3.Up;
    }

    public Vector3 CalculateDrag()
    {
        _drag = _coeffOfDrag * _global.AirDensity * _aircraft.LinearVelocity.Length() / 2 * _surfaceArea;
        return _drag * Vector3.Back;
    }

    public Vector3 CalculateRotatoryForce()
    {
        _torque = _coeffOfTorque * _global.AirDensity * _aircraft.LinearVelocity.Length() / 2 * _surfaceArea;
        return _torque * Vector3.Up;
    }

    private Vector3 CalculateCoeffsAtLowAoA(float alpha, float zeroLiftAOA)
    {
        float coeffOfLift = _cLAlpha * (alpha - zeroLiftAOA);
        float alphaEffictive = alpha - zeroLiftAOA - coeffOfLift / (Mathf.Pi * AspectRatio);
        float tangentialCoeff = SkinFrictionCoeff * Mathf.Cos(alphaEffictive);
        float normalCoeff = (coeffOfLift + tangentialCoeff * Mathf.Sin(alphaEffictive)) / Mathf.Cos(alphaEffictive);
        float coeffOfDrag = normalCoeff * Mathf.Sin(alphaEffictive) + tangentialCoeff * Mathf.Cos(alphaEffictive);
        float coeffOfTorque = -normalCoeff * (.25f - .175f * (1 - 2 * Mathf.Abs(alphaEffictive) / Mathf.Pi));

        return new Vector3(coeffOfLift, coeffOfDrag, coeffOfTorque);
    }

    private Vector3 CalculateCoeffsAtStall(float alpha, float zeroLiftAoA, float alphaStallPos, float alphaStallNeg)
    {
        float coeffOfLift;
        if (alpha > alphaStallPos)
        {
            coeffOfLift = _cLAlpha * (alphaStallPos - zeroLiftAoA);
        }
        else
        {
            coeffOfLift = _cLAlpha * (alphaStallNeg - zeroLiftAoA);
        }

        float lerpParam;
        if (alpha > alphaStallPos)
        {
            lerpParam = (Mathf.Pi / 2 - Mathf.Clamp(alpha, -Mathf.Pi / 2, Mathf.Pi / 2)) / (Mathf.Pi / 2 - alphaStallPos);
        }
        else
        {
            lerpParam = (-Mathf.Pi / 2 - Mathf.Clamp(alpha, -Mathf.Pi / 2, Mathf.Pi / 2)) / (-Mathf.Pi / 2 - alphaStallNeg);
        }

        float alphaEffective = alpha - zeroLiftAoA - Mathf.Lerp(0, coeffOfLift / (Mathf.Pi * AspectRatio), lerpParam);
        float normalCoeff = _dragCoeffAtStall * Mathf.Sin(alphaEffective) * (1 / (.56f + .44f * Mathf.Sin(alphaEffective)) - .41f * _cNu);
        float tangentCoeff = .5f * SkinFrictionCoeff * Mathf.Cos(alphaEffective);
        coeffOfLift = normalCoeff * Mathf.Cos(alphaEffective) - tangentCoeff * Mathf.Sin(alphaEffective);
        float coeffOfDrag = normalCoeff * Mathf.Sin(alphaEffective) + tangentCoeff * Mathf.Cos(alphaEffective);
        float coeffOfTorque = -normalCoeff * (.25f - .175f * (1f - 2f * alphaEffective / Mathf.Pi));

        return new Vector3(coeffOfLift, coeffOfDrag, coeffOfTorque);
    }

    private Vector3 ToLocalBasis(Vector3 v)
    {
        Basis basis = Transform.Basis.Transposed();
        return new Vector3(basis.X.Dot(v), basis.Y.Dot(v), basis.Z.Dot(v));
    }

    private float FlapEffectivenessCorrection(float flapAngle)
    {
        return Mathf.Lerp(0.8f, 0.4f, (Mathf.RadToDeg(Mathf.Abs(flapAngle)) - 10) / 50);
    }
}
