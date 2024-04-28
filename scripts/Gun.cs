using Godot;
using Godot.Collections;

public partial class Gun : RayCast3D
{
    [Signal]
    public delegate void EnemyHitEventHandler();

    private const int FireRate = 10;
    private const int ExplosionCount = 15;

    private int _firingCooldown = FireRate;
    private PackedScene _explosionPS = GD.Load<PackedScene>("res://scenes/explosion.tscn");
    private Array<GpuParticles3D> _explosionArray = new Array<GpuParticles3D>();
    private int _explosionArrayPtr = 0;

    public override void _Ready()
    {
        for (int i = 0; i < ExplosionCount; i++)
        {
            _explosionArray.Add(_explosionPS.Instantiate<GpuParticles3D>());
            AddChild(_explosionArray[_explosionArray.Count - 1]);
        }
    }

    public override void _Process(double delta)
    {
        _firingCooldown = _firingCooldown > 0 ? _firingCooldown - 1 : 0;
    }

    public void Fire()
    {
        if (_firingCooldown == 0)
        {
            _firingCooldown = FireRate;
            ForceRaycastUpdate();
            if (IsColliding())
            {
                _explosionArray[_explosionArrayPtr].GlobalPosition = GetCollisionPoint();
                _explosionArray[_explosionArrayPtr].Emitting = true;
                _explosionArrayPtr = (_explosionArrayPtr + 1) % ExplosionCount;
                if (((Node3D)GetCollider()).Name == "Enemy")
                    EmitSignal(SignalName.EnemyHit);
            }
        }
    }
}
