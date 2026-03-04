using UnityEngine;

[CreateAssetMenu(fileName = "LightningEffect", menuName = "BlockBlast/Effects/Lightning Effect")]
public class LightningEffect : ElementEffect
{
    [Tooltip("The number of random blocks that will be cleared when one lightning block is destroyed.")]
    [SerializeField] private int blocksToClear = 3;

    public override void ExecuteEffect(Board board, Vector2Int position)
    {
        // Add a number of charges to the board's lightning counter.
        board.AddLightningCharges(blocksToClear);
    }
}
