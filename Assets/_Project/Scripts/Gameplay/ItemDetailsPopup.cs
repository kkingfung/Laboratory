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
        [SerializeField] private GameObject statEntryPrefab;

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

            foreach (Transform child in statsContainer)
                Destroy(child.gameObject);

            foreach (var stat in item.Stats)
            {
                var entry = Instantiate(statEntryPrefab, statsContainer);
                var statNameText = entry.transform.Find("StatName")?.GetComponent<TextMeshProUGUI>();
                var statValueText = entry.transform.Find("StatValue")?.GetComponent<TextMeshProUGUI>();
                var statIconImage = entry.transform.Find("StatIcon")?.GetComponent<Image>();

                if (statNameText != null) statNameText.text = stat.StatName;
                if (statValueText != null) statValueText.text = stat.StatValue;
                if (statIconImage != null) statIconImage.sprite = stat.StatIcon;
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

        #region Inner Classes, Enums

        // No inner classes or enums currently.

        #endregion
    }
}
