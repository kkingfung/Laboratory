using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Laboratory.Gameplay.Inventory
{
    /// <summary>
    /// Displays item details in a popup UI.
    /// </summary>
    public class ItemDetailsPopup : MonoBehaviour
    {
        #region Fields

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI itemNameText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI rarityText;
        [SerializeField] private TextMeshProUGUI valueText;
        [SerializeField] private Transform statsContainer;
        [SerializeField] private StatEntryUI statEntryPrefab;

        #endregion

        #region Unity Override Methods

        private void Awake()
        {
            Hide();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Shows the popup with the given item data.
        /// </summary>
        public void Show(ItemData item)
        {
            if (item == null) return;

            itemNameText.text = item.ItemName;
            descriptionText.text = item.Description;
            iconImage.sprite = item.Icon;
            rarityText.text = GetRarityText(item.Rarity);
            valueText.text = item.Value.ToString();

            // Clear old entries (consider pooling for better FPS)
            for (int i = statsContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(statsContainer.GetChild(i).gameObject);
            }

            foreach (var stat in item.Stats)
            {
                var entry = Instantiate(statEntryPrefab, statsContainer);
                entry.SetData(stat.StatName, stat.StatValue, stat.StatIcon);
            }

            gameObject.SetActive(true);
        }

        /// <summary>
        /// Hides the popup.
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        #endregion

        #region Private Methods

        private string GetRarityText(int rarity)
        {
            return rarity switch
            {
                0 => "Common",
                1 => "Uncommon",
                2 => "Rare",
                3 => "Epic",
                4 => "Legendary",
                _ => "Unknown"
            };
        }

        #endregion
    }

    #region Inner Classes, Enums

    /// <summary>
    /// UI component for a single stat entry.
    /// </summary>
    [System.Serializable]
    public class StatEntryUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI statNameText;
        [SerializeField] private TextMeshProUGUI statValueText;
        [SerializeField] private Image statIconImage;

        public void SetData(string statName, string statValue, Sprite statIcon)
        {
            statNameText.text = statName;
            statValueText.text = statValue;
            statIconImage.sprite = statIcon;
        }
    }
    
    #endregion
}
