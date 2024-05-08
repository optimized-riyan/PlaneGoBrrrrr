using System;
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

    private float _correctedLiftSlope;
    private float _theta;
    private float _flapEffectivenessFactor;
    private float _stallAngleHighBase;
    private float _stallAngleLowBase;

    private Player _aircraft;
    private Global _global;
    private Vector3 _positionFromCoM;
    private float _flapAngle;

    public Wing()
    {
        _theta = Mathf.Acos(2 * FlapFraction - 1);
        _flapEffectivenessFactor = 1 - (_theta - Mathf.Sin(_theta)) / Mathf.Pi;
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

    public Vector3 CalculateCoefficients(float angleOfAttack, float flapAngle)
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
        float coeffOfLift = 0, coeffOfDrag = 0, coeffOfTorque = 0;

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

    private float FlapEffectivenessCorrection(float flapAngle)
    {
        return Mathf.Lerp(.8f, .4f, (Mathf.RadToDeg(Mathf.Abs(flapAngle)) - 10) / 50);
    }

    private float LiftCoefficientMaxFraction(float flapFraction)
    {
        return Mathf.Clamp(0, 1, 1f - .5f * (flapFraction - .1f) / .3f);
    }

    private float Calculate2DDragCoefficient(float flapAngle)
    {
        return -4.26e-2f * flapAngle * flapAngle + 2.1e-1f * flapAngle + 1.98f;
    }
}
