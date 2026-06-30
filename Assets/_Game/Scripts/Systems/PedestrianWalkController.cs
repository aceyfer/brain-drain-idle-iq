using UnityEngine;

namespace BrainDrain.Systems
{
    /// <summary>
    /// Per-pedestrian world-space movement: walks right-to-left at a random speed via
    /// transform.Translate (no physics), wrapping back offscreen-right at a random Y within
    /// the configured street bounds on exit. SetStage swaps the SpriteRenderer's sprite from a
    /// 6-entry pool (stage 1-6 maps to array index 0-5).
    /// </summary>
    public sealed class PedestrianWalkController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float minSpeed = 30f;
        [SerializeField] private float maxSpeed = 80f;

        [Header("Bounds")]
        [SerializeField] private float leftBoundX = -10f;
        [SerializeField] private float rightBoundX = 10f;
        [SerializeField] private float streetMinY = -2f;
        [SerializeField] private float streetMaxY = 2f;

        [Header("Stage Sprites")]
        [Tooltip("Indexed 0-5, matching CurrentStage 1-6.")]
        [SerializeField] private Sprite[] stageSprites = new Sprite[6];
        [SerializeField] private SpriteRenderer spriteRenderer;

        private float speed;
        private PedestrianWobble wobble;

        /// <summary>Current stage, 1-6. Set via SetStage, which also swaps the sprite.</summary>
        public int CurrentStage { get; private set; } = 1;

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            speed = Random.Range(minSpeed, maxSpeed);
            SetStage(CurrentStage);
        }

        private void Start()
        {
            wobble = GetComponent<PedestrianWobble>();
        }

        private void Update()
        {
            transform.Translate(Vector3.left * speed * Time.deltaTime, Space.World);

            if (transform.position.x < leftBoundX)
            {
                Respawn();
            }
        }

        private void Respawn()
        {
            float randomY = Random.Range(streetMinY, streetMaxY);
            transform.position = new Vector3(rightBoundX, randomY, transform.position.z);
            speed = Random.Range(minSpeed, maxSpeed);

            if (wobble != null)
            {
                wobble.ResetBaseY();
            }
        }

        public void SetStage(int stage)
        {
            CurrentStage = Mathf.Clamp(stage, 1, 6);

            int spriteIndex = CurrentStage - 1;
            if (spriteRenderer != null && stageSprites != null && spriteIndex < stageSprites.Length)
            {
                spriteRenderer.sprite = stageSprites[spriteIndex];
            }
        }
    }
}
