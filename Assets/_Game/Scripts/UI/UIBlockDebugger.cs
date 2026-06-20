using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BrainDrain.UI
{
    /// <summary>
    /// Troubleshooting helper that checks for a left mouse click and rays all UI elements
    /// underneath the pointer, logging the absolute top-most GameObject intercepting it.
    /// </summary>
    public class UIBlockDebugger : MonoBehaviour
    {
        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (EventSystem.current == null)
                {
                    return;
                }

                PointerEventData pointerData = new PointerEventData(EventSystem.current)
                {
                    position = Input.mousePosition
                };

                List<RaycastResult> results = new List<RaycastResult>();
                EventSystem.current.RaycastAll(pointerData, results);

                if (results.Count > 0)
                {
                    Debug.Log($"[UIBlockDebugger] Top-most UI element: '{results[0].gameObject.name}' (GameObject path: {GetGameObjectPath(results[0].gameObject.transform)})", results[0].gameObject);
                }
                else
                {
                    Debug.Log("[UIBlockDebugger] Clicked, but no UI elements were hit.");
                }
            }
        }

        private string GetGameObjectPath(Transform t)
        {
            string path = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }
            return path;
        }
    }
}