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

        // HUD toolbar
        private VBoxContainer _hudToolbar;

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

            // Create floating, togglable job board
            CreateJobBoard();

            // Create persistent right-aligned HUD toolbar
            CreateHudToolbar();
        }

        private void CreateJobBoard()
        {
            _jobBoardPanel = new Panel();
            _jobBoardPanel.Size = new Vector2(300, 400);
            _jobBoardPanel.Position = new Vector2(50, 50);
            _jobBoardPanel.Hide();
            AddChild(_jobBoardPanel);

            // Header bar with title and close button
            HBoxContainer header = new HBoxContainer();
            header.Size = new Vector2(280, 30);
            header.Position = new Vector2(10, 10);
            _jobBoardPanel.AddChild(header);

            Label titleLabel = new Label();
            titleLabel.Text = "Job Board";
            titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
            titleLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            header.AddChild(titleLabel);

            Button closeButton = new Button();
            closeButton.Text = "Close";
            closeButton.Pressed += () => _jobBoardPanel.Hide();
            header.AddChild(closeButton);

            // Scroll container for job list
            _jobScrollContainer = new ScrollContainer();
            _jobScrollContainer.AnchorLeft = 0;
            _jobScrollContainer.AnchorTop = 0;
            _jobScrollContainer.AnchorRight = 1;
            _jobScrollContainer.AnchorBottom = 1;
            _jobScrollContainer.OffsetLeft = 10;
            _jobScrollContainer.OffsetTop = 50;
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

        private void CreateHudToolbar()
        {
            _hudToolbar = new VBoxContainer();
            _hudToolbar.AnchorLeft = 0;
            _hudToolbar.AnchorTop = 0.5f;
            _hudToolbar.AnchorRight = 0;
            _hudToolbar.AnchorBottom = 0.5f;
            _hudToolbar.OffsetLeft = 10;
            _hudToolbar.OffsetRight = 60;
            _hudToolbar.GrowHorizontal = Control.GrowDirection.End;
            _hudToolbar.GrowVertical = Control.GrowDirection.Both;
            AddChild(_hudToolbar);

            Button hudButton = new Button();
            hudButton.Text = "J";
            hudButton.CustomMinimumSize = new Vector2(50, 50);
            hudButton.Pressed += ToggleJobBoard;
            _hudToolbar.AddChild(hudButton);
        }

        private void ToggleJobBoard()
        {
            if (_jobBoardPanel.Visible)
            {
                _jobBoardPanel.Hide();
            }
            else
            {
                _jobBoardPanel.Show();
            }
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            if (@event.IsActionPressed("toggle_job_board") && !@event.IsEcho())
            {
                ToggleJobBoard();
                GetViewport().SetInputAsHandled();
            }
        }
    }
}
