using Godot;
using CargoSpace.Core;
using CargoSpace.Shared;
using System.Collections.Generic;
using System.Linq;

namespace CargoSpace.Server
{
    public class JobManager
    {
        private List<Job> _jobBoard = new();
        private HashSet<Vector2I> _reservedTiles = new();
        private Dictionary<JobType, IJobBehavior> _behaviors = new();
        private NetworkBridge _networkBridge;

        public int BoardCount => _jobBoard.Count;

        public JobManager(NetworkBridge networkBridge)
        {
            _networkBridge = networkBridge;
            // Register behaviors
            _behaviors[JobType.SetState] = new SetStateJobBehavior();
            _behaviors[JobType.StartFire] = new StartFireJobBehavior();
            _behaviors[JobType.FightFire] = new FightFireJobBehavior();
            _behaviors[JobType.Operate] = new OperateJobBehavior();
            _behaviors[JobType.Haul] = new HaulJobBehavior();
        }

        public bool IsReserved(Vector2I target) => _reservedTiles.Contains(target);
        public void Reserve(Vector2I target) => _reservedTiles.Add(target);
        public void Release(Vector2I target) => _reservedTiles.Remove(target);

        public bool HasPendingJobForTarget(Vector2I target) => _jobBoard.Any(j => j.Target == target);
        public bool HasPendingHaulDestination(Vector2I destination) => _jobBoard.Any(j => j.Type == JobType.Haul && j.Destination == destination);

        public bool AddJob(Job job, bool isValidTile, bool isActivelyWorked, long peerId)
        {
            if (!isValidTile)
            {
                GameLogger.Debug($"Job rejected: target {job.Target} not valid/interactable");
                _networkBridge?.SendJobRejected(job.Id, peerId);
                return false;
            }

            bool destinationBlocked = job.Type == JobType.Haul &&
                (IsReserved(job.Destination) || HasPendingHaulDestination(job.Destination));

            if (HasPendingJobForTarget(job.Target) || IsReserved(job.Target) || isActivelyWorked || destinationBlocked)
            {
                GameLogger.Debug($"Job rejected: Tile {job.Target} is already pending, reserved, or actively worked.");
                _networkBridge?.SendJobRejected(job.Id, peerId);
                return false;
            }

            _jobBoard.Add(job);
            GameLogger.Debug($"Job added to board: {job.Id} {job.Type} at {job.Target}");
            _networkBridge?.BroadcastJobAdded(job);
            return true;
        }

        public Job? ClaimNextAvailableJob(out int index)
        {
            for (int i = 0; i < _jobBoard.Count; i++)
            {
                if (!_reservedTiles.Contains(_jobBoard[i].Target))
                {
                    index = i;
                    Job job = _jobBoard[i];
                    _jobBoard.RemoveAt(i);
                    _reservedTiles.Add(job.Target);
                    if (job.Type == JobType.Haul)
                    {
                        _reservedTiles.Add(job.Destination);
                    }
                    return job;
                }
            }
            index = -1;
            return null;
        }

        public void ExecuteJob(Job job, IJobExecutionContext context)
        {
            try
            {
                if (_behaviors.TryGetValue(job.Type, out var behavior))
                {
                    behavior.Execute(job, context);
                }
            }
            finally
            {
                // Operating jobs are continuous; the pawn holds the tile until explicitly released.
                if (job.Type != JobType.Operate)
                {
                    Release(job.Target);
                    if (job.Type == JobType.Haul)
                    {
                        Release(job.Destination);
                    }
                }
            }
        }

        public void CancelJob(JobId id, IEnumerable<Pawn> pawns)
        {
            foreach (var pawn in pawns)
            {
                if (pawn.CurrentJob.Id == id)
                {
                    // Handled via GridSimulation's ResetPawnState
                    return;
                }
            }

            for (int i = 0; i < _jobBoard.Count; i++)
            {
                if (_jobBoard[i].Id == id)
                {
                    _jobBoard.RemoveAt(i);
                    GameLogger.Debug($"Job cancelled and removed from board: {id}");
                    _networkBridge?.BroadcastJobRemoved(id);
                    return;
                }
            }
        }
    }
}
