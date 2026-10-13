using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using BrainDrain.Core;

namespace BrainDrain.Systems
{
    /// <summary>
    /// Ambient comedic beat, added 2026-09-22 per Aceyfer's direction ("keeping it funny";
    /// visual reference given was the rapture beam from "This Is the End" -- used here only as a
    /// tone/style reference for a bright descending light shaft, not a reproduction of that
    /// film's actual footage, art, or any copyrighted design). Every ~10-20 minutes, steals a
    /// random on-screen pedestrian from BackgroundPedestrianManager, spawns a UFO + descending
    /// beam above it, and has the pedestrian cartwheel (fast continuous spin, per Aceyfer's own
    /// words) while shrinking and rising into the beam before the UFO flies off.
    ///
    /// Purely cosmetic -- no currency/IQ/economy effect, matching how §61's weather system was
    /// scoped ("no gameplay effect") and this feature's own scoping doc's recommendation
    /// (Assets/Plans/pedestrian-polish-and-abduction-event-scoping.md, open question 4).
    ///
    /// Deliberately NOT wired through RandomEventManager/BrainRotEventData -- that pool fires
    /// every 90-180s and shows a modal popup requiring an accept/decline choice. This is ambient
    /// background spectacle on its own much longer timer with no popup and no player choice,
    /// closer in spirit to WeatherManager's periodic rain bursts (§61) than to a chaos event.
    ///
    /// UFO and beam art are procedurally drawn at runtime (BuildUfoSprite/BuildBeamSprite below)
    /// -- no image-gen tool is available in this session (same constraint noted throughout this
    /// project's other art tasks, e.g. §55d's mesh-overlay spec, §60's backdrop art), and a
    /// simple flying-saucer silhouette is geometric enough to read fine procedurally, matching
    /// ArtExpansionTool.cs's existing precedent for code-drawn borders/gradients. Swapping in
    /// real generated art later (Leonardo or otherwise) only means replacing what
    /// BuildUfoSprite/BuildBeamSprite return -- timing, motion, and wiring are unaffected.
    ///
    /// UNVERIFIED -- written from a session with no Unity Editor/compiler access (file-bridge
    /// only). No new packages or unfamiliar APIs are used (plain UnityEngine.UI/Coroutine code
    /// matching BackgroundPedestrianManager's and WeatherManager's existing patterns exactly), so
    /// the risk profile here is much lower than §12's UGS work -- but this still needs a real
    /// compile + Play Mode pass before being trusted. See Editor/UfoAbductionWireFix.cs for the
    /// one-time scene setup this depends on (creates the host GameObject and wires
    /// pedestrianManager -- this component does nothing useful if that field is left unassigned).
    ///
    /// Follow-up, 2026-09-22 (Aceyfer, first real Play Mode look): "the ufo is going down to the
    /// human out of nowhere" -- the UFO used to cut straight from off-screen to its hover point
    /// with nothing announcing it first. Added a brief pulsing ground-glow telegraph at the
    /// target pedestrian's feet (warningLeadSeconds) before the UFO spawns, plus a fade-in on the
    /// UFO itself during descent, so the beat reads as "something's about to happen" instead of
    /// popping into frame. (Separately, Aceyfer also reported the effect being hard to see in that
    /// same test pass -- that was the Editor-only DebugCheatPanel's placeholder buttons sitting on
    /// top of the game view, not this component; see DebugCheats.cs/TestingMenuShortcuts.cs, close
    /// that panel before watching a forced test.)
    ///
    /// Second follow-up, same day, from Aceyfer's actual look at the telegraph fix: "its not using
    /// a beam like discussed. it came down popping up halfway on the screen, then the cartwheel
    /// happens like its getting flushed down a toilet swinging around the ship like a monkey." Two
    /// real bugs, both from grounding this against the wrong RectTransform originally:
    /// 1. Everything was parented under pedestrianManager.ContainerRect, which
    ///    BackgroundPedestrianManager.cs's own doc comment already says is only "the UI street
    ///    band behind the player character" -- a short strip, not the full screen. Using its
    ///    rect.height as if it were screen height put the UFO's "off-screen" spawn point only
    ///    slightly above the middle of that short band (still on-screen), and made the beam's
    ///    height (ufoY - groundY) tiny. Fixed two ways: (a) reparenting the stolen pedestrian into
    ///    a genuinely full-screen "AbductionOverlay" RectTransform (fullScreenOverlay below,
    ///    anchors 0-1, same technique WeatherSystemWireFix already uses for AtmosphereOverlay) via
    ///    SetParent(fullScreenOverlay, true) -- worldPositionStays keeps it visually in the same
    ///    spot while Unity recomputes anchoredPosition against the new, correctly-sized parent; and
    ///    (b) the hover-height formula itself was never "near the top" even in full-screen space
    ///    (half of rect.height is screen *center*) -- replaced with ufoHoverHeightFraction (a real
    ///    fraction of screenHeight) and a spawn point genuinely above screenHeight, not half of it.
    /// 2. Pedestrians spawn with pivot (0.5, 0) -- feet-anchored, so they stand on the street
    ///    baseline correctly (BackgroundPedestrianManager.SpawnPedestrian). Rotating 1080 degrees
    ///    around a feet pivot swings the entire sprite in a circle around its own feet instead of
    ///    spinning in place -- exactly the "swinging around like a monkey" Aceyfer described.
    ///    Fixed with RecenterPivot() below, which shifts the pivot to (0.5, 0.5) with a matching
    ///    anchoredPosition correction so the sprite doesn't jump, right after the reparent.
    ///
    /// Third follow-up, 2026-09-23, after the fullScreenOverlay fix was actually tested: "the beam
    /// works and the player abduction happens but no ufo visable." Root cause: sibling order, not
    /// math. Editor/UfoAbductionWireFix.cs placed AbductionOverlay right after AtmosphereOverlay --
    /// deliberately still *below* CustomSafeArea (the HUD/buttons layer) in the Canvas's child
    /// order, on the assumption that only the low-on-screen beam/pedestrian mattered. But the UFO
    /// hovers at ufoHoverHeightFraction (0.78 -- high on screen), right behind the game's opaque top
    /// HUD bar (rank/stage readout), which lives inside CustomSafeArea and renders in front of
    /// anything earlier in the sibling order. The beam and cartwheeling pedestrian sit low enough to
    /// clear that bar, so they were visible; the UFO itself never could be. Fixed by moving
    /// AbductionOverlay to sit right after CustomSafeArea instead (still below
    /// CelebrationFlashOverlay, which is meant to cover literally everything at a rare climax
    /// moment -- these two aren't expected to ever overlap). Also set the stolen pedestrian's Image
    /// raycastTarget to false right after reparenting: it now renders in front of CustomSafeArea's
    /// buttons for the ~4s sequence, and shouldn't be able to intercept clicks meant for them.
    ///
    /// Fourth follow-up, 2026-09-23, after the sibling-order fix confirmed the UFO/beam/spin all
    /// worked: "the ped pauses in place before spinning up. i think the ped should continue the
    /// walk animation like walk in place til they spin up in the sky." Root cause: correct, not a
    /// bug -- TryStealRandomPedestrianForEffect deliberately cancels the pedestrian's own
    /// MoveRoutine coroutine (so it can't fight this sequence's own motion for the same
    /// RectTransform), which is exactly what froze it solid at the moment of the steal. Fixed by
    /// having AbductionSequence fake MoveRoutine's own footstep bounce/tilt in place (X pinned,
    /// same amplitude/frequency math, pulled live from BackgroundPedestrianManager rather than a
    /// second hardcoded copy) through the warning telegraph and UFO descent, via
    /// ApplyWalkInPlaceBounce below -- so the pedestrian reads as still obliviously walking right
    /// up until the beam grabs them and the cartwheel takes over.
    /// </summary>
    public sealed class UfoAbductionController : MonoBehaviour
    {
        [Header("Dependencies")]
        [Tooltip("Wired by Editor/UfoAbductionWireFix.cs. Falls back to a scene search if left empty, but the wire-fix tool should always set this explicitly.")]
        [SerializeField] private BackgroundPedestrianManager pedestrianManager;
        [Tooltip("A genuinely full-screen RectTransform ('AbductionOverlay', anchors 0-1), wired by Editor/UfoAbductionWireFix.cs. Added 2026-09-22 -- pedestrianManager.ContainerRect is only the narrow pedestrian walking-band strip, not the full screen; using it directly was the original 'UFO pops up halfway on screen' bug.")]
        [SerializeField] private RectTransform fullScreenOverlay;

