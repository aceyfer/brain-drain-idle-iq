using UnityEngine;

namespace BrainDrain.Core
{
    /// <summary>
    /// RETIRED BY DESIGN (Aceyfer, Jul 13 2026): the rank-figure diorama (Outcast ->
    /// President cast, rendered by the Diorama Camera compositing over the main view)
    /// served no purpose and read as giant "pedestrians" parading over the game whenever
    /// rank flapped mid-crossfade (the §18 fourth population). Presentation state is
    /// code-owned (Bible §8): this script now only asserts its figure renderers off.
    /// Scene edits are embargoed pending the post-embargo cleanup pass, so the
    /// _DioramaContainer object, its five figure mounts, this script, and the Diorama
    /// Camera are ledgered in §19 for physical removal. The world-restoration stage
    /// BACKDROPS are owned by WorldRestorationManager and are NOT affected.
    /// </summary>
    public sealed class DioramaManager : MonoBehaviour
    {
        [Header("Diorama References")]
        [SerializeField] private GameObject[] dioramaObjects;

        public GameObject[] DioramaObjects
        {
            get => dioramaObjects;
            set => dioramaObjects = value;
        }

        private void Start()
        {
            if (dioramaObjects == null)
            {
                return;
            }

            for (int i = 0; i < dioramaObjects.Length; i++)
            {
                if (dioramaObjects[i] == null)
                {
                    continue;
                }

                SpriteRenderer sr = dioramaObjects[i].GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.enabled = false;
                }
            }
        }
    }
}
