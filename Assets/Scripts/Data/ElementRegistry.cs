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
    /// Chooses a random element based on their spawn chances.
    /// The roll is against 1.0, so if the sum of all chances is less than 1.0,
    /// there's a possibility of returning Element.Normal.
    /// </summary>
    public Element ChooseRandomElement()
    {
        Initialize();

        // Roll a random value between 0.0 and 1.0
        float roll = Random.value;
        float cumulativeChance = 0f;

        foreach (var elementData in Elements)
        {
            // We only consider special elements for random selection
            if (elementData.ElementType != Element.Normal)
            {
                cumulativeChance += elementData.SpawnChance;
                if (roll < cumulativeChance)
                {
                    return elementData.ElementType;
                }
            }
        }

        // If the roll is higher than the sum of all spawn chances, no special element is chosen.
        return Element.Normal;
    }
}
