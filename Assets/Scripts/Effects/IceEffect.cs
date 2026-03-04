using UnityEngine;

[CreateAssetMenu(fileName = "IceEffect", menuName = "BlockBlast/Effects/Ice Effect")]
public class IceEffect : ElementEffect
{
    public override void ExecuteEffect(Board board, Vector2Int position)
    {
        // The Ice block is not cleared on the first pass.
        // It "melts" and becomes a Normal block.
        // The HandleCellClear in Board.cs will have already cleared the data for this cell,
        // so we need to set it back to a normal occupied state.
        board.SetCellAsOccupied(position.x, position.y);
        board.SetElementAt(position.x, position.y, Element.Normal);
    }
}
