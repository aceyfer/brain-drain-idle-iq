# Brain Freeze Spacing Audit + Rewarded-Ad Idle Extension

**Status:** audit + design, 2026-08-31. No code, assets, or scene touched yet — this is written the same way as the IAP plan, pending Aceyfer's call on the open questions at the bottom.

## Part 1 — Brain Freeze duration spacing

### Current state (read directly from the live assets)

| Asset | itemId | Duration | Price | $/hour |
|---|---|---|---|---:|
| `BrainFreeze.asset` | `brain_freeze` | 24h | $1.50 | $0.0625 |
| `BrainFreeze48.asset` | `brain_freeze_48` | 48h | $2.50 | $0.0521 |
| `DeepFreeze.asset` | `deep_freeze` | 168h (7d) | $5.99 | $0.0357 |

Spacing today is 24 → 48 → 168: a flat +24h step, then a +120h jump. Your instinct is right that 1/3/7 days (24/72/168) reads far more like a deliberate tier ladder than 24/48/168 does — it's the same pattern nearly every mobile game's "boost" tier uses, and it fixes the awkward "why does the second tier only add one more day" feel.

### The catch: duration and price move together

Every tier right now gets *cheaper per hour* the bigger it is — that's what makes the top tier ("Deep Freeze") the flagship deal instead of a rip-off. If duration goes from 48h to 72h **without** touching the $2.50 price, the middle tier's rate becomes $0.0347/hour — *lower* than Deep Freeze's $0.0357/hour. The 3-day option would quietly become a better deal than the 7-day option, which undercuts the exact tier that's supposed to be your best value and your highest price point. This is a real inversion, not a nitpick — it's the kind of thing a player's Reddit thread finds in about a day.

To keep the tiers in the order they're supposed to be in, price needs to move too. A few concrete options at 72h:

| Option | Price | $/hour | Stays below Deep Freeze's $0.0357/hr? |
|---|---|---:|---|
| A — cheapest bump | $2.99 | $0.0415 | Yes, comfortable margin |
| B — round number | $3.49 | $0.0485 | Yes |
| C — no price change | $2.50 | $0.0347 | **No — inverts the ladder** |

I'd steer away from C. Between A and B it's a pure business call — A keeps the jump small and easy to swallow for someone who already bought the 48h version before; B leans harder into "3 days costs meaningfully more than 1 day, meaningfully less than 7."

### Naming detail worth a decision

The asset's internal `itemId` (`brain_freeze_48`) and store `productId` (`com.eighthkind.braindrain.brainfreeze48`) both say "48." Since no product is registered on Google Play yet (per the IAP plan, nothing's live there), this is the one moment where renaming those IDs to something like `brain_freeze_72` / `...brainfreeze72` costs nothing. Once a product ID is ever actually created in Play Console, the IAP plan's own rule kicks in — "once registered/sold, treat it as permanent" — so this is a now-or-never cleanup. Leaving the ID saying "48" while the display says "72" is harmless (players never see the ID) but is exactly the kind of thing that reads as a bug report six months from now if anyone greps the codebase.

### Recommended change (pending price pick above)

In `BrainFreeze48.asset`: `freezeDurationHours: 48` → `72`, `displayName: 'Brain Freeze: 48'` → `'Brain Freeze: 72'`, description's "Two days" → "Three days," `realMoneyPriceDisplay` updated to whichever price is picked, and optionally the itemId/productId rename discussed above. One-line, low-risk change once the price question is answered — happy to make it the moment you pick A, B, or your own number.

## Part 2 — Optional rewarded ads for players who can't/won't pay

### What this actually plugs into

The game already has almost exactly this mechanic, just as a paid permanent purchase, not a free repeatable one. Here's the real chain, read directly from `PlayerIQManager.cs`:

- Every player already gets **8 free real-time hours** (`OfflineDecayMaxHours = 8f`) before IQ decays all the way down to the floor when the app is closed — that's already your "away for a work day" grace period, today, for free, no purchase needed.
- The **24-Hour Corporate Cloak** ($9.99, one-time) adds +24 hours to that window — but permanently, forever, via `PlayerIQManager.ExtendOfflineDecayWindow()`. It's a stat upgrade, not a timed boost.
- **Brain Freeze** (Part 1, above) is a different lever entirely — it raises the decay *floor* from 1 to 113 for a purchased real-time duration, plus an instant jump to 200 IQ. It doesn't touch the *window*.

So there are two different existing systems you could hang a "watch an ad" reward on, and they behave very differently. This matters for the design, not just as trivia.

### Why the ad reward can't just call `ExtendOfflineDecayWindow()`

That method is explicitly documented as a **permanent** accumulator — it only ever goes up, never resets, by design, because it exists for one $9.99 one-time purchase. If a free ad reward called the same method, a player who watches ads regularly would build up a permanent bonus over time with no ceiling — eventually for free exceeding what someone paid $9.99 for, and with no natural stopping point. That's not a design nitpick, it's a real path to quietly devaluing your own $9.99 item. The ad reward needs its own mechanism, separate from Corporate Cloak's stat.

