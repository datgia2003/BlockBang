using UnityEngine;

[CreateAssetMenu(fileName = "NormalEffect", menuName = "BlockBlast/Effects/Normal Effect")]
public class NormalEffect : ElementEffect
{
    public override void ExecuteEffect(Board board, Vector2Int position)
    {
        // Normal blocks have no special effect.
    }
}
