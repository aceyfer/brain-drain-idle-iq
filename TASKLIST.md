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
- [x] 10. KILL "neurons" premium currency — DONE. All premium = direct real-world currency via GodTierStoreManager only. Bad Words Pack = $5.00 (details §10)

## THEN — Path to Market
- [ ] 23. FTUE / comprehension pass (COGS-narrated onboarding) — scoped, BLOCKS §11 (details §23)
- [ ] 24. Dialogue log v2 + The Pocket (log polish, COGS/STREET tab split, card pocket — supersedes §23c) — scoped, not started, after §23 (details §24)
- [x] 11. Define "first playable" cut line — what ships v1, what waits — **PASSED**. First-playable achieved 2026-07-23, Aceyfer verdict (details §11)
- [x] 26. Economy display/design audit — CLOSED. Jumper Cables/IQ Overclock Chip descriptions fixed, PlayerIQManager subscribe guard added (`f0ba24d`); Overcharged IQ system + Underground Economy's 9BP/7Cash split doc-synced into CLAUDE.md/Bible §6; remaining findings confirmed intentional, no action needed (details §26)
- [ ] 12. Real IAP wiring for direct-currency purchases (platform store) (details §12)
- [ ] 13. Art debt: COGS Level 1 portrait, remaining placeholder art pass (details §13)
- [ ] 14. Device build + test on real phone (iOS target visible in editor) (details §14)
- [ ] 15. Store presence: name/ratings/screenshots/description (details §15)

## PARKED (do not touch until NOW+NEXT clear)
- ShopTabView virtualization + real purchase routing (dormant by design)
- Shop-3/Points family (PointsShopPanel/CashShopPanel/ConvertPanel) reconciliation
- UIBlockDebugger legacy-Input rewrite (quarantined)
- §25 concept: COGS counterfeits the resistance (endgame content, post-first-playable) — not scoped (details §25)