        [Header("Timing")]
        [Tooltip("Aceyfer's spec, 2026-09-22: 'every 10-20 mins'.")]
        [SerializeField] private float minIntervalSeconds = 600f;
        [SerializeField] private float maxIntervalSeconds = 1200f;

        [Header("Warning Telegraph")]
        [Tooltip("Added 2026-09-22 per Aceyfer's testing feedback ('out of nowhere') -- a brief pulsing ground glow at the target pedestrian's feet before the UFO shows up, so the eye is drawn there first.")]
        [SerializeField] private float warningLeadSeconds = 0.6f;
        [SerializeField] private float warningGlowSize = 70f;

        [Header("Sequence Timing (seconds)")]
        [Tooltip("Bumped from 1.2s to 1.6s alongside the warning telegraph (2026-09-22) -- gives the descent itself a little more room to read once the eye is already on the spot.")]
        [SerializeField] private float ufoDescendSeconds = 1.6f;
        [SerializeField] private float abductSeconds = 2.2f;
        [SerializeField] private float ufoDepartSeconds = 1.0f;

        [Header("Cartwheel")]
        [Tooltip("Total rotation applied over abductSeconds -- multiple full spins, per Aceyfer's 'spin like a cartwheel' direction.")]
        [SerializeField] private float cartwheelTotalDegrees = 1080f;

