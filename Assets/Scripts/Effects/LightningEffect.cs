using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LightningEffect", menuName = "BlockBlast/Effects/Lightning Effect")]
public class LightningEffect : ElementEffect
{
    [Tooltip("The number of random blocks that will be cleared when one lightning block is destroyed.")]
    [SerializeField] private int blocksToClear = 3;

    public override void ExecuteEffect(Board board, Vector2Int position)
    {
        LevelModeManager.Instance?.OnGoalProgress(LevelGoalType.TriggerLightning, 1);
        
        // === VFX: lightning arc from this cell to future targets ===
        // We peek at what cells will be cleared (already occupied) and draw arcs.
        var origin = board.GetCellWorldPosition(position.x, position.y);
        var targets = board.PeekRandomOccupiedCells(blocksToClear);

        if (targets.Count > 0 && ElementVFX.Instance != null)
        {
            foreach (var t in targets)
            {
                var targetPos = board.GetCellWorldPosition(t.x, t.y);
                var arcHandle = ElementVFX.Instance.SpawnPersistentLightningArc(origin, targetPos);
                board.EnqueueLightningClear(t, arcHandle);
            }
            
            // Screen flash + sound
            ElementVFX.Instance.PlayLightningGlobalEffects();
        }
        else if (targets.Count > 0)
        {
            foreach (var t in targets)
            {
                board.EnqueueLightningClear(t, null); // No VFX fallback
            }
        }
    }
}
