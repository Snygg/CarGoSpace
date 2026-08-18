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
        }

        public bool IsReserved(Vector2I target) => _reservedTiles.Contains(target);
        public void Reserve(Vector2I target) => _reservedTiles.Add(target);
        public void Release(Vector2I target) => _reservedTiles.Remove(target);

        public bool HasPendingJobForTarget(Vector2I target) => _jobBoard.Any(j => j.Target == target);

        public void AddJob(Job job, bool isValidTile, bool isActivelyWorked, long peerId)
        {
            if (!isValidTile)
            {
                GameLogger.Debug($"Job rejected: target {job.Target} not valid/interactable");
                _networkBridge?.SendJobRejected(job.Id, peerId);
                return;
            }

            if (HasPendingJobForTarget(job.Target) || IsReserved(job.Target) || isActivelyWorked)
            {
                GameLogger.Debug($"Job rejected: Tile {job.Target} is already pending, reserved, or actively worked.");
                _networkBridge?.SendJobRejected(job.Id, peerId);
                return;
            }

            _jobBoard.Add(job);
            GameLogger.Debug($"Job added to board: {job.Id} {job.Type} at {job.Target}");
            _networkBridge?.BroadcastJobAdded(job);
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
                Release(job.Target);
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
