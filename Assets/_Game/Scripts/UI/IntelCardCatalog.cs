using System.Collections.Generic;

namespace BrainDrain.UI
{
    /// <summary>
    /// Single verbatim-copy source for THE LITERATES resistance cards shown across the §23 FTUE
    /// pass. Both FTUEManager (first-encounter delivery) and PocketPanelUI (§24c re-reading, THE
    /// POCKET) read every card's front/back/confirm text from here, so the copy lives in exactly
    /// one place in code. TASKLIST_DETAILS.md §23's "Creative package" section remains the source
    /// of truth ABOVE this file -- do NOT paraphrase when editing these constants; match §23 byte
    /// for byte. Only the five LiteratesCard-skin beats live here (Beats 2/3/5/7/9). The two
    /// COGSTerminal beats (Beat 1 boot, Beat 8 Snotting) are not resistance cards, never enter THE
    /// POCKET, and stay owned by FTUEManager.
    /// </summary>
    public static class IntelCardCatalog
    {
        /// <summary>One collectible resistance card's verbatim copy. Skin is always
        /// IntelCardSkin.LiteratesCard for every entry in this catalog.</summary>
        public readonly struct LiteratesCard
        {
            public readonly string Id;
            public readonly string Front;
            public readonly string Back;
            public readonly string Confirm;

            public LiteratesCard(string id, string front, string back, string confirm)
            {
                Id = id;
                Front = front;
                Back = back;
                Confirm = confirm;
            }
        }

        // ---- Stable card ids (save/derivation/display keys; never renumber) ----
        public const string GaryMattressId = "gary_mattress";        // Beat 2  (gated by ftueCard1Seen)
        public const string SnakeUttersId = "snake_utters";          // Beat 3  (gated by ftueCard2Seen)
        public const string ArmadilloSauceId = "armadillo_sauce";    // Beat 5  (gated by ftueCashBeatSeen)
        public const string CheeseDirtId = "cheese_dirt";            // Beat 7  (gated by ftueRestoreBeatSeen)
        public const string TedsCeilingFansId = "teds_ceiling_fans"; // Beat 9  THE NAME (gated by ftueNameRevealSeen)

        private static readonly Dictionary<string, LiteratesCard> Cards = new()
        {
            [GaryMattressId] = new LiteratesCard(
                GaryMattressId,
                "GARY'S DISCOUNT MATTRESS EMPORIUM — \"We Also Have Soup\"",
                "It calls it \"collection.\" Ask it who's collecting. It won't answer. Neither can we — not yet, not in writing, not this close to a fresh asset. But here's what the tin can doesn't know: every tap leaks a little light back into the world. Watch the sky. It remembers.\n" +
                "— The Literates\n" +
                "p.s. read it twice. reading twice is how we got like this. the good version of like this.",
                "I READ IT. ALL OF IT."),

            [SnakeUttersId] = new LiteratesCard(
                SnakeUttersId,
                "SNAKE UTTERS WHOLESALE — \"Ask About Our Utters\"",
                "Buildings make juice while you nap. COGS calls that \"theft of company time.\" Do it anyway — sleeping on the job is the only job worth having.\n" +
                "Buy cheap ones first. The math is friendlier. We checked. We're the last people who check math.\n" +
                "— TL",
                "MATH CONFIRMED"),

            [ArmadilloSauceId] = new LiteratesCard(
                ArmadilloSauceId,
                "ARMADILLO SAUCE LEGAL SERVICES — \"It Goes With Everything, Including Court\"",
                "It just told you not to convert, didn't it. Funny how the thing metering the street panics when you spend what's yours.\n" +
                "Convert. Buy. Repeat. That's the whole machine. Now it's your machine.\n" +
                "— TL",
                "MY MACHINE NOW"),

            [CheeseDirtId] = new LiteratesCard(
                CheeseDirtId,
                "CHEESE DIRT MEMORIAL FOUNDATION — \"Never Forget The Flavor\"",
                "Every point you put into the world makes the streets a little smarter and their grip a little weaker. They allow it because they think it's a rounding error.\n" +
                "Be a rounding error. Be the biggest rounding error they've ever seen.\n" +
                "— TL",
                "ROUNDING UP"),

            [TedsCeilingFansId] = new LiteratesCard(
                TedsCeilingFansId,
                "TED'S CEILING FANS & OTHER CEILING ITEMS — \"Look Up More\"",
                "You've tapped long enough. You've earned the word. The ones collecting — the ones that thing works for — are called the ILLUMISNOTTI. Old money. Older grudges. They drained the world stupid on purpose, and your little machine is a straw in everyone's skull. Now you know why the sky matters. Keep leaking light. Make the name useless.\n" +
                "— The Literates\n" +
                "p.s. memorize this card. then eat it. kidding. paper's valuable. hide it.",
                "I KNOW THE NAME"),
        };

        /// <summary>Look up one card's verbatim copy by id. Returns false for unknown ids
        /// (never expected for the five FTUE ids above; guards a future non-FTUE caller).</summary>
        public static bool TryGet(string id, out LiteratesCard card) => Cards.TryGetValue(id, out card);
    }
}