        [Header("Visuals")]
        [SerializeField] private Vector2 ufoSize = new Vector2(180f, 90f);
        [SerializeField] private float beamWidthAtUfo = 140f;
        [Tooltip("Added 2026-09-22 alongside the fullScreenOverlay fix: how high up the screen the UFO hovers, as a fraction of real screen height (0 = bottom, 1 = top). The old formula (half of whatever RectTransform was passed in) never meant 'near the top' even conceptually -- this one does.")]
        [SerializeField, Range(0.3f, 0.95f)] private float ufoHoverHeightFraction = 0.78f;

        private static Sprite cachedUfoSprite;
        private static Sprite cachedBeamSprite;
        private static Sprite cachedWarningGlowSprite;

        private float secondsUntilNextAbduction;
        private Coroutine sequenceRoutine;

        private static UfoAbductionController instance;

        /// <summary>Not self-bootstrapping (unlike RandomEventManager.Instance) -- this component
        /// depends on a serialized pedestrianManager reference that only
        /// Editor/UfoAbductionWireFix.cs sets, so auto-creating a bare GameObject here would just
        /// produce a broken instance. Null means the wire-fix tool hasn't been run yet in this
        /// scene, which DebugCheats.ForceUfoAbductionCooldownElapsed already accounts for.</summary>
        public static UfoAbductionController Instance => instance;

        private void Awake()
        {
            instance = this;
        }

        private void Start()
        {
            if (pedestrianManager == null)
            {
                pedestrianManager = FindAnyObjectByType<BackgroundPedestrianManager>();
                if (pedestrianManager == null)
                {
                    Debug.LogError("[UfoAbductionController] No BackgroundPedestrianManager found in the scene -- this component can never fire. Run BrainDrain > Fix UFO Abduction Event.", this);
                }
            }

            ScheduleNext();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnSecondTick -= HandleSecondTick;
                GameManager.Instance.OnSecondTick += HandleSecondTick;
            }
            else
            {
                Debug.LogError("[UfoAbductionController] GameManager.Instance is null; cannot subscribe to tick.", this);
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnSecondTick -= HandleSecondTick;
            }

            if (instance == this)
            {
                instance = null;
            }
        }

        private void ScheduleNext()
        {
            secondsUntilNextAbduction = Random.Range(minIntervalSeconds, maxIntervalSeconds);
        }

