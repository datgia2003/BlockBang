using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LightningEffect", menuName = "BlockBlast/Effects/Lightning Effect")]
public class LightningEffect : ElementEffect
{
    [Tooltip("The number of random blocks that will be cleared when one lightning block is destroyed.")]
    [SerializeField] private int blocksToClear = 3;

    public override void ExecuteEffect(Board board, Vector2Int position)
    {
        // === VFX: lightning arc from this cell to future targets ===
        // We peek at what cells will be cleared (already occupied) and draw arcs.
        var origin = board.GetCellWorldPosition(position.x, position.y);
        var targets = board.PeekRandomOccupiedCells(blocksToClear);

        if (targets.Count > 0 && ElementVFX.Instance != null)
        {
            var targetPositions = new List<Vector3>(targets.Count);
            foreach (var t in targets)
                targetPositions.Add(board.GetCellWorldPosition(t.x, t.y));

            ElementVFX.Instance.PlayLightningVFX(origin, targetPositions);
        }

        // Add lightning charges to the board counter (cells cleared separately)
        board.AddLightningCharges(blocksToClear);
    }
}
