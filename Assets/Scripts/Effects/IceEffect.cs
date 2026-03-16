using UnityEngine;

[CreateAssetMenu(fileName = "IceEffect", menuName = "BlockBlast/Effects/Ice Effect")]
public class IceEffect : ElementEffect
{
    public override void ExecuteEffect(Board board, Vector2Int position)
    {
        LevelModeManager.Instance?.OnGoalProgress(LevelGoalType.TriggerIce, 1);
        
        // The Ice block "shatters" on first hit — becomes a Normal block.
        board.SetCellAsOccupied(position.x, position.y);
        board.SetElementAt(position.x, position.y, Element.Normal);
        
        // Punch scale the cell and pop block burst
        board.PlayIceShatterAnim(position);

        // === VFX: ice shatter at this position ===
        var worldPos = board.GetCellWorldPosition(position.x, position.y);
        ElementVFX.Instance?.PlayIceVFX(worldPos);
    }
}
