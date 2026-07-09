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
- [ ] 6. BG1.jpg texture import → Sprite (2D and UI) — kills stageSprites[0] warning (details §6)
- [ ] 7. Doc sync commit — Bible §6 economy table to 16 buildings; CLAUDE.md inventory refresh (details §7)
- [ ] 8. Guard the 12 unguarded Editor tools (batch, one commit) (details §8)
- [ ] 9. PremiumShopManager vs GodTierStoreManager — read-only check for duplicate responsibility (details §9; now step 1 of §16)
- [ ] 16. Replace third shop tab (RP/World Restoration) with Premium real-currency store — starts with §9's audit (details §16)
- [ ] 17. COGS portrait renders on Play Stop but not during Play — inverted visibility (details §17)
- [ ] 18. Oversized pedestrians (details §18)

## DECISION REQUIRED — Monetization (owner: Aceyfer)
- [ ] 10. KILL "neurons" premium currency — never approved. All premium = direct real-world currency. Bad Words Pack = $5, not 50 neurons. Audit shop specs + code for any neuron references, purge, log decision (details §10)

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
