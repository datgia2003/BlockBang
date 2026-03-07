using UnityEngine;

[CreateAssetMenu(fileName = "NewElementData", menuName = "BlockBlast/Element Data")]
public class ElementData : ScriptableObject
{
    [Tooltip("The type of the element.")]
    public Element ElementType;

    [Header("Sprites")]
    [Tooltip("Sprite hiển thị khi cell ở trạng thái bình thường.")]
    public Sprite NormalSprite;

    [Tooltip("Sprite hiển thị khi cell đang được highlight (sắp xóa hàng).")]
    public Sprite HighlightSprite;

    [Header("Visuals")]
    [Tooltip("Tint color được áp lên sprite (giữ White nếu không muốn tô màu).")]
    public Color ElementColor = Color.white;

    [Header("Gameplay")]
    [Tooltip("The base chance (0-1) for this element to be chosen. The actual probability will be normalized against the sum of all element chances.")]
    [Range(0f, 1f)]
    public float SpawnChance = 0.1f;

    [Tooltip("The special effect to trigger when this element is cleared.")]
    public ElementEffect Effect;
}
