using UnityEngine;

[CreateAssetMenu(fileName = "FireEffect", menuName = "BlockBlast/Effects/Fire Effect")]
public class FireEffect : ElementEffect
{
    public override void ExecuteEffect(Board board, Vector2Int position)
    {
        // === VFX: fire explosion at this cell ===
        var worldPos = board.GetCellWorldPosition(position.x, position.y);
        ElementVFX.Instance?.PlayFireVFX(worldPos);

        // Instead of immediate loop clearing, enqueue a 2s burn routine in Board
        board.StartFireDelayedClear(position);
    }
}
