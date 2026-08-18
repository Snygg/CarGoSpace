using Godot;
using CargoSpace.Core;
using CargoSpace.Server;
using CargoSpace.Shared;

namespace CargoSpace.Client
{
    public partial class UIManager : CanvasLayer
    {
        private Panel _menuPanel;
        private Label _stateLabel;
        private Button _toggleButton;
        private NetworkBridge _networkBridge;
        private Vector2I _currentGridCoord;

        public void Initialize(NetworkBridge networkBridge)
        {
            _networkBridge = networkBridge;
        }

        public override void _Ready()
        {
            // Create menu panel
            _menuPanel = new Panel();
            _menuPanel.Size = new Vector2(300, 150);
            _menuPanel.Position = new Vector2(100, 100);
            _menuPanel.Hide();
            AddChild(_menuPanel);

            // Create vertical container
            VBoxContainer vbox = new VBoxContainer();
            vbox.Size = new Vector2(280, 130);
            vbox.Position = new Vector2(10, 10);
            _menuPanel.AddChild(vbox);

            // Create state label
            _stateLabel = new Label();
            _stateLabel.Text = "Console: OFF";
            vbox.AddChild(_stateLabel);

            // Create toggle button
            _toggleButton = new Button();
            _toggleButton.Text = "Toggle Console";
            _toggleButton.Pressed += OnTogglePressed;
            vbox.AddChild(_toggleButton);
        }

        public void ShowConsoleMenu(Vector2I gridCoord, int currentState)
        {
            _currentGridCoord = gridCoord;
            _stateLabel.Text = $"Console: {(currentState == 1 ? "ON" : "OFF")}";
            _menuPanel.Show();
        }

        public void HideMenu()
        {
            _menuPanel.Hide();
        }

        private void OnTogglePressed()
        {
            _networkBridge?.SendJobCommand(_currentGridCoord, JobType.ToggleState);
            HideMenu();
        }
    }
}
