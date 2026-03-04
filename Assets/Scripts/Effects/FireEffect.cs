using UnityEngine;

[CreateAssetMenu(fileName = "FireEffect", menuName = "BlockBlast/Effects/Fire Effect")]
public class FireEffect : ElementEffect
{
    public override void ExecuteEffect(Board board, Vector2Int position)
    {
        // Explode and clear all adjacent cells (including diagonals)
        for (int dr = -1; dr <= 1; dr++)
        {
            for (int dc = -1; dc <= 1; dc++)
            {
                if (dr == 0 && dc == 0) continue; // Don't clear the center cell itself again

                int nr = position.y + dr;
                int nc = position.x + dc;

                if (board.IsWithinBounds(nc, nr))
                {
                    // Call HandleCellClear on the neighbor.
                    // This will recursively trigger other effects if any.
                    board.HandleCellClear(nr, nc);
                }
            }
        }
    }
}
