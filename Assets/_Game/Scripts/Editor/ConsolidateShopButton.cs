#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

namespace BrainDrain.EditorTools
{
    /// <summary>
    /// Phase 1 shop consolidation: collapses two HUD shop buttons (BP SHOP + $ SHOP) into one.
    /// Relabels ShopButton to "SHOP" and hides CashShopButton in the hierarchy (not destroyed —
    /// Phase 2 wires it as the $ tab inside the unified panel). Idempotent on re-run.
    /// Run via BrainDrain > Testing > Consolidate Shop Button.
    /// </summary>
    public static class ConsolidateShopButton
    {
        [MenuItem("BrainDrain/Testing/Consolidate Shop Button")]
        public static void Run()
        {
            if (EditorToolGuard.BlockedByPlayMode("ConsolidateShopButton.Run")) return;
            bool anyChange = false;

            // 1. Relabel ShopButton → "SHOP" via SerializedObject so m_text is written directly
            //    to YAML. Undo.RecordObject + label.text = ... is unreliable for TMP in editor
            //    tools: the text setter does internal mesh work but doesn't flush m_text through
            //    the Unity serialization path reliably.
            GameObject shopButton = FindByName("ShopButton");
            if (shopButton == null)
            {
                Debug.LogWarning("[ConsolidateShopButton] 'ShopButton' not found in scene. " +
                                 "Run 'BrainDrain > Fix HUD Layout (Mobile Overhaul)' first to create it.");
            }
            else
            {
                TextMeshProUGUI label = shopButton.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label == null)
                {
                    Debug.LogWarning("[ConsolidateShopButton] ShopButton has no TextMeshProUGUI child — label not updated.");
                }
                else
                {
                    SerializedObject so = new SerializedObject(label);
                    so.Update();
                    SerializedProperty textProp = so.FindProperty("m_text");
                    if (textProp == null)
                    {
                        Debug.LogWarning("[ConsolidateShopButton] m_text property not found on ShopButton TMP — label not updated.");
                    }
                    else if (textProp.stringValue != "SHOP")
                    {
                        textProp.stringValue = "SHOP";
                        so.ApplyModifiedProperties();
                        EditorUtility.SetDirty(label);
                        anyChange = true;
                        Debug.Log("[ConsolidateShopButton] ShopButton label set to 'SHOP'.");
                    }
                    else
                    {
                        Debug.Log("[ConsolidateShopButton] ShopButton already labelled 'SHOP' — no change.");
                    }
                }
            }

            // 2. Hide CashShopButton (kept in hierarchy intact for Phase 2) via SerializedObject
            //    so m_IsActive is written correctly to YAML.
            GameObject cashShopButton = FindByName("CashShopButton");
            if (cashShopButton == null)
            {
                Debug.Log("[ConsolidateShopButton] 'CashShopButton' not found in scene — skipped.");
            }
            else
            {
                SerializedObject so = new SerializedObject(cashShopButton);
                so.Update();
                SerializedProperty activeProp = so.FindProperty("m_IsActive");
                if (activeProp == null)
                {
                    Debug.LogWarning("[ConsolidateShopButton] m_IsActive property not found on CashShopButton.");
                }
                else if (activeProp.boolValue)
                {
                    activeProp.boolValue = false;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(cashShopButton);
                    anyChange = true;
                    Debug.Log("[ConsolidateShopButton] CashShopButton hidden (kept in hierarchy for Phase 2).");
                }
                else
                {
                    Debug.Log("[ConsolidateShopButton] CashShopButton already inactive — no change.");
                }
            }

            if (anyChange)
            {
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
                Debug.Log("[ConsolidateShopButton] Scene saved. Phase 1 complete: single 'SHOP' button in HUD.");
            }
            else
            {
                Debug.Log("[ConsolidateShopButton] Already up to date — no scene changes needed.");
            }
        }

        private static GameObject FindByName(string name)
        {
            GameObject found = GameObject.Find(name);
            if (found != null) return found;

            foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (go.name == name) return go;
            }
            return null;
        }
    }
}
#endif
