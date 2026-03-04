using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Blocks : MonoBehaviour
{
    [SerializeField] private Block[] blocks;
    [SerializeField] private Board board;
    private int[] polyominoIndexes;
    private int blockCount = 0;
    private bool isGameOver = false;

    void Update()
    {
        if (isGameOver == true && Input.GetMouseButtonDown(0))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    void Start()
    {
        var blockWidth = (float)Board.Size / blocks.Length;
        var cellSize = (float)Board.Size / (Block.Size * blocks.Length + blocks.Length + 1);
        for (var i = 0; i < blocks.Length; ++i)
        {
            blocks[i].transform.localPosition = new(blockWidth * (i + 0.5f), -0.25f - cellSize * 4.0f, 0.0f);
            blocks[i].transform.localScale = new(cellSize, cellSize, cellSize);
            blocks[i].Initialize();
        }
        polyominoIndexes = new int[blocks.Length];
        GenerateNewBlocks();
    }

    public void GenerateNewBlocks()
    {
        blockCount = 0;
        for (var i = 0; i < blocks.Length; ++i)
        {
            polyominoIndexes[i] = Random.Range(0, Polyominos.Length);
            blocks[i].gameObject.SetActive(true);
            blocks[i].Show(polyominoIndexes[i]);
            ++blockCount;
        }
    }

    public void Remove()
    {
        --blockCount;
        if (blockCount <= 0)
        {
            blockCount = 0;
            GenerateNewBlocks();
        }
        var isLose = true;
        for (var i = 0; i < blocks.Length; ++i)
        {
            if (blocks[i].gameObject.activeSelf == true && board.CheckPlace(polyominoIndexes[i]) == true)
            {
                isLose = false;
                break;
            }
        }
        if (isLose == true)
        {
            if (SkillManager.Instance != null && SkillManager.Instance.AreAnySkillsReady())
            {
                // A skill is ready, so don't end the game yet.
                // The player might be able to use the skill to continue.
            }
            else
            {
                isGameOver = true;
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowGameOverScreen();
                }
            }
        }
    }
    public void ResetSortingOrder()
    {
        for (var i = 0; i < blocks.Length; ++i)
        {
            blocks[i].SetSortingOrder(0);
        }
    }
}
