using System;
using System.Collections;
using Godot;

public partial class Wing : Node3D
{
    [ExportGroup("Wing Properties")]
    [Export]
    private float SkinFrictionCoefficient = .02f;
    [Export]
    private float AspectRatio = 4f;
    [Export]
    private float ZeroLiftAoABase = 0;
    [Export]
    private float FlapFraction = .4f;
    [Export]
    private float StallAngleHighBaseDeg = 15f;
    [Export]
    private float StallAngleLowBaseDeg = -15f;
    [Export]
    private float Chord = 3f;

    private float _correctedLiftSlope;
    private float _theta;
    private float _flapEffectivenessFactor;
    private float _stallAngleHighBase;
    private float _stallAngleLowBase;
    private float _surfaceArea;

    private Player _aircraft;
    private Global _global;
    private Vector3 _positionFromCoM;
    private Vector3 _velocity;
    private float _flapAngle;
    private float _coeffOfLift;
    private float _coeffOfDrag;
    private float _coeffOfTorque;

    public Wing()
    {
        _theta = Mathf.Acos(2 * FlapFraction - 1);
        _flapEffectivenessFactor = 1 - (_theta - Mathf.Sin(_theta)) / Mathf.Pi;
        _surfaceArea = Chord * Chord * AspectRatio;
    }

    public override void _Ready()
    {
        _aircraft = GetOwner<Player>();
        _global = GetNode<Global>("/root/Global");
        _correctedLiftSlope = _global.LiftSlope * (AspectRatio / (AspectRatio + 2 * (AspectRatio + 4) / (AspectRatio + 2)));
        _positionFromCoM = Position - _aircraft.CenterOfMass;
        _stallAngleHighBase = Mathf.DegToRad(StallAngleHighBaseDeg);
        _stallAngleLowBase = Mathf.DegToRad(StallAngleLowBaseDeg);
    }

    public override void _PhysicsProcess(double delta)
    {
        UpdateCoefficients();
    }

    private void UpdateCoefficients()
    {
        _velocity = -(Basis * _aircraft.LinearVelocity) + _aircraft.AngularVelocity.Cross(_positionFromCoM); 
        _velocity = new Vector3(0, _velocity.Y, _velocity.Z);
        float angleOfAttack = Mathf.Atan2(_velocity.Y, _velocity.Z);

        (_coeffOfLift, _coeffOfDrag, _coeffOfTorque) = CalculateCoefficients(angleOfAttack, _flapAngle);
    }

