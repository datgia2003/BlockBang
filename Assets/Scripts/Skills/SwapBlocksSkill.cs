using UnityEngine;

[CreateAssetMenu(fileName = "SwapBlocksSkill", menuName = "BlockBlast/Skills/Swap Blocks")]
public class SwapBlocksSkill : Skill
{
    private Blocks blocksManager;

    public void Initialize(Blocks blocksManager)
    {
        this.blocksManager = blocksManager;
    }

    public override void Execute()
    {
        if (blocksManager != null)
        {
            blocksManager.GenerateNewBlocks();
        }
    }
}
