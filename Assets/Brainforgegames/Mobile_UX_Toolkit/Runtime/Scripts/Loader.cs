using TMPro;
using UnityEngine;
namespace BrainforgeGames.MobileUXToolkit
{
    public enum TextLocation
    {
        Middle,
        Bottom
    }

    public class Loader : MonoBehaviour
    {
        public static Loader Instance;
        [SerializeField] GameObject loadingPanel;
        [SerializeField] TextMeshProUGUI customTextMiddle;
        [SerializeField] TextMeshProUGUI customTextBottom;
        static GameObject staticLoadingPanel;
        static TextMeshProUGUI staticCustomText;
        static TextMeshProUGUI staticMiddleText;

        void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);

            staticLoadingPanel = loadingPanel;
            staticMiddleText = staticCustomText = customTextMiddle;
        }

        public static void Show()
        {
            if (staticLoadingPanel == null)
            {
                Debug.LogWarning("No Loading Panel Found");
                return;
            }
            staticMiddleText.text = "Loading...";
            staticMiddleText.gameObject.SetActive(true);
            staticLoadingPanel.SetActive(true);
        }

        static void ShowCustom()
        {
            if (staticLoadingPanel == null)
            {
                Debug.LogWarning("No Loading Panel Found");
                return;
            }
            staticLoadingPanel.SetActive(true);
        }

        public static void Hide()
        {
            if (staticLoadingPanel == null)
            {
                Debug.LogWarning("No Loading Panel Found");
                return;
            }
            staticLoadingPanel.SetActive(false);
            staticCustomText?.gameObject.SetActive(false);
        }

        public static bool IsVisible()
        {
            return staticLoadingPanel.activeInHierarchy;
        }

        public void SetCustomMessageAndShow(string customMessage, TextLocation location)
        {
            customTextMiddle?.gameObject.SetActive(false);
            customTextBottom?.gameObject.SetActive(false);
            staticCustomText = location == TextLocation.Middle ? customTextMiddle : customTextBottom;
            if (staticCustomText != null)
            {
                staticCustomText.text = customMessage;
                staticCustomText.gameObject.SetActive(true);
            }
            ShowCustom();
        }
    }
}
