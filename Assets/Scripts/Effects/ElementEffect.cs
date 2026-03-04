using UnityEngine;

/// <summary>
/// Base class for all element effects.
/// Inherit from this class to create a new element behavior.
/// </summary>
public abstract class ElementEffect : ScriptableObject
{
    /// <summary>
    /// Executes the element's special effect on the board.
    /// </summary>
    /// <param name="board">The main game board.</param>
    /// <param name="position">The position on the board where the effect is triggered.</param>
    public abstract void ExecuteEffect(Board board, Vector2Int position);
}