### Recommended design: retroactive recovery at the "welcome back" moment, not a pre-booked timer

Your framing ("1 ad = 30 min, 2 ads = 1 hour, up to a cap") describes a *timer you'd extend before leaving*. The trouble with that shape is you don't know how long you'll be gone when you leave — you'd have to guess and pre-buy ad-watches against an unknown absence. The game's actual architecture points at something better: `PlayerIQManager.LoadStateWithOfflineDecay()` already computes decay **after the fact**, the moment the player returns, and already fires `OnOfflineDecayApplied` (which `DialogueManager` already listens to for the "welcome back" narrator line). That's the natural hook.

Proposed flow: when the player returns and decay has been applied, show a rewarded-ad prompt alongside (or instead of) the narrator line — "COGS docked you N IQ while you were gone. Watch an ad to get some of it back." Each ad watched recovers the value of 30 minutes less decay, same ladder you described (1 ad = 30 min recovered, 2 ads = 1 hour, linear — not accelerating, matching "2 ads = 1 hour" being exactly double "1 ad = 30 min"), capped at a maximum. Since the base free window is already 8 hours, capping the ad-recoverable portion at **+4 hours** (8 ads' worth) lands the *effective* protected window at 12 hours for a F2P player willing to watch a full ladder of ads — which is exactly the 8-or-12-hour range you floated, and it arrives there without ever touching Corporate Cloak's permanent bonus or Brain Freeze's floor mechanic.

This also happens to be the exact pattern most idle games use ("watch a video to get more of what you earned/avoided losing while away") — it's proven, players expect it, and Google/Meta ad networks fill this specific ad placement reliably because it's such a common unit type.

Mechanically this needs one new time-boxed value (not a permanent accumulator) — closer in shape to how Brain Freeze's own expiry timestamp works than to Corporate Cloak's flat float, since its entire point is "this bonus applies to the return that just happened," not "forever from now on."

### SDK feasibility

Checked current documentation (Unity's own): legacy **Unity Ads** is deprecated in favor of **Unity LevelPlay** (`com.unity.services.levelplay`), Unity's current ad-mediation package — it's what mediates rewarded video across AdMob and other networks, ships an official rewarded-ad API, and is documented as compatible with the Unity 6000.x line this project is already on. This is a real, current, supported package — not a dead end.

One thing worth checking before adding it: `TASKLIST.md`'s PARKED section mentions a prior ad SDK ("DataBite Plankton") was removed from this project in commit `c567a7c`, with no reason recorded beyond "removed... as an ad SDK." I don't have git-log access from here to see why. Worth a quick look (Claude Code can pull that commit's message/diff) before installing a new ads package, in case there's a known reason — a policy call, a conflict with something else in the project, a store-review issue — that would be worth knowing before repeating it. If it turns out to be unrelated (e.g. it bundled unwanted analytics, or was just a different vendor being swapped out), LevelPlay is clear to proceed.

Rough effort: comparable to the IAP integration already planned in `iap-integration-plan.md` — package install, dashboard/account setup, per-platform (Android first, matching the project's existing decision) ad-unit configuration, a rewarded-ad callback wired to the new time-boxed recovery value, and GDPR/consent handling (LevelPlay ships a consent-management flow; needed regardless of EU player share, since Google Play requires it). Unlike real-money IAP, ad-reward crediting is low-stakes enough that it doesn't need the server-authoritative validation the IAP plan recommends — a modified client giving itself a few extra IQ points isn't a real threat model the way faking a $9.99 receipt is.

Bonus: LevelPlay/AdMob rewarded placements generate real ad revenue on their own, independent of whether the player ever buys anything — this isn't just a F2P kindness feature, it's a second monetization channel next to the God Shop's pure-IAP model.

## Open questions — need your call before implementation

1. **72h price:** $2.99, $3.49, or your own number? (Not $2.50 — see the inversion above.)
2. **itemId/productId rename** (`brain_freeze_48` → `brain_freeze_72`): do it now while it's free, or leave it since it's invisible to players?
3. **Ad ladder cap:** is +4 hours (8 ads, landing at a 12h effective window) the right ceiling, or did you have a specific number in mind for "a good cap"?
4. **Retroactive recovery vs. pre-booked timer:** confirm the "watch ads at the welcome-back screen to recover lost IQ" framing above, since it's a different shape than "extend a timer before you leave" — I think it fits the existing code better and is the more standard pattern, but it's your call on the actual player experience.
5. Should I (or Claude Code, once git settles) check what commit `c567a7c` actually removed and why, before LevelPlay gets added?

Once these are answered I can make the Brain Freeze asset edit immediately (it's small and low-risk) and write a LevelPlay integration plan with the same structure as the IAP one, ready to hand off for implementation.
