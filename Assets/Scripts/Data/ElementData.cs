using UnityEngine;

[CreateAssetMenu(fileName = "NewElementData", menuName = "BlockBlast/Element Data")]
public class ElementData : ScriptableObject
{
    [Tooltip("The type of the element.")]
    public Element ElementType;

    [Tooltip("The color of the element when it appears on a cell.")]
    public Color ElementColor = Color.white;

    [Tooltip("The base chance (0-1) for this element to be chosen. The actual probability will be normalized against the sum of all element chances.")]
    [Range(0f, 1f)]
    public float SpawnChance = 0.1f;

    [Tooltip("The special effect to trigger when this element is cleared.")]
    public ElementEffect Effect;
}
