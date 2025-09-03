using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class WorldButtonUI : MonoBehaviour
{
    [Header("UI Komponensek")]
    [SerializeField] private TextMeshProUGUI worldNameText;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private Image worldIconImage;
    [SerializeField] private Button button;
    public void Setup(WorldDefinition world, GameData progressData, System.Action onClickAction)
    {
        worldNameText.text = world.worldName;
        worldIconImage.sprite = world.worldIcon;

        if (progressData != null && world.levels.Count > 0)
        {
            // Megszámoljuk, hány pálya van teljesítve az adott világban.
            int completedCount = world.levels.Count(level => progressData.completedLevelIds.Contains(level.levelId));
            progressText.text = $"{completedCount} / {world.levels.Count}";
            progressText.gameObject.SetActive(true);
        }
        else
        {
            // Ha nincs mentés, elrejtjük a haladás szövegét.
            progressText.gameObject.SetActive(false);
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClickAction?.Invoke());
    }
}