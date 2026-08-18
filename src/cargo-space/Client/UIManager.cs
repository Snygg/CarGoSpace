using Godot;
using CargoSpace.Core;
using CargoSpace.Server;
using CargoSpace.Shared;
using System.Collections.Generic;

namespace CargoSpace.Client
{
    public partial class UIManager : CanvasLayer
    {
        private ClientManager _clientManager;
        private NetworkBridge _networkBridge;

        // Context slot (dynamic context menus)
        private PanelContainer _contextSlot;

        // HUD toolbar
        private VBoxContainer _hudToolbar;

        // Job Board UI
        private Panel _jobBoardPanel;
        private ScrollContainer _jobScrollContainer;
        private VBoxContainer _jobListContainer;
        private Dictionary<JobId, Control> _jobRows = new Dictionary<JobId, Control>();

        // Triage / unconfirmed hazard alerts
        private VBoxContainer _triageListContainer;
        private Dictionary<Vector2I, Control> _triageAlerts = new Dictionary<Vector2I, Control>();

        // Active job tracking so triage doesn't re-add a fire that already has a FightFire job
        private Dictionary<JobId, (JobType Type, Vector2I Target)> _jobInfo = new Dictionary<JobId, (JobType, Vector2I)>();

        public void Initialize(ClientManager clientManager, NetworkBridge networkBridge)
        {
            _clientManager = clientManager;
            _networkBridge = networkBridge;
        }

        public override void _Ready()
        {
            // Create context slot for dynamic menus
            _contextSlot = new PanelContainer();
            _contextSlot.AnchorLeft = 0;
            _contextSlot.AnchorTop = 1;
            _contextSlot.AnchorRight = 0;
            _contextSlot.AnchorBottom = 1;
            _contextSlot.OffsetLeft = 20;
            _contextSlot.OffsetTop = -200;
            _contextSlot.OffsetRight = 320;
            _contextSlot.OffsetBottom = -20;
            _contextSlot.GrowHorizontal = Control.GrowDirection.End;
            _contextSlot.GrowVertical = Control.GrowDirection.Begin;
            _contextSlot.Hide();
            AddChild(_contextSlot);

            // Create floating, togglable job board
            CreateJobBoard();

            // Create persistent left-aligned HUD toolbar
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

            // Scroll container for board content
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

            // Master content container
            VBoxContainer boardContent = new VBoxContainer();
            boardContent.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            boardContent.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            _jobScrollContainer.AddChild(boardContent);

            // Collapsible Triage section
            Button triageToggle = new Button();
            triageToggle.Text = "Unconfirmed Hazards";
            triageToggle.Pressed += () => _triageListContainer.Visible = !_triageListContainer.Visible;
            boardContent.AddChild(triageToggle);

            _triageListContainer = new VBoxContainer();
            _triageListContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            _triageListContainer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            boardContent.AddChild(_triageListContainer);

            // Separator
            HSeparator separator = new HSeparator();
            boardContent.AddChild(separator);

            // Active Jobs section
            Label jobsHeader = new Label();
            jobsHeader.Text = "Active Jobs";
            boardContent.AddChild(jobsHeader);

            _jobListContainer = new VBoxContainer();
            _jobListContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            _jobListContainer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            boardContent.AddChild(_jobListContainer);
        }

        public void ClearContextMenu()
        {
            foreach (Node child in _contextSlot.GetChildren())
            {
                child.QueueFree();
            }
            _contextSlot.Hide();
        }

        public void ShowContextMenu(Vector2I gridCoord, TileDefinition tileDef, int currentState, byte hazardState)
        {
            ClearContextMenu();
            _contextSlot.Show();

            VBoxContainer vbox = new VBoxContainer();
            vbox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            vbox.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            _contextSlot.AddChild(vbox);

            Label titleLabel = new Label();
            titleLabel.Text = tileDef.Name ?? tileDef.Type.ToString();
            vbox.AddChild(titleLabel);

            if (tileDef.Type == TileType.Console)
            {
                Label stateLabel = new Label();
                stateLabel.Text = $"Console: {(currentState == 1 ? "ON" : "OFF")}";
                vbox.AddChild(stateLabel);

                int desiredState = currentState == 0 ? 1 : 0;
                Button setStateButton = new Button();
                setStateButton.Text = currentState == 0 ? "Turn ON" : "Turn OFF";
                setStateButton.Pressed += () =>
                {
                    JobId jobId = JobId.Create();
                    AddJobUI(jobId, JobType.SetState, gridCoord);
                    _networkBridge?.SendJobCommand(jobId, gridCoord, JobType.SetState, desiredState);
                    ClearContextMenu();
                };
                vbox.AddChild(setStateButton);
            }

            Button startFireButton = new Button();
            startFireButton.Text = "Debug: Start Fire";
            startFireButton.Pressed += () =>
            {
                JobId jobId = JobId.Create();
                AddJobUI(jobId, JobType.StartFire, gridCoord);
                _networkBridge?.SendJobCommand(jobId, gridCoord, JobType.StartFire, 0);
                ClearContextMenu();
            };
            vbox.AddChild(startFireButton);
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
            _jobInfo[id] = (type, target);
        }

        public void RemoveJobUI(JobId id)
        {
            if (_jobRows.TryGetValue(id, out Control row))
            {
                row.QueueFree();
                _jobRows.Remove(id);
            }

            _jobInfo.Remove(id);
        }

        private bool HasActiveFightFireJob(Vector2I target)
        {
            foreach (var kvp in _jobInfo)
            {
                if (kvp.Value.Type == JobType.FightFire && kvp.Value.Target == target)
                {
                    return true;
                }
            }
            return false;
        }

        private string GetJobDescription(JobType type)
        {
            return type switch
            {
                JobType.SetState => "Set Console",
                JobType.StartFire => "Start Fire",
                JobType.FightFire => "Fight Fire",
                _ => type.ToString()
            };
        }

        public void TryDiscoverHazard(Vector2I target, byte hazardState, bool isVisible)
        {
            if (hazardState == 0)
            {
                if (_triageAlerts.TryGetValue(target, out Control existingAlert))
                {
                    existingAlert.QueueFree();
                    _triageAlerts.Remove(target);
                }
                return;
            }

            if (!isVisible || HasActiveFightFireJob(target) || _triageAlerts.ContainsKey(target))
            {
                return;
            }

            PanelContainer alertPanel = new PanelContainer();
            alertPanel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            alertPanel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
            {
                BgColor = new Color(0.4f, 0.12f, 0.05f, 0.9f)
            });

            HBoxContainer alert = new HBoxContainer();
            alert.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            alertPanel.AddChild(alert);

            Label alertLabel = new Label();
            alertLabel.Text = $"Fire at {target.X},{target.Y}";
            alertLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            alert.AddChild(alertLabel);

            Button confirmButton = new Button();
            confirmButton.Text = "+";
            confirmButton.Pressed += () =>
            {
                JobId jobId = JobId.Create();
                AddJobUI(jobId, JobType.FightFire, target);
                _networkBridge?.SendJobCommand(jobId, target, JobType.FightFire, 0);

                _triageAlerts.Remove(target);
                alertPanel.QueueFree();
            };
            alert.AddChild(confirmButton);

            _triageListContainer.AddChild(alertPanel);
            _triageAlerts[target] = alertPanel;
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
