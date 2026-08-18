using Godot;

namespace CargoSpace.Client;

public partial class PawnVisual : Node2D
{
    public override void _Draw()
    {
        Color color = Colors.Red;
        DrawCircle(new Vector2(16, 8), 6, color); // Head
        DrawLine(new Vector2(16, 14), new Vector2(16, 24), color, 2); // Body
        DrawLine(new Vector2(16, 16), new Vector2(8, 20), color, 2); // Left Arm
        DrawLine(new Vector2(16, 16), new Vector2(24, 20), color, 2); // Right Arm
        DrawLine(new Vector2(16, 24), new Vector2(10, 32), color, 2); // Left Leg
        DrawLine(new Vector2(16, 24), new Vector2(22, 32), color, 2); // Right Leg
    }
}
