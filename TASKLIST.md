# BRAIN DRAIN — SHIP TASKLIST (Parent)

> Rule: work top-to-bottom. One task, one commit. Details, decisions, and changelog live in `TASKLIST_DETAILS.md` — go there only when a task needs context.
> Status legend: [ ] open · [~] in progress · [x] done · [!] blocked

## NOW — Shop Collapse Endgame (blocks everything)
- [x] 1. CanvasGuard fix — stop it fighting nested tab canvases + Play-mode guard (details §1)
- [x] 2. Stray giant shop element — get hierarchy path, diagnose, resolve (details §2)
- [x] 3. Paste FindObjectsSortMode console line to Claude Code — confirm harmless (details §3)
- [x] 4. Full clean Play-mode re-test — tabs, buys on all 3 tabs, close/reopen, no stray (details §4)
- [x] 5. LAND THE COMMIT TRAIN — 4, 5, guard, AutoSceneFixes, CanvasGuard, Bible (details §5)
> Note: BP and Cash tabs fully verified working end to end. RP tab superseded by design decision — see §16; not a bug, not blocking.

## NEXT — Stability & Hygiene
- [x] 16. God Shop — COMPLETE end to end. Phase A consolidation (commits `939222f`..`34841b7` + docs `516ce70`); Phase B build-out (`8d15c22` tab UI + owned-toggle, `0f0809c` code-owned tab labels, `9cb8075` rebirth-trigger suppression, `de5d4c0` convert-arrow-glyph cleanup) (details §16)
- [x] 6. BG1-BG6 Sprite Mode Multiple→Single (not Texture Type) — healed 7 dangling refs: 6x stageSprites + SkylineBG m_Sprite (details §6, commit `36c76c1`)
- [x] 7. Doc sync — DONE. Bible §6 rebuilt to 16 buildings (both tabs, defaults quirk, 3 flagged balance anomalies); CLAUDE.md refreshed (16-building reality, costType purchase routing, DioramaManager/COGSWorldPortraitUI marked retired, §20 pacing, 25-tap threshold) (details §7)
- [x] 8. Guard the 12 unguarded Editor tools — CLOSED. All 19 MenuItem entry points guarded via shared `EditorToolGuard` helper, batch commit `e1ba781` (details §8)
- [x] 17. COGS portrait/dialogue visibility — four stacked root causes fixed (details §17, commits `1e01517`/`7fac5d8`/`05c7c13`/`5d3085c`)
- [x] 18. Oversized pedestrians — CLOSED. Three root causes: unnormalized source art + inconsistent hand scales, Animator sprite-swap overriding normalization, rank-figure diorama compositing over the game via Diorama Camera (details §18, commits `aba3091`/`0ec69f9`/`49cda11`/`6d7f709`)
- [x] 20. Dialogue pacing/queue in DialogueManager — CLOSED. Codex findings audited (1 factual error caught); commits: `3e44480` pacing (min-gap + cooldown + coalescing), `780eb8a` shop sort determinism, `06aaa87` legacy walker retirement (details §20)
- [x] 20b. Dialogue log panel + button (GTA-style narrator-line history; also enables anti-repeat) — CLOSED, remaining play-checks moved to §11 (details §20)
- [x] 20c. Chatter bubble fade curve + readability + pedestrian duplicate-NPC fix — CLOSED (details §20)
- [x] 21. World portrait overlap check — CLOSED, verified clear by Aceyfer in Play mode 2026-07-15 (moot anyway: world portrait retired §17/§18); SNOTTING badge sits alone top-right (details §21)

## DECISION REQUIRED — Monetization (owner: Aceyfer)
- [x] 10. KILL "neurons" premium currency — DONE. All premium = direct real-world currency via GodTierStoreManager only. Bad Words Pack = $3.99 (details §10)

## THEN — Path to Market
- [x] 23. FTUE / comprehension pass (COGS-narrated onboarding) — SHIPPED & live-tested. FTUEManager + code-built IntelCardUI (`258deee`), FTUE seen-flags in save (`83e8de0`), Beat 9 Illumisnotti name-reveal (`52da27b`/`0b37537`); unblocked §11 (details §23)
- [x] 24. Dialogue log v2 + The Pocket — CLOSED. (a) washout/width/height fixes (`64bc500`/`446af70`/`452eee6`/`26624e1`), (b) COGS/STREET tabs + chatter history + cadence tune (`aa3b298`/`6445834`/`df36d36`), (c) THE POCKET card inventory, derive-from-flags (`36b0d3c`/`23a9707`) — all shipped & (c) Play-mode verified 2026-07-26; (d) post-ship UI fixes — opened-panel z-order + log tab-bar padding so the Dia-Log close-X isn't stolen by the Pocket button and clears the STREET tab (`6142a3d`/`d3ba6f3`) (details §24)
- [x] 11. Define "first playable" cut line — what ships v1, what waits — **PASSED**. First-playable achieved 2026-07-23, Aceyfer verdict (details §11)
- [x] 26. Economy display/design audit — CLOSED. Jumper Cables/IQ Overclock Chip descriptions fixed, PlayerIQManager subscribe guard added (`f0ba24d`); Overcharged IQ system + Underground Economy's 9BP/7Cash split doc-synced into CLAUDE.md/Bible §6; remaining findings confirmed intentional, no action needed (details §26)
- [ ] 12. Real IAP wiring for direct-currency purchases (platform store) (details §12)
- [ ] 13. Art debt: COGS Level 1 portrait, remaining placeholder art pass (details §13)
- [!] 14. Device build + test on real phone — BLOCKED on hardware (no Android device owned; iOS unbuildable on Windows). Android-first decided 2026-08-05. Player Settings prep landed (`c6d6b90`) (details §14)
- [ ] 15. Store presence: name/ratings/screenshots/description (details §15)

