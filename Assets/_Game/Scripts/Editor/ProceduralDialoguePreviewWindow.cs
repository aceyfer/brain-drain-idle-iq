using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using BrainDrain.Systems;

namespace BrainDrain.EditorTools
{
    /// <summary>
    /// PROCEDURAL_DIALOGUE_SPEC.md "Tooling": choose a stage and channel, dump 50 resolved
    /// lines from the procedural pool. Reloads word banks/templates from Resources fresh each
    /// time the window opens or the button is pressed, so edits to the JSON show up without an
    /// Editor restart.
    /// </summary>
    public sealed class ProceduralDialoguePreviewWindow : EditorWindow
    {
        private DialogueChannel channel = DialogueChannel.COGS;
        private int stage;
        private readonly List<string> results = new();
        private Vector2 scrollPosition;

        [MenuItem("BrainDrain/Procedural Dialogue/Preview Window")]
        public static void Open()
        {
            GetWindow<ProceduralDialoguePreviewWindow>("Procedural Dialogue Preview");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Procedural Dialogue Preview", EditorStyles.boldLabel);

            channel = (DialogueChannel)EditorGUILayout.EnumPopup("Channel", channel);
            stage = EditorGUILayout.IntSlider("Stage", stage, 0, RestorationStageBands.StageCount - 1);

            if (GUILayout.Button("Resolve 50 lines"))
            {
                ResolveFifty();
            }

            EditorGUILayout.Space();
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            for (int i = 0; i < results.Count; i++)
            {
                EditorGUILayout.LabelField($"{i + 1}. {results[i]}", EditorStyles.wordWrappedLabel);
            }
            EditorGUILayout.EndScrollView();
        }

        private void ResolveFifty()
        {
            var resolver = new ProceduralDialogueResolver(ProceduralDialogueLoader.LoadWordBanks(), ProceduralDialogueLoader.LoadTemplates());

            results.Clear();
            for (int i = 0; i < 50; i++)
            {
                results.Add(resolver.ResolvePreview(channel, stage));
            }
        }
    }
}
