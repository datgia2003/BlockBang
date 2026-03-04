using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Polyominos
{
    private static readonly int[][,] polyominos = new int[][,]
    {
        new int[,]
        {
            {0,0,1},
            {0,0,1},
            {1,1,1}
        },
        new int[,]
        {
            {1,1},
            {1,1}
        },
        new int[,]
        {
            {1,1,1,1}
        },
        new int[,]
        {
            {1},
            {1},
            {1},
            {1}
        },
        new int[,]
        {
            {1,0},
            {1,0},
            {1,1}
        },
        new int[,]
        {
            {0,1},
            {0,1},
            {1,1}
        },
        new int[,]
        {
            {1,0},
            {1,1},
            {0,1}
        },
        new int[,]
        {
            {0,1},
            {1,1},
            {1,0}
        },
        new int[,]
        {
            {1,0,0},
            {1,1,1},
            {0,0,0}
        },
        new int[,]
        {
            {0,0,1},
            {1,1,1},
            {0,0,0}
        },
        new int[,]
        {
            {0,1,0},
            {1,1,1},
            {0,0,0}
        },
        new int[,]
        {
            {1,1,0},
            {0,1,1},
            {0,0,0}
        },
        new int[,]
        {
            {0,1,1},
            {1,1,0},
            {0,0,0}
        },
        new int[,]
        {
            {1,1,1},
            {0,1,0},
            {0,0,0}
        },
        new int[,]
        {
            {1,0,0},
            {1,0,0},
            {1,0,0},
            {1,1,1}
        }
    };

    static Polyominos()
    {
        foreach (var polyomino in polyominos)
        {
            ReverseRows(polyomino);
        }
    }
    public static int[,] Get(int index) => polyominos[index];
    public static int Length => polyominos.Length;

    static void ReverseRows(int[,] polyomino)
    {
        var polyominoRows = polyomino.GetLength(0);
        var polyominoColumns = polyomino.GetLength(1);
        for (var r = 0; r < polyominoRows / 2; ++r)
        {
            var topRow = r;
            var bottomRow = polyominoRows - 1 - r;
            for (var c = 0; c < polyominoColumns; ++c)
            {
                var tmp = polyomino[topRow, c];
                polyomino[topRow, c] = polyomino[bottomRow, c];
                polyomino[bottomRow, c] = tmp;
            }
        }
    }
}