        private void HandleSecondTick()
        {
            secondsUntilNextAbduction -= 1f;
            if (secondsUntilNextAbduction > 0f)
            {
                return;
            }

            ScheduleNext();

            if (sequenceRoutine != null)
            {
                // Not expected at 10-20 min spacing vs. a ~4.4s sequence, but never stack two.
                return;
            }

            if (pedestrianManager == null || !pedestrianManager.TryStealRandomPedestrianForEffect(out RectTransform pedestrian))
            {
                // No pedestrian on screen right now -- skip silently. Matches this project's
                // established random-event convention (e.g. RandomEventManager deferring rather
                // than forcing an effect over an FTUE card) of never blocking or guessing when the
                // precondition for a nice-to-have beat just isn't met this cycle.
                return;
            }

            sequenceRoutine = StartCoroutine(AbductionSequence(pedestrian));
        }

        private IEnumerator AbductionSequence(RectTransform pedestrian)
        {
            RectTransform container = pedestrianManager != null ? pedestrianManager.ContainerRect : null;
            if (container == null || pedestrian == null || fullScreenOverlay == null)
            {
                if (pedestrian != null)
                {
                    Destroy(pedestrian.gameObject);
                }

                if (fullScreenOverlay == null)
                {
                    Debug.LogError("[UfoAbductionController] fullScreenOverlay is unassigned -- run BrainDrain > Fix UFO Abduction Event. Skipping this abduction.", this);
                }

                sequenceRoutine = null;
                yield break;
            }

            // Reparent the stolen pedestrian into the full-screen overlay -- worldPositionStays
            // (true) keeps it visually exactly where it was, while Unity recomputes anchoredPosition
            // against fullScreenOverlay's rect instead of the narrow PedestrianContainer's. See the
            // class doc comment's second follow-up for why this matters (2026-09-22).
            pedestrian.SetParent(fullScreenOverlay, true);
            RecenterPivot(pedestrian);

            // AbductionOverlay now renders in front of CustomSafeArea's HUD/buttons (2026-09-23 fix,
            // see class doc comment's third follow-up) so the UFO clears the top HUD bar. That means
            // this pedestrian could intercept clicks meant for those buttons for the sequence's ~4s
            // duration if left raycastable -- it never needs to be, it's purely decorative here.
            Image pedestrianImage = pedestrian.GetComponent<Image>();
            if (pedestrianImage != null)
            {
                pedestrianImage.raycastTarget = false;
            }

            float pedestrianX = pedestrian.anchoredPosition.x;
            float groundY = pedestrian.anchoredPosition.y;
            float screenHeight = fullScreenOverlay.rect.height;
            float ufoY = screenHeight * ufoHoverHeightFraction;

            // Walk-in-place hold (Aceyfer's testing feedback, 2026-09-23: "the ped pauses in place
            // before spinning up... should continue the walk animation like walk in place til they
            // spin up in the sky"). TryStealRandomPedestrianForEffect already cancelled this
            // pedestrian's MoveRoutine coroutine, which is what froze it solid the instant it got
            // stolen -- X pinned here (unlike real walking) but the same footstep bounce + tilt
            // keeps playing through the telegraph and descent, so the pedestrian reads as still
            // obliviously walking right up until the beam grabs them. Pulls BackgroundPedestrianManager's
            // live wobble tuning rather than a second hardcoded copy -- see that class's
            // WobbleVerticalAmplitude/WobbleRotationAmplitude/WobbleFrequency/CurrentWobbleMultiplier.
            float lookDirMultiplier = Mathf.Sign(pedestrian.localScale.x);
            if (lookDirMultiplier == 0f)
            {
                lookDirMultiplier = 1f;
            }
            float walkWobbleVerticalAmplitude = pedestrianManager.WobbleVerticalAmplitude * pedestrianManager.CurrentWobbleMultiplier;
            float walkWobbleRotationAmplitude = pedestrianManager.WobbleRotationAmplitude * pedestrianManager.CurrentWobbleMultiplier;
            float walkWobbleFrequency = pedestrianManager.WobbleFrequency;
            float walkInPlaceElapsed = Random.Range(0f, 10f); // matches MoveRoutine's own randomized phase offset

            // --- Warning telegraph: a brief pulsing ground glow at the target's feet, so the eye
            // is already on the spot before the UFO shows up (Aceyfer's testing feedback, 2026-09-22
            // -- previously this cut straight to the UFO with nothing announcing it first). ---
            RectTransform warningRt = CreateImage("AbductionWarning (Generated)", fullScreenOverlay, GetWarningGlowSprite(), Vector2.one * warningGlowSize);
            warningRt.anchoredPosition = new Vector2(pedestrianX, groundY);
            Image warningImage = warningRt.GetComponent<Image>();

            float t = 0f;
            while (t < warningLeadSeconds)
            {
                t += Time.deltaTime;
                walkInPlaceElapsed += Time.deltaTime;
                float p = Mathf.Clamp01(t / warningLeadSeconds);
                // Pulses twice while growing -- reads as "something's about to happen" rather than a static flash.
                float pulse = 0.5f + 0.5f * Mathf.Sin(p * Mathf.PI * 4f);
                warningImage.color = new Color(1f, 0.95f, 0.6f, Mathf.Lerp(0.15f, 0.85f, p) * pulse);
                warningRt.localScale = Vector3.one * Mathf.Lerp(0.6f, 1.3f, p);
                ApplyWalkInPlaceBounce(pedestrian, walkInPlaceElapsed, pedestrianX, groundY, lookDirMultiplier, walkWobbleVerticalAmplitude, walkWobbleRotationAmplitude, walkWobbleFrequency);
                yield return null;
            }
            Destroy(warningRt.gameObject);

            // --- UFO enters from off the top of the screen, fading in as it descends ---
            RectTransform ufoRt = CreateImage("AbductionUFO (Generated)", fullScreenOverlay, GetUfoSprite(), ufoSize);
            Vector2 ufoStart = new Vector2(pedestrianX, screenHeight + ufoSize.y); // genuinely above the top edge -- not visible until it descends below screenHeight
            ufoRt.anchoredPosition = ufoStart;
            Vector2 ufoTarget = new Vector2(pedestrianX, ufoY);
            Image ufoImage = ufoRt.GetComponent<Image>();
            ufoImage.color = new Color(1f, 1f, 1f, 0f);

            t = 0f;
            while (t < ufoDescendSeconds)
            {
                t += Time.deltaTime;
                walkInPlaceElapsed += Time.deltaTime;
                float p = Mathf.Clamp01(t / ufoDescendSeconds);
                ufoRt.anchoredPosition = Vector2.Lerp(ufoStart, ufoTarget, EaseOutCubic(p));
                // Fades in over the first third of the descent so it doesn't hard-pop into frame.
                ufoImage.color = new Color(1f, 1f, 1f, Mathf.Clamp01(p / 0.35f));
                ApplyWalkInPlaceBounce(pedestrian, walkInPlaceElapsed, pedestrianX, groundY, lookDirMultiplier, walkWobbleVerticalAmplitude, walkWobbleRotationAmplitude, walkWobbleFrequency);
                yield return null;
            }
            ufoRt.anchoredPosition = ufoTarget;
            ufoImage.color = Color.white;

            // Rise animation below starts from wherever the walk-in-place bounce actually left the
            // pedestrian, not the flat groundY baseline -- avoids a visible snap back down to
            // groundY the instant the cartwheel phase takes over.
            float riseStartY = pedestrian.anchoredPosition.y;

            // --- Beam appears, pedestrian cartwheels upward into it ---
            RectTransform beamRt = CreateImage("AbductionBeam (Generated)", fullScreenOverlay, GetBeamSprite(), new Vector2(beamWidthAtUfo, Mathf.Max(10f, ufoY - groundY)));
            beamRt.pivot = new Vector2(0.5f, 1f); // hangs down from the UFO's belly, not centered
            beamRt.anchoredPosition = new Vector2(pedestrianX, ufoY - (ufoSize.y * 0.15f));
            Image beamImage = beamRt.GetComponent<Image>();
            beamImage.color = new Color(1f, 1f, 1f, 0f);

            Vector3 pedestrianStartScale = pedestrian.localScale;
            float pedestrianStartRotation = pedestrian.localRotation.eulerAngles.z;
            Color pedestrianStartColor = pedestrianImage != null ? pedestrianImage.color : Color.white;

            t = 0f;
            while (t < abductSeconds)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / abductSeconds);