## PARKED (do not touch until NOW+NEXT clear)
- ShopTabView virtualization + real purchase routing (dormant by design)
- Shop-3/Points family (PointsShopPanel/CashShopPanel/ConvertPanel) reconciliation
- UIBlockDebugger legacy-Input rewrite (quarantined)
- §25 concept: COGS counterfeits the resistance (endgame content, post-first-playable) — not scoped (details §25)
- DailyCapThrottleOnset's 6 seeded lines are all full-range (0-100 RestorationPercent), same clinical/in-control COGS register — needs a proper tier pass with late-arc variants where COGS is rattled that the population's daily surplus is shrinking as the world heals, matching the existing degrading-tone-arc convention every other trigger's line pool already has
- Literates Pocket intel card: a Literate confidently blaming the daily cap slowdown on something absurd and unrelated, as ironic contrast to COGS's clinical accuracy — idea only, not written or wired
- `NumberFormatter.Format` has no suffix past "Qi" (~10^21) — values beyond that produce an unbounded digit string with no further suffix rollover. Pre-existing, unrelated to the stage-progression rebalance, not urgent given the 250,000,000 terminal threshold
- UI/Companion: Tier 6's `displayName` ("Illumisnotty Board Seats") never renders — `CashShopUIController`'s maxed branch is a hardcoded string, doesn't read tier data. Needs a `CompanionManager` accessor for the highest *owned* tier (distinct from `GetNextTier()`, null at max) plus a controller edit
- UI/Companion: `HotChickSpawner` spawns up to 6 figures but never reads `CompanionTierData` — no link between a spawned sprite and which tier it represents. Wire during the art pass
- UI/Companion: `EffectText` (Cash Shop companion row) has `m_fontSizeMin: 18` above its `m_fontSize: 14` — harmless today since auto-sizing is off, fix before ever enabling text auto-sizing on it
- Build size: delete `Assets/Nature - Essentials` (122 MB, zero scene references, 3D content in a 2D project) — largest easy win
- Build size: CyberWare music ships 929 MB of source WAVs (already Vorbis on import, only referenced clips ship) — reference only the stems actually used, never all 38
- Build size: `Assets/Resources` is 12 KB — keep it that way, anything placed there force-ships regardless of references
- Audio: `BackgroundMusicManager` has a single `backgroundMusicClip` field; CyberWare ships layered stems (Layer-1/2/3/Full + stingers), already owned. Add instrument layers as World Restoration progresses instead of swapping tracks — the world heals and the music fills in
- VFX: no smoke/fog asset exists yet, needed for the six restoration backdrops — URP particles can do this natively with layered soft-noise sprites, alpha tuned per stage, choking at stage 0, clear by stage 5
- VFX: two rain packs installed (Rain Particles, Rainy VFX) — acid rain early, clearing later, tied to restoration stage
- VFX: URP color grading per restoration stage on the existing volume profile — sickly desaturated green early, warm/saturated at Utopia, one volume, six presets
- Retention: `SimpleAndroidNotifications` is installed, unused — hook it to the daily engagement cap reset ("population's cognitive surplus replenishes")
- Retention: easter egg for a later chatter pass — a pedestrian line crediting the player with getting someone's IQ to exactly 117, deliberate Halo nod, distinct from the 113 floor
- Leaderboard (decision required before any work): saves are plain JSON on local disk, trivially editable — any leaderboard would be fabricated immediately, needs server-side validation. Options: Game Center + Google Play Games (free, native, no backend, two integrations) vs. Unity Gaming Services Leaderboards (one API for both, free tier adequate). The daily engagement cap's per-day production ceiling gives server-side validation a concrete rule for rejecting impossible scores
- 1 unspecified warning appeared in the Editor console during the 2026-08-04 God Shop price-glyph check (Bug 3, closed — turned out to be a screenshot artifact, not a real defect). Content wasn't captured at the time; if it recurs, get the actual text before investigating
- IAP: no package installed, `realMoneyPriceDisplay` is display-only — nothing can actually be purchased. Needs Apple/Google store setup, Unity IAP, receipt validation