    private Vector3 CalculateCoefficients(float angleOfAttack, float flapAngle)
    {
        float deltaLiftCoeff = _correctedLiftSlope * _flapEffectivenessFactor * _flapEffectivenessFactor * FlapEffectivenessCorrection(flapAngle) * flapAngle;
        float zeroLiftAoA = ZeroLiftAoABase - deltaLiftCoeff / _correctedLiftSlope;

        float liftCoeffMaxHigh = _correctedLiftSlope * (_stallAngleHighBase - ZeroLiftAoABase) + deltaLiftCoeff * LiftCoefficientMaxFraction(FlapFraction);
        float liftCoeffMaxLow = _correctedLiftSlope * (_stallAngleLowBase - ZeroLiftAoABase) + deltaLiftCoeff * LiftCoefficientMaxFraction(FlapFraction);

        float stallAngleHigh = zeroLiftAoA + liftCoeffMaxHigh / _correctedLiftSlope;
        float stallAngleLow = zeroLiftAoA + liftCoeffMaxLow / _correctedLiftSlope;

        bool isStalling = false;
        if (!(angleOfAttack < stallAngleHigh && angleOfAttack > stallAngleLow))
            isStalling = true;
        float coeffOfLift, coeffOfDrag, coeffOfTorque;

        if (!isStalling)
        {
            coeffOfLift = _correctedLiftSlope * (angleOfAttack - ZeroLiftAoABase);
            float inducedAngle = coeffOfLift / (Mathf.Pi * AspectRatio);
            float effectiveAoA = angleOfAttack - zeroLiftAoA - inducedAngle;
            float tangentialCoeff = SkinFrictionCoefficient * Mathf.Cos(effectiveAoA);
            float normalCoeff = (coeffOfLift + tangentialCoeff * Mathf.Sin(effectiveAoA)) / Mathf.Cos(effectiveAoA);
            coeffOfDrag = normalCoeff * Mathf.Sin(effectiveAoA) + tangentialCoeff * Mathf.Cos(effectiveAoA);
            coeffOfTorque = -normalCoeff * (.25f - .175f * (1 - 2f * effectiveAoA / Mathf.Pi));
        }
        else
        {
            coeffOfLift = _correctedLiftSlope * (angleOfAttack - zeroLiftAoA);
            float inducedAngle;
            if (angleOfAttack > stallAngleHigh)
                inducedAngle = Mathf.Lerp(coeffOfLift / (Mathf.Pi * AspectRatio), 0, (angleOfAttack - stallAngleHigh) / (Mathf.Pi / 2 - stallAngleHigh));
            else
                inducedAngle = Mathf.Lerp(coeffOfLift / (Mathf.Pi * AspectRatio), 0, (angleOfAttack - stallAngleLow) / (-Mathf.Pi / 2 - stallAngleLow));
            float effectiveAoA = angleOfAttack - zeroLiftAoA - inducedAngle;
            float normalCoeff = Calculate2DDragCoefficient(flapAngle) * Mathf.Sin(effectiveAoA) * (1 / (.56f + .44f * Mathf.Abs(Mathf.Sin(effectiveAoA))) - .41f * (1f - Mathf.Exp(-17f / AspectRatio)));
            float tangentialCoeff = .5f * SkinFrictionCoefficient * Mathf.Cos(effectiveAoA);
            coeffOfLift = normalCoeff * Mathf.Cos(effectiveAoA) - tangentialCoeff * Mathf.Sin(effectiveAoA);
            coeffOfDrag = normalCoeff * Mathf.Sin(effectiveAoA) + tangentialCoeff * Mathf.Cos(effectiveAoA);
            coeffOfTorque = -normalCoeff * (.25f - .175f * (1 - 2 * effectiveAoA / Mathf.Pi));
        }

        return new Vector3(coeffOfLift, coeffOfDrag, coeffOfTorque);
    }

    public Vector3[] GetForces(Vector3 positionFromCoM)
    {
        float velSq = _velocity.LengthSquared();
        // the forces will need to be converted to the plane's coordinates
        // TODO: verify
        Vector3 lift = _coeffOfLift * _global.AirProfileConstant * velSq * _surfaceArea * _velocity.Normalized().Cross(Vector3.Right);
        Vector3 drag = _coeffOfDrag * _global.AirProfileConstant * velSq * _surfaceArea * _velocity.Normalized();
        Vector3 torque = _coeffOfTorque * _global.AirProfileConstant * velSq * _surfaceArea * positionFromCoM.Cross(Vector3.Up);
        
        Vector3[] array = [lift+drag, torque];

        return array;
    }

    private static float FlapEffectivenessCorrection(float flapAngle)
    {
        return Mathf.Lerp(.8f, .4f, (Mathf.RadToDeg(Mathf.Abs(flapAngle)) - 10) / 50);
    }

    private static float LiftCoefficientMaxFraction(float flapFraction)
    {
        return Mathf.Clamp(1f - .5f * (flapFraction - .1f) / .3f, 0, 1f);
    }

    private static float Calculate2DDragCoefficient(float flapAngle)
    {
        return -4.26e-2f * flapAngle * flapAngle + 2.1e-1f * flapAngle + 1.98f;
    }

    public void FlapUp()
    {
        _flapAngle = .3f;
    }

    public void FlapDown()
    {
        _flapAngle = -3f;
    }

    public void ResetFlap()
    {
        _flapAngle = 0;
    }
}
