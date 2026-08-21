using Godot;
using CargoSpace.Core;
using System.Collections.Generic;

namespace CargoSpace.Client
{
    public partial class ClientEntityManager : Node
    {
        private Dictionary<PawnId, PawnVisual> _pawnVisuals = new();

        public void UpdatePawnPosition(PawnId id, Vector2I gridPosition)
        {
            if (!_pawnVisuals.ContainsKey(id))
            {
                PawnVisual pawnVisual = new PawnVisual();
                pawnVisual.ZIndex = 10;
                AddChild(pawnVisual);
                _pawnVisuals[id] = pawnVisual;
            }

            PawnVisual visual = _pawnVisuals[id];

            Vector2 worldPosition = new Vector2(
                gridPosition.X * Constants.TileSize,
                gridPosition.Y * Constants.TileSize
            );

            visual.Position = worldPosition;
        }

        public void ClearPawns()
        {
            foreach (var kvp in _pawnVisuals)
            {
                kvp.Value.QueueFree();
            }
            _pawnVisuals.Clear();
        }
    }
}
