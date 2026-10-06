# PLAYER PURCHASE RULES — Brain Drain: Idle IQ

*Plain-English reference for the store listing and support replies. Grounded in the actual
implementation audited and fixed 2026-10-05 (IAP safety audit, `TASKLIST_DETAILS.md`), updated
2026-10-06 for the freeze-inventory amendment. Not legal text — copy the relevant lines into the
store listing / support macros as needed, and keep this file in sync if the purchase logic ever
changes.*

---

## What's for sale

All God Shop items are **real-money purchases only** — there is no in-game currency that can buy
them, and no in-game currency path exists to any of them (a hard project rule, see
`CLAUDE.md`/`PROJECT_BIBLE.md` §7 rule 13).

| Item | Price | What you get | Type |
|---|---|---|---|
| **Bad Words Pack** | $5.00 | Unlocks the Tier 3 unfiltered swear-word narrator lines. Toggle them on/off anytime once owned. | One-time (non-consumable) |
| **Brain Freeze** | $1.50 | Adds 1 charge to your Wallet. Activate anytime — while active, your IQ can't drop below 113 for **24 hours** and you start at 200. | Wallet charge (unlimited purchases) |
| **Brain Freeze: 72** | $2.99 | Same 113 floor, same 200 start, for **72 hours (3 days)** once activated. | Wallet charge (unlimited purchases) |
| **Deep Freeze** | $5.99 | Same 113 floor, same 200 start, for a full **week (168 hours)** once activated. | Wallet charge (unlimited purchases) |

Prices shown in-game always come from Google Play's own listing for your country/currency — the
numbers above are reference only and may not match what you're actually charged.

## Bad Words Pack — buy once, yours forever

- You only ever pay for this once. Once owned, the Shop button becomes an **on/off toggle** for
  the unfiltered lines — it never charges you again.
- **Reinstalling the app or switching to a new device?** Google Play automatically tells the game
  what you already own the moment the Shop connects — you don't need to do anything.
- If that automatic check ever misses it (a slow connection, a very fresh install, or on iOS in
  the future), open **Settings → Restore Purchases**. This re-checks what Google Play has on
  record for your account and restores anything the game doesn't already show as owned. It's safe
  to tap any time, as often as you like — it never charges you.

## Brain Freeze family — buy charges, activate them whenever you want

*Replaced 2026-10-06 — buying used to activate a freeze immediately and stack its duration onto
whatever was already running. It no longer does either of those things.*

- These three items all do the same thing (hold your IQ at a floor of 113, starting you at 200)
  for different lengths of time once **activated**.
- **Buying adds a charge to your Wallet — it does not start anything.** You can buy as many
  charges as you want, of any mix of the three, at any time. The Shop button always shows the
  real price; it never changes to reflect what's running.
- **Only one freeze can ever be active at a time.** Open the Wallet and tap **USE** on a charge to
  start it. While one is running, every other Use button (including other charges of the *same*
  item) is disabled and shows how much time is left on the active one instead — there is no way to
  stack or extend an active freeze by buying or activating more.
- This caps the longest possible protection at whichever single charge you activate — 7 days for
  Deep Freeze, the longest of the three. No more month-long runs from stacking multiple purchases.
- When the active freeze runs out, the game shows a small nudge — "Freeze ended. Use another?
  (xN left)" — if you still have charges. It never activates one for you; you choose when to use
  the next one, if any.
- Your charge count and whichever freeze is currently active are mirrored to the cloud and
  restored on reinstall or a new device (see "No lost purchases" below) — buying 5 charges and
  never using them doesn't lose them if you reinstall.

## If a purchase doesn't go through immediately

- **"PURCHASE PENDING"** — some payment methods (certain gift cards, cash-based payment options)
  take a little while to clear on Google's end. The game shows this exactly, and won't let you buy
  the same item again until it resolves. You will not be charged twice, and the item is granted
  automatically the moment Google confirms the payment — no need to reopen the Shop or do
  anything else.
- **"STORE UNAVAILABLE"** — shown when the game can't reach Google Play (no internet, or Google
  Play itself is briefly down). Buy buttons are disabled and no purchase attempt is made. Try
  again once you're back online.

## Refunds

**Refunds are handled entirely by Google Play, not by us.** We don't have a way to directly
refund a purchase from inside the game. To request one:

1. Open the Google Play Store app
2. Go to **Menu → Account → Order history** (or **Google Play → Payments & subscriptions →
   Budget & history**, depending on your Play Store version)
3. Find the Brain Drain: Idle IQ purchase
4. Select **Report a problem** / **Request a refund**

Google's own refund policy and timing apply, not ours — within a short window (typically within a
couple of hours of purchase) Google Play often lets you refund directly from Order history with no
questions asked; after that it goes through Google's standard review.

If Google approves a refund for a **one-time item** (Bad Words Pack), the game automatically
removes it the next time it checks in with the store — you don't need to tell us. A refunded
**Brain Freeze charge** simply isn't re-granted; since it's a consumable, Google doesn't track
long-term ownership of it the way it does for Bad Words Pack. A charge already spent by activating
it isn't retroactively removed if it's since expired.

## What support can safely tell a player

- "Your purchase didn't appear" → ask them to try **Settings → Restore Purchases** first, with the
  app online. If that doesn't surface it, ask for their Google Play order ID/receipt so the Play
  Console order history can be checked.
- "I got charged but don't have the item" → check whether the Play Store itself shows the charge
  as completed. If it does and the game still doesn't show the item after Restore Purchases and a
  full app restart, that's a genuine bug report, not a refund question — escalate it.
- "I want a refund" → point them to the Google Play refund flow above. We cannot process refunds
  on our end.
- "I bought Brain Freeze twice, did I waste money?" → no — every purchase adds a charge to the
  Wallet, nothing is ever wasted or overwritten by buying more. They just need to open the Wallet
  and tap Use on each charge whenever they're ready (one at a time).
- "I bought a freeze but it's not protecting my IQ" → buying only adds a Wallet charge now, it
  doesn't activate automatically — have them open the Wallet and tap **USE** on it.

## No lost purchases — Wallet charges survive a reinstall

- Freeze charges and whichever freeze is currently active are mirrored to UGS Cloud Save right
  after every purchase and every activation, keyed to your signed-in player identity.
- On launch, the game reconciles its local count against the cloud count for each item by taking
  the **higher** of the two — a charge known to either side is never lost, and the merge itself
  can never invent an extra charge that wasn't actually paid for.
- **Player identity across reinstall**: the game prefers signing you in via Google Play Games,
  which keeps your identity (and so your Wallet) tied to your Google account across a reinstall or
  a new device. If Google Play Games sign-in isn't available (declined, unavailable, or not yet
  configured on this build), it falls back to an anonymous identity — and an anonymous identity
  does **not** survive a reinstall, so a Wallet mirrored only under one would appear empty on the
  next install until Google Play Games sign-in is completed. See `TASKLIST_DETAILS.md`'s
  2026-10-06 entry for the current verification status of this fallback.
- If Cloud Save can't be reached at the moment of a purchase or activation, the game keeps your
  local charge (already safely written to the normal save file) and quietly retries the mirror
  next time — a purchase is never held up or lost waiting on the network.

## For whoever next touches this file

This document describes **intended, audited behavior** as of the 2026-10-05 IAP safety pass and
the 2026-10-06 freeze-inventory amendment (`TASKLIST_DETAILS.md`). If you change how purchases,
grants, or restoration work in code, update this file in the same change — a support reply that
contradicts what the game actually does is worse than no documentation at all.
