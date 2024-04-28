using Godot;

public partial class Enemy : Node3D
{
    public bool IsFiring = false;

    public override void _Process(double delta)
    {
        if (IsFiring)
            GetNode<Gun>("Jet/Gun").Fire();
    }
}
