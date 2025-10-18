using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Laboratory.Core.Equipment.Types;
using Laboratory.Core.Equipment.Systems;

namespace Laboratory.Core.Equipment.Authoring
{
    /// <summary>
    /// MonoBehaviour authoring for equipment shops and vendors
    /// </summary>
    public class EquipmentShopAuthoring : MonoBehaviour
    {
        [Header("Shop Configuration")]
        public EquipmentType[] availableEquipment;
        [Range(1, 10)] public int shopLevel = 1;
        public bool sellsAllRarities = false;
        public float restockInterval = 300f; // 5 minutes

        [Header("Pricing")]
        public int baseCost = 100;
        public float rarityPriceMultiplier = 2.0f;

        [ContextMenu("Stock Shop")]
        public void StockShop()
        {
            var craftingSystem = World.DefaultGameObjectInjectionWorld?.GetExistingSystemManaged<EquipmentCraftingSystem>();
            if (craftingSystem == null) return;

            foreach (var equipType in availableEquipment)
            {
                var maxRarity = sellsAllRarities ? EquipmentRarity.Mythic : (EquipmentRarity)math.min((int)EquipmentRarity.Epic, shopLevel);
                var rarity = (EquipmentRarity)UnityEngine.Random.Range(0, (int)maxRarity + 1);

                var equipment = craftingSystem.CreateEquipment(equipType, rarity);
                Debug.Log($"Stocked {rarity} {equipType} in shop");
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 3f);
            Gizmos.DrawIcon(transform.position + Vector3.up * 2f, "Equipment_Shop_Icon");
        }
    }
}