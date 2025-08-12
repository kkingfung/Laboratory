using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MessagePipe;
using System.Collections.Generic;

namespace Game.Inventory
{
    public class ItemDetailsPopup : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject popupRoot;
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI rarityText;
        [SerializeField] private TextMeshProUGUI valueText;

        [Header("Stats UI")]
        [SerializeField] private Transform statsContainer;
        [SerializeField] private GameObject statLinePrefab; // prefab with icon + 2 TMP_Text

        [Header("Rarity Colors")]
        [SerializeField] private Color commonColor = Color.gray;
        [SerializeField] private Color uncommonColor = Color.green;
        [SerializeField] private Color rareColor = Color.blue;
        [SerializeField] private Color epicColor = new Color(0.64f, 0f, 0.92f); // purple
        [SerializeField] private Color legendaryColor = new Color(1f, 0.5f, 0f); // orange

        private List<GameObject> _spawnedStatLines = new();

        private ISubscriber<ItemSelectedEvent> _itemSelectedSubscriber;
        private IDisposable _itemSelectedSub;

        [Inject]
        public void Construct(ISubscriber<ItemSelectedEvent> itemSelectedSubscriber)
        {
            _itemSelectedSubscriber = itemSelectedSubscriber;
        }

        private void OnEnable()
        {
            popupRoot.SetActive(false);
            _itemSelectedSub = _itemSelectedSubscriber.Subscribe(OnItemSelected);
        }

        private void OnDisable()
        {
            _itemSelectedSub?.Dispose();
        }

        private void OnItemSelected(ItemSelectedEvent e)
        {
            ClearStatLines();

            if (e.Item == null)
            {
                popupRoot.SetActive(false);
                return;
            }

            iconImage.sprite = e.Item.Icon;
            nameText.text = e.Item.ItemName;
            rarityText.text = GetRarityName(e.Item.Rarity);
            rarityText.color = GetRarityColor(e.Item.Rarity);
            descriptionText.text = e.Item.Description;
            valueText.text = $"{e.Item.Value} gold";

            foreach (var stat in e.Item.Stats)
            {
                var line = Instantiate(statLinePrefab, statsContainer);

                var iconImage = line.transform.Find("StatIcon")?.GetComponent<Image>();
                var texts = line.GetComponentsInChildren<TextMeshProUGUI>();

                if (iconImage != null)
                {
                    iconImage.sprite = stat.StatIcon;
                    iconImage.gameObject.SetActive(stat.StatIcon != null);
                }

                if (texts.Length >= 2)
                {
                    texts[0].text = stat.StatName;
                    texts[1].text = stat.StatValue;
                }

                _spawnedStatLines.Add(line);
            }

            popupRoot.SetActive(true);
        }

        private void ClearStatLines()
        {
            foreach (var line in _spawnedStatLines)
            {
                Destroy(line);
            }
            _spawnedStatLines.Clear();
        }

        private string GetRarityName(int rarity)
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

        private Color GetRarityColor(int rarity)
        {
            return rarity switch
            {
                0 => commonColor,
                1 => uncommonColor,
                2 => rareColor,
                3 => epicColor,
                4 => legendaryColor,
                _ => Color.white
            };
        }
    }
}
