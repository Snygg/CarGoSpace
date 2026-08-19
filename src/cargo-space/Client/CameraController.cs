using Godot;
using CargoSpace.Core;

namespace CargoSpace.Client
{
    public partial class CameraController : Camera2D
    {
        [Export]
        public float PanSpeed = 500f;

        [Export]
        public float ZoomSpeed = 10f;

        private Vector2 _targetZoom = new Vector2(1, 1);

        public override void _Process(double delta)
        {
            // Get input from custom camera actions (defined in Godot Input Map)
            Vector2 inputDir = Input.GetVector("camera_pan_left", "camera_pan_right", "camera_pan_up", "camera_pan_down");
            
            // Pan camera based on input
            if (inputDir != Vector2.Zero)
            {
                Position += inputDir * PanSpeed * (float)delta;
            }

            // Smoothly interpolate to the target zoom level
            Zoom = Zoom.Lerp(_targetZoom, (float)delta * ZoomSpeed);
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            if (@event is InputEventMouseButton mouseBtn)
            {
                if (mouseBtn.ButtonIndex == MouseButton.WheelUp)
                    _targetZoom += new Vector2(0.1f, 0.1f);
                else if (mouseBtn.ButtonIndex == MouseButton.WheelDown)
                    _targetZoom -= new Vector2(0.1f, 0.1f);

                // Clamp zoom to reasonable levels (e.g., 0.5x to 2.0x)
                _targetZoom = _targetZoom.Clamp(new Vector2(0.5f, 0.5f), new Vector2(2.0f, 2.0f));
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