                // Beam fades in fast, holds, then fades out right at the end alongside the pedestrian.
                float beamAlpha = Mathf.Sin(Mathf.Clamp01(p / 0.15f) * Mathf.PI * 0.5f) * (1f - Mathf.Pow(p, 3f));
                beamImage.color = new Color(1f, 1f, 1f, beamAlpha);

                // "Spin like a cartwheel" -- fast continuous rotation, not the walk-cycle's gentle tilt.
                float rotation = pedestrianStartRotation + cartwheelTotalDegrees * p;
                pedestrian.localRotation = Quaternion.Euler(0f, 0f, rotation);

                // Rises from the ground into the beam, shrinking as it "gets sucked up". Starts
                // from riseStartY (wherever the walk-in-place bounce left it), not groundY.
                float risenY = Mathf.Lerp(riseStartY, ufoY - (ufoSize.y * 0.1f), EaseInCubic(p));
                pedestrian.anchoredPosition = new Vector2(pedestrianX, risenY);
                pedestrian.localScale = Vector3.Lerp(pedestrianStartScale, pedestrianStartScale * 0.1f, p);

                if (pedestrianImage != null)
                {
                    pedestrianImage.color = new Color(pedestrianStartColor.r, pedestrianStartColor.g, pedestrianStartColor.b, 1f - Mathf.Pow(p, 2f));
                }

