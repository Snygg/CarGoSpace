using Godot;
using CargoSpace.Core;
using CargoSpace.Server;
using CargoSpace.Shared;
using System.Collections.Generic;

namespace CargoSpace.Client
{
    public partial class UIManager : CanvasLayer
    {
        private Panel _menuPanel;
        private Label _stateLabel;
        private Button _toggleButton;
        private ClientManager _clientManager;
        private NetworkBridge _networkBridge;
        private Vector2I _currentGridCoord;

        // Job Board UI
        private Panel _jobBoardPanel;
        private ScrollContainer _jobScrollContainer;
        private VBoxContainer _jobListContainer;
        private Dictionary<JobId, Control> _jobRows = new Dictionary<JobId, Control>();

        public void Initialize(ClientManager clientManager, NetworkBridge networkBridge)
        {
            _clientManager = clientManager;
            _networkBridge = networkBridge;
        }

        public override void _Ready()
        {
            // Create console menu panel
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

            // Create persistent left-side job board
            CreateJobBoard();
        }

        private void CreateJobBoard()
        {
            _jobBoardPanel = new Panel();
            _jobBoardPanel.AnchorLeft = 0;
            _jobBoardPanel.AnchorTop = 0;
            _jobBoardPanel.AnchorRight = 0;
            _jobBoardPanel.AnchorBottom = 1;
            _jobBoardPanel.OffsetLeft = 0;
            _jobBoardPanel.OffsetTop = 0;
            _jobBoardPanel.OffsetRight = 260;
            _jobBoardPanel.OffsetBottom = 0;
            AddChild(_jobBoardPanel);

            // Title label
            Label titleLabel = new Label();
            titleLabel.Text = "Job Board";
            titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
            titleLabel.AnchorLeft = 0;
            titleLabel.AnchorRight = 1;
            titleLabel.OffsetTop = 10;
            titleLabel.OffsetBottom = 30;
            _jobBoardPanel.AddChild(titleLabel);

            // Scroll container for job list
            _jobScrollContainer = new ScrollContainer();
            _jobScrollContainer.AnchorLeft = 0;
            _jobScrollContainer.AnchorTop = 0;
            _jobScrollContainer.AnchorRight = 1;
            _jobScrollContainer.AnchorBottom = 1;
            _jobScrollContainer.OffsetLeft = 10;
            _jobScrollContainer.OffsetTop = 40;
            _jobScrollContainer.OffsetRight = -10;
            _jobScrollContainer.OffsetBottom = -10;
            _jobBoardPanel.AddChild(_jobScrollContainer);

            // VBox container for job rows
            _jobListContainer = new VBoxContainer();
            _jobListContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            _jobListContainer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            _jobScrollContainer.AddChild(_jobListContainer);
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

        public void AddJobUI(JobId id, JobType type, Vector2I target)
        {
            if (_jobRows.ContainsKey(id))
            {
                return;
            }

            HBoxContainer row = new HBoxContainer();
            row.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

            Label jobLabel = new Label();
            jobLabel.Text = $"{GetJobDescription(type)} @ {target.X},{target.Y}";
            jobLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            row.AddChild(jobLabel);

            Button cancelButton = new Button();
            cancelButton.Text = "Cancel";
            cancelButton.Pressed += () => OnCancelJobPressed(id);
            row.AddChild(cancelButton);

            _jobListContainer.AddChild(row);
            _jobRows[id] = row;
        }

        public void RemoveJobUI(JobId id)
        {
            if (_jobRows.TryGetValue(id, out Control row))
            {
                row.QueueFree();
                _jobRows.Remove(id);
            }
        }

        private string GetJobDescription(JobType type)
        {
            return type switch
            {
                JobType.ToggleState => "Toggle Console",
                _ => type.ToString()
            };
        }

        private void OnTogglePressed()
        {
            // Optimistic UI: ClientManager creates a local GUID and shows the job immediately,
            // then sends the request to the server for validation.
            _clientManager?.QueueConsoleToggleJob(_currentGridCoord);
            HideMenu();
        }

        private void OnCancelJobPressed(JobId id)
        {
            _networkBridge?.SendCancelJobRequest(id);
        }
    }
}
