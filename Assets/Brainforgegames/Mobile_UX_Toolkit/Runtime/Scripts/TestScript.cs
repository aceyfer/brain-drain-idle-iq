using UnityEngine;
namespace BrainforgeGames.MobileUXToolkit
{
    public class TestScript : MonoBehaviour
    {

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                Toast.Show("This is a toast message");
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                Toast.Show("This is another toast message\n This is a long toast message");
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                Loader.Show();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                Loader.Instance.SetCustomMessageAndShow("Custom Message here", TextLocation.Bottom);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                Loader.Hide();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha6))
            {
                Popup.Show("Title", "This is description", "Close", () => { });
            }
            else if (Input.GetKeyDown(KeyCode.Alpha7))
            {
                Popup.ShowAuto("Title", "Description here");
            }
            else if (Input.GetKeyDown(KeyCode.Alpha8))
            {
                Debug.LogError("Checking internet");
                InternetAvailabilityChecker.CheckNow(isOnline =>
                {
                    Debug.LogError("Internet Online: " + isOnline);
                });
            }
            else if (Input.GetKeyDown(KeyCode.Alpha9))
            {
                ScreenTransition.CloseCircle();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha0))
            {
                ScreenTransition.OpenCircle();
            }
        }
    }
}