                yield return null;
            }

            Destroy(pedestrian.gameObject);
            Destroy(beamRt.gameObject);

            // --- UFO exits (flies off sideways and slightly up, job done) ---
            t = 0f;
            Vector2 ufoExitStart = ufoRt.anchoredPosition;
            float exitDirection = Random.value > 0.5f ? 1f : -1f;
            Vector2 ufoExitTarget = ufoExitStart + new Vector2(exitDirection * fullScreenOverlay.rect.width, screenHeight * 0.25f);
            while (t < ufoDepartSeconds)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / ufoDepartSeconds);
                ufoRt.anchoredPosition = Vector2.Lerp(ufoExitStart, ufoExitTarget, EaseInCubic(p));
                yield return null;
            }

            Destroy(ufoRt.gameObject);
            sequenceRoutine = null;
        }

        private static RectTransform CreateImage(string name, RectTransform parent, Sprite sprite, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            img.preserveAspect = false;

            return rt;
        }

        /// <summary>
        /// Fakes BackgroundPedestrianManager.MoveRoutine's footstep bounce/tilt in place -- same
        /// math (Mathf.Abs(sin) vertical bounce, sin-based rotation tilt, both scaled by the
        /// current wobble amplitude), just with X pinned instead of advancing, since this
        /// pedestrian isn't going anywhere until the beam takes it. Added 2026-09-23 so a stolen
        /// pedestrian keeps looking like it's still walking through the warning telegraph and UFO
        /// descent instead of freezing solid the instant MoveRoutine's coroutine gets cancelled.
        /// </summary>
        private static void ApplyWalkInPlaceBounce(
            RectTransform pedestrian,
            float elapsedTime,
            float pinnedX,
            float baselineY,
            float lookDirMultiplier,
            float verticalAmplitude,
            float rotationAmplitude,
            float frequency)
        {
            float wobbleSine = Mathf.Sin(elapsedTime * frequency);
            float verticalOffset = Mathf.Abs(wobbleSine) * verticalAmplitude;
            float rotationAngle = wobbleSine * rotationAmplitude;

            pedestrian.anchoredPosition = new Vector2(pinnedX, baselineY + verticalOffset);
            pedestrian.localRotation = Quaternion.Euler(0f, 0f, rotationAngle * lookDirMultiplier);
        }

        /// <summary>
        /// Shifts a RectTransform's pivot to (0.5, 0.5), correcting anchoredPosition in the same
        /// step so the rect doesn't visually jump -- standard Unity recipe (offset = (newPivot -
        /// oldPivot) * rect.size, added to anchoredPosition; both are pre-scale local units, so
        /// localScale never enters this). Added 2026-09-22: pedestrian sprites spawn with pivot
        /// (0.5, 0) (feet-anchored, for standing on the street baseline), and rotating around that
        /// off-center pivot swings the whole sprite in a circle instead of spinning in place. A
        /// no-op if the pivot is already centered.
        /// </summary>
        private static void RecenterPivot(RectTransform rt)
        {
            Vector2 oldPivot = rt.pivot;
            Vector2 newPivot = new Vector2(0.5f, 0.5f);
            if (oldPivot == newPivot)
            {
                return;
            }

            Vector2 size = rt.rect.size;
            Vector2 deltaPivot = newPivot - oldPivot;
            rt.pivot = newPivot;
            rt.anchoredPosition += new Vector2(deltaPivot.x * size.x, deltaPivot.y * size.y);
        }

        private static float EaseOutCubic(float p) => 1f - Mathf.Pow(1f - p, 3f);
        private static float EaseInCubic(float p) => p * p * p;

        private static Sprite GetUfoSprite()
        {
            if (cachedUfoSprite == null)
            {
                cachedUfoSprite = BuildUfoSprite();
            }

            return cachedUfoSprite;
        }

        private static Sprite GetBeamSprite()
        {
            if (cachedBeamSprite == null)
            {
                cachedBeamSprite = BuildBeamSprite();
            }

            return cachedBeamSprite;
        }

        private static Sprite GetWarningGlowSprite()
        {
            if (cachedWarningGlowSprite == null)
            {
                cachedWarningGlowSprite = BuildWarningGlowSprite();
            }

            return cachedWarningGlowSprite;
        }

        /// <summary>
        /// Procedural 128x64 flying-saucer silhouette: a flattened gray ellipse body, a lighter
        /// dome on top, a few colored "lights" along the rim. Simple/geometric enough to read
        /// fine without hand-drawn art -- see the class doc comment. Cached statically so this
        /// only runs once per session, not once per abduction.
        /// </summary>
        private static Sprite BuildUfoSprite()
        {
            const int width = 128;
            const int height = 64;
            var pixels = new Color32[width * height];

            Color32 bodyColor = new Color32(150, 155, 165, 255);
            Color32 bodyShadow = new Color32(105, 110, 120, 255);
            Color32 domeColor = new Color32(190, 225, 235, 200);
            Color32 clear = new Color32(0, 0, 0, 0);

            float bodyCenterY = height * 0.42f;
            float bodyRadiusX = width * 0.48f;
            float bodyRadiusY = height * 0.22f;

            float domeCenterY = height * 0.5f;
            float domeRadiusX = width * 0.22f;
            float domeRadiusY = height * 0.28f;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float nx = x - width * 0.5f;

                    float bodyDy = y - bodyCenterY;
                    bool inBody = (nx * nx) / (bodyRadiusX * bodyRadiusX) + (bodyDy * bodyDy) / (bodyRadiusY * bodyRadiusY) <= 1f;

                    float domeDy = y - domeCenterY;
                    bool inDome = y >= bodyCenterY && (nx * nx) / (domeRadiusX * domeRadiusX) + (domeDy * domeDy) / (domeRadiusY * domeRadiusY) <= 1f;

                    Color32 pixel = clear;
                    if (inDome)
                    {
                        pixel = domeColor;
                    }
                    else if (inBody)
                    {
                        pixel = bodyDy < 0 ? bodyShadow : bodyColor;
                    }

                    pixels[y * width + x] = pixel;
                }
            }

            // A few rim "lights" -- small colored dots along the widest part of the body.
            Color32[] lightColors =
            {
                new Color32(255, 210, 60, 255),
                new Color32(255, 90, 90, 255),
                new Color32(90, 210, 255, 255),
            };
            const int lightCount = 5;
            for (int i = 0; i < lightCount; i++)
            {
                float lx = width * 0.5f + Mathf.Lerp(-bodyRadiusX * 0.75f, bodyRadiusX * 0.75f, i / (float)(lightCount - 1));
                float ly = bodyCenterY + bodyRadiusY * 0.55f;
                PlotDot(pixels, width, height, lx, ly, 2.5f, lightColors[i % lightColors.Length]);
            }

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            texture.SetPixels32(pixels);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
        }

        private static void PlotDot(Color32[] pixels, int width, int height, float cx, float cy, float radius, Color32 color)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(cx - radius));
            int maxX = Mathf.Min(width - 1, Mathf.CeilToInt(cx + radius));
            int minY = Mathf.Max(0, Mathf.FloorToInt(cy - radius));
            int maxY = Mathf.Min(height - 1, Mathf.CeilToInt(cy + radius));

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    if (dx * dx + dy * dy <= radius * radius)
                    {
                        pixels[y * width + x] = color;
                    }
                }
            }
        }

        /// <summary>
        /// Procedural vertical beam: narrow near the ground, widening toward the top (near the
        /// UFO), warm-white glow with a brighter core -- the "rapture" look Aceyfer referenced
        /// (style/tone reference only, not a reproduction of any film's actual footage or art).
        /// Row 0 = bottom of the texture (narrow end); only used stretched via a RectTransform
        /// here, so texture-space orientation only has to stay internally consistent with itself.
        /// </summary>
        private static Sprite BuildBeamSprite()
        {
            const int width = 128;
            const int height = 256;
            var pixels = new Color32[width * height];

            for (int y = 0; y < height; y++)
            {
                float v = y / (float)(height - 1); // 0 at bottom (narrow), 1 at top (wide)
                float halfWidthFrac = Mathf.Lerp(0.18f, 0.5f, v);

                for (int x = 0; x < width; x++)
                {
                    float nx = (x - width * 0.5f) / (width * 0.5f);
                    float distFrac = Mathf.Abs(nx) / halfWidthFrac;

                    Color32 pixel;
                    if (distFrac > 1f)
                    {
                        pixel = new Color32(0, 0, 0, 0);
                    }
                    else
                    {
                        float edgeSoftness = 1f - Mathf.SmoothStep(0.6f, 1f, distFrac);
                        float core = 1f - Mathf.SmoothStep(0f, 0.5f, distFrac);
                        byte alpha = (byte)Mathf.Clamp(edgeSoftness * 180f, 0f, 255f);
                        byte warmth = (byte)Mathf.Lerp(255f, 235f, core);
                        pixel = new Color32(255, 255, warmth, alpha);
                    }

                    pixels[y * width + x] = pixel;
                }
            }

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            texture.SetPixels32(pixels);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 1f), 100f);
        }

        /// <summary>
        /// Procedural soft radial glow -- a simple round warning marker for the ground-telegraph
        /// phase added 2026-09-22 (see the class doc comment / AbductionSequence). Plain white with
        /// alpha falling off from center to edge; the actual warm-yellow tint and pulsing are done
        /// by tinting this sprite's Image.color at runtime, not baked into the texture, so one
        /// cached sprite covers the whole pulse. Cached statically like the other two sprites here.
        /// </summary>
        private static Sprite BuildWarningGlowSprite()
        {
            const int size = 64;
            var pixels = new Color32[size * size];
            float center = size * 0.5f;
            float radius = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float distFrac = Mathf.Sqrt(dx * dx + dy * dy) / radius;
                    float alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(distFrac));
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.Clamp(alpha * 255f, 0f, 255f));
                }
            }

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            texture.SetPixels32(pixels);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only test hook, mirroring RandomEventManager.ForceCooldownElapsed -- lets a
        /// DebugCheats entry trigger an abduction on the next tick instead of waiting a real
        /// 10-20 minutes to verify this manually. NOT yet wired into DebugCheats.cs (deliberately
        /// left as a one-line follow-up rather than guessed blind against that file's existing
        /// menu structure -- see the scoping doc / TASKLIST_DETAILS.md for this task).
        /// </summary>
        public void ForceCooldownElapsed()
        {
            secondsUntilNextAbduction = 0f;
        }
#endif
    }
}
