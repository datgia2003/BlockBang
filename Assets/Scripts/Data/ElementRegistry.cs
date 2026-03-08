using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "ElementRegistry", menuName = "BlockBlast/Element Registry")]
public class ElementRegistry : ScriptableObject
{
    [Tooltip("List of all element data objects in the game. The 'Normal' element should not be in this list.")]
    public List<ElementData> Elements;

    private float totalSpawnChance;
    private Dictionary<Element, ElementData> elementMap;

    private void Initialize()
    {
        // If the map is already created, we don't need to do anything.
        // This is safer than a boolean flag across domain reloads.
        if (elementMap != null) return;

        elementMap = new Dictionary<Element, ElementData>();
        totalSpawnChance = 0f;

        // Guard against the list not being assigned in the inspector.
        if (Elements == null) return;

        foreach (var elementData in Elements)
        {
            if (elementData != null && elementData.ElementType != Element.Normal)
            {
                totalSpawnChance += elementData.SpawnChance;
                elementMap[elementData.ElementType] = elementData;
            }
        }
    }

    /// <summary>
    /// Gets the ElementData for a specific element type.
    /// </summary>
    public ElementData GetElementData(Element elementType)
    {
        Initialize();
        elementMap.TryGetValue(elementType, out var data);
        return data; // Returns null if not found
    }

    /// <summary>
    /// Chooses a random element based on their spawn chances,
    /// adjusted by active buffs (Lightning/Fire rate up, Ice rate down).
    /// </summary>
    public Element ChooseRandomElement()
    {
        Initialize();

        // Gather buff modifiers
        float lightningBonus = BuffManager.Instance?.LightningRateBonus ?? 0f;
        float fireBonus      = BuffManager.Instance?.FireRateBonus      ?? 0f;
        float iceReduction   = BuffManager.Instance?.IceRateReduction   ?? 0f;

        float roll = Random.value;
        float cumulativeChance = 0f;

        foreach (var elementData in Elements)
        {
            if (elementData.ElementType == Element.Normal) continue;

            float chance = elementData.SpawnChance;

            // Apply buffs per element type
            if (elementData.ElementType == Element.Lightning)
                chance = Mathf.Max(0f, chance + lightningBonus);
            else if (elementData.ElementType == Element.Fire)
                chance = Mathf.Max(0f, chance + fireBonus);
            else if (elementData.ElementType == Element.Ice)
                chance = Mathf.Max(0f, chance - iceReduction);

            cumulativeChance += chance;
            if (roll < cumulativeChance)
                return elementData.ElementType;
        }

        // If the roll exceeds cumulative chance → Normal element
        return Element.Normal;
    }
}
