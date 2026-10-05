# PLAYER PURCHASE RULES — Brain Drain: Idle IQ

*Plain-English reference for the store listing and support replies. Grounded in the actual
implementation audited and fixed 2026-10-05 (IAP safety audit, `TASKLIST_DETAILS.md`). Not legal
text — copy the relevant lines into the store listing / support macros as needed, and keep this
file in sync if the purchase logic ever changes.*

---

## What's for sale

All God Shop items are **real-money purchases only** — there is no in-game currency that can buy
them, and no in-game currency path exists to any of them (a hard project rule, see
`CLAUDE.md`/`PROJECT_BIBLE.md` §7 rule 13).

| Item | Price | What you get | Type |
|---|---|---|---|
| **Bad Words Pack** | $5.00 | Unlocks the Tier 3 unfiltered swear-word narrator lines. Toggle them on/off anytime once owned. | One-time (non-consumable) |
| **Brain Freeze** | $1.50 | Your IQ can't drop below 113 for **24 hours**. Starts you at 200 immediately. | Timed (consumable) |
| **Brain Freeze: 72** | $2.99 | Same 113 floor, same 200 start, for **72 hours (3 days)**. | Timed (consumable) |
| **Deep Freeze** | $5.99 | Same 113 floor, same 200 start, for a full **week (168 hours)**. | Timed (consumable) |

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

## Brain Freeze family — buying again extends it, never wastes it

- These three items all do the same thing (hold your IQ at a floor of 113) for different lengths
  of time.
- If you buy one while a freeze is **already active**, the new time is added on top of what's
  left — it does not restart the clock or get wasted. Buy Brain Freeze (24h) with 10 hours left on
  a previous one, and you'll have 34 hours left afterward.
- While a freeze from that exact item is currently active, its Shop button shows how much time
  buying it again would add (`+24h`, `+72h`, `+7d`) instead of the price, so it's always clear
  that tapping it stacks rather than charges you for something you already have.

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
**timed freeze** simply isn't re-granted; since it's a consumable, Google doesn't track long-term
ownership of it the way it does for Bad Words Pack.

## What support can safely tell a player

- "Your purchase didn't appear" → ask them to try **Settings → Restore Purchases** first, with the
  app online. If that doesn't surface it, ask for their Google Play order ID/receipt so the Play
  Console order history can be checked.
- "I got charged but don't have the item" → check whether the Play Store itself shows the charge
  as completed. If it does and the game still doesn't show the item after Restore Purchases and a
  full app restart, that's a genuine bug report, not a refund question — escalate it.
- "I want a refund" → point them to the Google Play refund flow above. We cannot process refunds
  on our end.
- "I bought Brain Freeze twice, did I waste money?" → no — repeat purchases of the same timed item
  always stack the remaining time, they're never wasted or overwritten.

## For whoever next touches this file

This document describes **intended, audited behavior** as of the 2026-10-05 IAP safety pass
(`TASKLIST_DETAILS.md`). If you change how purchases, grants, or restoration work in code, update
this file in the same change — a support reply that contradicts what the game actually does is
worse than no documentation at all.
