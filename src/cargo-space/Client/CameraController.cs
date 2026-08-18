using Godot;
using CargoSpace.Core;

namespace CargoSpace.Client
{
    public partial class CameraController : Camera2D
    {
        [Export]
        public float PanSpeed = 500f;

        public override void _Process(double delta)
        {
            // Get input from custom camera actions (defined in Godot Input Map)
            Vector2 inputDir = Input.GetVector("camera_pan_left", "camera_pan_right", "camera_pan_up", "camera_pan_down");
            
            // Pan camera based on input
            if (inputDir != Vector2.Zero)
            {
                Position += inputDir * PanSpeed * (float)delta;
            }
        }

        public void CenterOnGrid(int gridWidth, int gridHeight)
        {
            // Center camera on the grid
            Position = new Vector2(gridWidth / 2f, gridHeight / 2f);
            GameLogger.Debug($"Camera centered on grid at {Position}");
        }
    }
}
