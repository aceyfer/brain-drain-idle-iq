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
- [ ] 7. Doc sync commit — Bible §6 economy table to 16 buildings; CLAUDE.md inventory refresh (details §7)
- [ ] 8. Guard the 12 unguarded Editor tools (batch, one commit) (details §8)
- [x] 17. COGS portrait/dialogue visibility — four stacked root causes fixed (details §17, commits `1e01517`/`7fac5d8`/`05c7c13`/`5d3085c`)
- [x] 18. Oversized pedestrians — CLOSED. Three root causes: unnormalized source art + inconsistent hand scales, Animator sprite-swap overriding normalization, rank-figure diorama compositing over the game via Diorama Camera (details §18, commits `aba3091`/`0ec69f9`/`49cda11`/`6d7f709`)
- [x] 20. Dialogue pacing/queue in DialogueManager — CLOSED. Codex findings audited (1 factual error caught); commits: `3e44480` pacing (min-gap + cooldown + coalescing), `780eb8a` shop sort determinism, `06aaa87` legacy walker retirement (details §20)
- [ ] 20b. Dialogue log panel + button (GTA-style narrator-line history; also enables anti-repeat) — scoped, not started (details §20)
- [ ] 21. World portrait position tune (code-owned) if it overlaps THE SNOTTING button — pending Aceyfer's visual check (details §21)

## DECISION REQUIRED — Monetization (owner: Aceyfer)
- [x] 10. KILL "neurons" premium currency — DONE. All premium = direct real-world currency via GodTierStoreManager only. Bad Words Pack = $5.00 (details §10)

## THEN — Path to Market
- [ ] 11. Define "first playable" cut line — what ships v1, what waits (details §11)
- [ ] 12. Real IAP wiring for direct-currency purchases (platform store) (details §12)
- [ ] 13. Art debt: COGS Level 1 portrait, remaining placeholder art pass (details §13)
- [ ] 14. Device build + test on real phone (iOS target visible in editor) (details §14)
- [ ] 15. Store presence: name/ratings/screenshots/description (details §15)

## PARKED (do not touch until NOW+NEXT clear)
- ShopTabView virtualization + real purchase routing (dormant by design)
- Shop-3/Points family (PointsShopPanel/CashShopPanel/ConvertPanel) reconciliation
- UIBlockDebugger legacy-Input rewrite (quarantined)
