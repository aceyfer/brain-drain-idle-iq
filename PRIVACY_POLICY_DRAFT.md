# PRIVACY POLICY DRAFT — Brain Drain: Idle IQ

*Drafted to satisfy the "Privacy policy URL" requirement flagged in `STORE_LISTING_DRAFT.md` §15 / `TASKLIST_DETAILS.md` §15. Grounded in what's actually implemented as of the §12 IAP work (2026-09-22) — see the caveats at the bottom before publishing this anywhere. This is not legal advice; have an actual lawyer look at it before it goes live, especially the ad-tech and payment sections.*

---

**Effective date:** [FILL IN — the date you publish this]

Eighth Kind Studios ("we," "us," "our") develops **Brain Drain: Idle IQ** (the "Game"). This policy explains what information the Game collects, how it's used, and who it's shared with.

## 1. Information We Collect

**Purchases.** The Game sells optional real-money items (the "God Shop") through Google Play's billing system (and Apple's, if and when the Game ships on iOS). Google (or Apple) processes your payment directly — we never see or store your card number, bank details, or other payment credentials. We do receive confirmation that a purchase happened, which item was bought, and a purchase/transaction identifier, so we can grant you the item and prevent it from being granted twice.

**Account linking.** To let your purchases and progress follow you to a new device, the Game signs you in with Google Play Games Services. This shares a Google account identifier with us so we can tie your purchases to you across devices.

**Advertising.** The Game offers optional rewarded video ads (watch an ad, get a gameplay bonus) through Unity LevelPlay, an ad mediation platform. LevelPlay and the ad networks it connects to may collect your device's advertising identifier, general device and app information, and approximate location, in order to select, deliver, and measure ads. You are never shown an ad without choosing to start one.

**Gameplay data.** Your progress (buildings owned, currency balances, settings, etc.) is stored on your device. [Once the account-linked save backend described in `TASKLIST_DETAILS.md` §12 is live, add a sentence here: a copy of this progress is also stored on our servers, tied to your linked account, so it can be restored on a new device.]

**What we don't collect.** We don't ask for your name, email address, or phone number to play the Game. We don't collect precise GPS location. We don't knowingly collect personal information from children (see §5).

## 2. How We Use Information

- To grant, restore, and prevent duplication of purchases
- To let your progress and purchases follow you across devices
- To serve and measure the optional rewarded ads described above
- To diagnose crashes and technical problems
- To meet legal and tax obligations tied to app-store sales

We do not sell your personal information.

## 3. Third-Party Services

The Game uses these third-party services, each with its own privacy policy:

- **Google Play Billing / App Store payments** — payment processing
- **Google Play Games Services** — account linking and cross-device save restore
- **Unity LevelPlay** (and the ad networks it mediates to) — rewarded video ads
- [FILL IN once run: the specific ad network partners LevelPlay's Network Manager pulls in — each one technically deserves its own line here once known]

## 4. Your Choices

- **Ads:** you can reset or limit your device's advertising identifier in your device's system settings (Android: Settings → Privacy → Ads; iOS: Settings → Privacy → Tracking).
- **Bad Words Pack / profanity:** if purchased, this content can be toggled on or off at any time from within the Game.
- **Account data:** to request deletion of data tied to your linked account, contact us at the address below.

## 5. Children's Privacy

Brain Drain: Idle IQ is a satirical game with mature humor (including an optional purchasable profanity toggle) and is not directed at children. We do not knowingly collect personal information from anyone under 13 (or the equivalent minimum age in your country). If you believe a child has provided us with personal information, contact us and we'll delete it.

*Flag for Aceyfer: your Google Play / App Store age-rating answers (see the age-rating note in `TASKLIST_DETAILS.md` §15) need to be consistent with this section — if the rating questionnaire lands the Game above the "everyone/teen" line because of the Bad Words Pack, that's fine, but this section shouldn't simultaneously claim the Game is family-friendly.*

## 6. Data Retention & Security

We retain purchase and account-linking records for as long as needed to support your purchases (including refund/support requests) and to meet tax and legal obligations. We use reasonable technical measures to protect the data we hold, but no method of storage or transmission is 100% secure.

## 7. Changes to This Policy

We may update this policy as the Game changes. Material changes will be reflected by updating the effective date above.

## 8. Contact

Questions about this policy or your data: **[FILL IN — a support email or contact page]**

---

## Open items before this can actually be published

- **Contact email** — needs a real one. Using a personal Gmail address as the public support contact is a real option indie devs use, but worth a deliberate choice, not a default.
- **Ad network partner list (§3)** — Unity LevelPlay is a mediation layer, not a single ad network. Per `TASKLIST_DETAILS.md` §57, its Network Manager (which pulls in the actual ad SDKs) hasn't been run yet. Once it has, the specific networks it mediates to (e.g. AdMob, Meta Audience Network, etc., whichever get enabled) should each get a named line in §3 — regulators and both stores generally expect this level of specificity, not just "an ad network."
- **GDPR/consent (CMP) gap** — §57 explicitly flags that a consent-management platform (Google UMP or similar) has *not* been wired in yet. This policy describes the data collection as if ads are live; it should not be published as the Game's real policy in the EU/UK until that CMP work is actually done, or this policy will describe protections the Game doesn't yet have. Also relevant to Google Play's own "Ads" declaration and the EU-specific "Data safety" section in Play Console, which are separate from this document but need to match it.
- **Server-side save backend** — §12's account-linked save/entitlement backend (UGS Cloud Code, Aceyfer's to build) doesn't exist yet. The bracketed sentence in §1 should only be uncommented once it's real; publishing it before then would overstate what's actually stored.
- **Governing law / jurisdiction** — not addressed at all here; where Eighth Kind Studios is legally based should probably get a line, and this is exactly the kind of thing worth a few minutes with an actual lawyer or a template service (e.g. a game-specific privacy policy generator) rather than relying solely on this draft.
- **Hosting** — both Play Console and App Store Connect require this to live at a public URL, not just be an on-device file. Cheapest options: a GitHub Pages page, a free Google Sites page, or a page on whatever studio website Eighth Kind Studios ends up with.
