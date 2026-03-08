using UnityEngine;

[CreateAssetMenu(fileName = "FireEffect", menuName = "BlockBlast/Effects/Fire Effect")]
public class FireEffect : ElementEffect
{
    public override void ExecuteEffect(Board board, Vector2Int position)
    {
        // === VFX: fire explosion at this cell ===
        var worldPos = board.GetCellWorldPosition(position.x, position.y);
        ElementVFX.Instance?.PlayFireVFX(worldPos);

        // Explode and clear all adjacent cells (including diagonals)
        for (int dr = -1; dr <= 1; dr++)
        {
            for (int dc = -1; dc <= 1; dc++)
            {
                if (dr == 0 && dc == 0) continue;

                int nr = position.y + dr;
                int nc = position.x + dc;

                if (board.IsWithinBounds(nc, nr))
                    board.HandleCellClear(nr, nc);
            }
        }
    }
}
