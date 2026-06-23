using System;
using UnityEngine;
using UnityEngine.Events;

namespace BrainDrain.Core
{
    /// <summary>Concrete UnityEvent subclass required for a double payload to show up in the Inspector.</summary>
    [Serializable]
    public sealed class CashChangedUnityEvent : UnityEvent<double> { }

    /// <summary>Concrete UnityEvent subclass required for a double payload to show up in the Inspector.</summary>
    [Serializable]
    public sealed class PointsChangedUnityEvent : UnityEvent<double> { }

    /// <summary>
    /// Tracks the three connected currency tiers (Brain Power, Cash, Points) plus premium
    /// Neurons, and applies idle BPPS/CPS income each second. Brain Power is the original,
    /// untouched tier; Cash and Points are an additive extension on top of it.
    /// </summary>
    public sealed class CurrencyManager : MonoBehaviour
    {
        private const float AutoConvertIntervalSeconds = 10f;

        private double brainPower;
        private double cumulativeBrainPower;
        private int neurons;
        private double idleBpps;
        private double _rebirthMultiplier = 1.0;

        private double cashPerSecond;
        private double currentCash;
        private double cashMultiplier = 1.0;

        private double currentPoints;
        private double pointsConversionRate = 0.1;

        private bool autoConvertCash;
        private float secondsSinceLastAutoConvert;

        // -- Illumisnotti rewrite (2026-06-21): Shop 2/Shop 3 permanent multiplier layers --
        // Separate from _rebirthMultiplier/cashMultiplier/pointsConversionRate so a player who
        // owns zero shop items behaves identically to before this pass (all start at the
        // neutral 1.0/0.0 value). Composed multiplicatively alongside the existing multipliers,
        // never replacing them.
        private double shopCashMultiplier = 1d;
        private double shopAllMultiplier = 1d;
        private double shopCashToPointsMultiplier = 1d;
        private double shopAllPointGainsMultiplier = 1d;

        // -- Illumisnotti rewrite: timed Illumisnotti-event modifiers --
        // Lazily expire on next read rather than via a dedicated tick subscription -- simpler,
        // and AddBrainPower/AddCash are called constantly (every tap and every second tick) so
        // staleness is never more than a frame or two.
        private double temporaryBrainPowerPercent;
        private float temporaryBrainPowerExpiryTime;
        private double temporaryAllMultiplierPercent;
        private float temporaryAllMultiplierExpiryTime;

        /// <summary>
        /// Hot Chick offline-BPPS-decay multiplier (added 2026-06-22), set once per load by
        /// SaveManager.ApplyLoadedDataToSystems based on real-world elapsed offline hours.
        /// Stacks multiplicatively with GetIQProductionMultiplier() rather than replacing it --
        /// starts at 1.0 (no effect) so a fresh save/a player who's never been offline behaves
        /// identically to before this feature existed. Applied to idle BPPS payout only, not
        /// Cash -- the feature is explicitly scoped to Brain-Power-per-second, per its own name.
        /// </summary>
        private float offlineBPPSMultiplier = 1f;

        /// <summary>Convenient accessor routed through GameManager when available.</summary>
        public static CurrencyManager Instance
        {
            get
            {
                if (GameManager.Instance != null)
                {
                    return GameManager.Instance.Currency;
                }

                return FindAnyObjectByType<CurrencyManager>();
            }
        }

        /// <summary>Current spendable Brain Power balance.</summary>
        public double BrainPower => brainPower;

        /// <summary>Lifetime Brain Power earned. Never decreases when spending.</summary>
        public double CumulativeBrainPower => cumulativeBrainPower;

        /// <summary>Current Neurons balance.</summary>
        public int Neurons => neurons;

        /// <summary>Total idle Brain Power generated per second from buildings and upgrades.</summary>
        public double IdleBPPS => idleBpps;

        /// <summary>Permanent income multiplier accumulated across rebirths.</summary>
        public double RebirthMultiplier => _rebirthMultiplier;

        /// <summary>Total Cash generated per second from Underground Economy (and any future Cash buildings).</summary>
        public double CashPerSecond => cashPerSecond;

        /// <summary>Current spendable/convertible Cash balance.</summary>
        public double CurrentCash => currentCash;

        /// <summary>Permanent Cash income multiplier accumulated across rebirths.</summary>
        public double CashMultiplier => cashMultiplier;

        /// <summary>Current Points balance, the third currency tier.</summary>
        public double CurrentPoints => currentPoints;

        /// <summary>How many Points one Cash converts into. Improves per rebirth.</summary>
        public double PointsConversionRate => pointsConversionRate;

        /// <summary>Permanent Cash-per-second multiplier from owned Cash Shop items/companion tiers. Starts at 1.0 (no effect).</summary>
        public double ShopCashMultiplier => shopCashMultiplier;

        /// <summary>Permanent multiplier applied to both Brain Power and Cash production from "all multipliers" Cash Shop items, plus The Grand Snotting's 10x capstone. Starts at 1.0 (no effect).</summary>
        public double ShopAllMultiplier => shopAllMultiplier;

        /// <summary>Permanent Cash-to-Points conversion multiplier from Points Shop items (e.g. The Snotty Guard). Starts at 1.0 (no effect). Distinct from pointsConversionRate's own additive bonuses.</summary>
        public double ShopCashToPointsMultiplier => shopCashToPointsMultiplier;

        /// <summary>Permanent multiplier on all Points gained via conversion, from Points Shop items (e.g. Dream Insertion Broadcast). Starts at 1.0 (no effect).</summary>
        public double ShopAllPointGainsMultiplier => shopAllPointGainsMultiplier;

        /// <summary>Current Hot Chick offline-BPPS-decay multiplier (1.0 = no decay, set by SaveManager on load).</summary>
        public float OfflineBPPSMultiplier => offlineBPPSMultiplier;

        /// <summary>If true, all current Cash is automatically converted to Points every 10 seconds.</summary>
        public bool AutoConvertCash
        {
            get => autoConvertCash;
            set => autoConvertCash = value;
        }

        /// <summary>Fired when the spendable Brain Power balance changes. Passes the new total.</summary>
        public event Action<double> OnBrainPowerChanged;

        /// <summary>Fired when lifetime Brain Power earned increases. Passes the new cumulative total.</summary>
        public event Action<double> OnCumulativeBrainPowerChanged;

        /// <summary>Fired when the Neurons balance changes. Passes the new total.</summary>
        public event Action<int> OnNeuronsChanged;

        /// <summary>
        /// Fired exactly once, the first time Brain Power is ever earned. In practice this is
        /// always the player's first tap, since idle/event income require already owning Brain
        /// Power first.
        /// </summary>
        public event Action OnFirstBrainPowerEarned;

        /// <summary>
        /// Fired when the Cash balance changes. UnityEvent (not a C# event, unlike everything
        /// else above) per the explicit spec for this tier, so it's wireable from the Inspector.
        /// </summary>
        public CashChangedUnityEvent OnCashChanged = new();

        /// <summary>Fired when the Points balance changes. UnityEvent for the same reason as OnCashChanged.</summary>
        public PointsChangedUnityEvent OnPointsChanged = new();

        /// <summary>
        /// Fired whenever ConvertCashToPoints actually converts something (not on a no-op call
        /// with zero Cash available). Passes the amount of Cash converted -- used for narrator
        /// commentary on the CONVERT button. Also fires from the 10s auto-convert tick if
        /// AutoConvertCash is on, since both paths call this same method; harmless today since
        /// no UI exposes that toggle yet, so in practice this only ever fires from the button.
        /// </summary>
        public event Action<double> OnCashConverted;

        private bool hasEarnedFirstBrainPower;

        private void Start()
        {
            SubscribeToGameTick();
        }

        private void OnDestroy()
        {
            UnsubscribeFromGameTick();
        }

        /// <summary>Adds idle Brain-Power-per-second income from a building or upgrade.</summary>
        public void AddIdleBPPS(double amount)
        {
            if (amount <= 0d)
            {
                return;
            }

            idleBpps += amount;
        }

        /// <summary>Adds idle Cash-per-second income from a building (e.g. Underground Economy).</summary>
        public void AddCashPerSecond(double amount)
        {
            if (amount <= 0d)
            {
                return;
            }

            cashPerSecond += amount;
        }

        /// <summary>Adds Brain Power to the player's balance and lifetime cumulative total.</summary>
        public void AddBrainPower(double amount)
        {
            if (amount <= 0d)
            {
                return;
            }

            double multipliedAmount = amount * _rebirthMultiplier * shopAllMultiplier
                * GetTemporaryBrainPowerFactor() * GetTemporaryAllMultiplierFactor();

            brainPower += multipliedAmount;
            cumulativeBrainPower += multipliedAmount;

            OnBrainPowerChanged?.Invoke(brainPower);
            OnCumulativeBrainPowerChanged?.Invoke(cumulativeBrainPower);

            if (!hasEarnedFirstBrainPower)
            {
                hasEarnedFirstBrainPower = true;
                OnFirstBrainPowerEarned?.Invoke();
            }
        }

        /// <summary>Adds Cash to the player's balance, scaled by the permanent Cash rebirth multiplier.</summary>
        public void AddCash(double amount)
        {
            if (amount <= 0d)
            {
                return;
            }

            currentCash += amount * cashMultiplier * shopCashMultiplier * shopAllMultiplier * GetTemporaryAllMultiplierFactor();
            OnCashChanged?.Invoke(currentCash);
        }

        // -- Illumisnotti rewrite (2026-06-21): Shop 2/Shop 3 permanent multiplier grants --
        // All additive-stacking, mirroring ExecuteRebirth's existing "+= bonus" pattern.

        /// <summary>Permanently increases the Cash-per-second multiplier (Hot Chick tiers, Shivering Designer Micro-Dog).</summary>
        public void AddShopCashMultiplierBonus(double percent) => shopCashMultiplier += percent;

        /// <summary>Permanently increases the "all multipliers" bonus (Private VIP Velvet Rope, Solid Gold Gaming Throne).</summary>
        public void AddShopAllMultiplierBonus(double percent) => shopAllMultiplier += percent;

        /// <summary>Multiplicative jump to the "all multipliers" bonus -- used by The Grand Snotting's literal 10x capstone, distinct from the other items' additive percents.</summary>
        public void MultiplyShopAllMultiplier(double factor) => shopAllMultiplier *= factor;

        /// <summary>Permanently increases the Cash-to-Points conversion multiplier (The Snotty Guard). Distinct from pointsConversionRate's own bonuses below.</summary>
        public void AddShopCashToPointsBonus(double percent) => shopCashToPointsMultiplier += percent;

        /// <summary>Permanently increases the multiplier on all Points gained via conversion (Dream Insertion Broadcast).</summary>
        public void AddShopAllPointGainsBonus(double percent) => shopAllPointGainsMultiplier += percent;

        /// <summary>Permanently increases pointsConversionRate directly -- the same mechanism ExecuteRebirth uses internally, exposed publicly for Points Shop items (Snott County Redistricting, Illumisnotti Leak Network, Snott Family Crest Takeover).</summary>
        public void AddPointsConversionRateBonus(double bonus) => pointsConversionRate += bonus;

        /// <summary>Restores the four shop multiplier layers from a save file. Kept separate from LoadState (whose 8-param signature predates this system) rather than extending it.</summary>
        public void LoadShopMultipliers(double restoredShopCash, double restoredShopAll, double restoredShopCashToPoints, double restoredShopAllPointGains)
        {
            shopCashMultiplier = restoredShopCash;
            shopAllMultiplier = restoredShopAll;
            shopCashToPointsMultiplier = restoredShopCashToPoints;
            shopAllPointGainsMultiplier = restoredShopAllPointGains;
        }

        /// <summary>
        /// Sets the Hot Chick offline-BPPS-decay multiplier directly. Called once by
        /// SaveManager on load (after computing it from elapsed offline hours) and once by
        /// CompanionManager on every Hot Chick purchase (reset to 1.0, since buying one
        /// immediately extends the decay window with zero elapsed time against it).
        /// </summary>
        public void SetOfflineBPPSMultiplier(float multiplier)
        {
            offlineBPPSMultiplier = multiplier;
        }

        // -- Illumisnotti rewrite: timed Illumisnotti random-event modifiers --

        /// <summary>Applies a temporary percent modifier to Brain Power production (both tap and idle, since both flow through AddBrainPower) for the given duration. Used by "BP drops" style events (e.g. Propaganda Broadcast). A new call replaces any still-active one rather than stacking.</summary>
        public void ApplyTemporaryBrainPowerModifier(double percent, float durationSeconds)
        {
            temporaryBrainPowerPercent = percent;
            temporaryBrainPowerExpiryTime = Time.time + durationSeconds;
        }

        /// <summary>Applies a temporary percent modifier to both Brain Power AND Cash production for the given duration. Used by "all multipliers" style events (e.g. Lord Snott Tantrum). A new call replaces any still-active one rather than stacking.</summary>
        public void ApplyTemporaryAllMultiplierModifier(double percent, float durationSeconds)
        {
            temporaryAllMultiplierPercent = percent;
            temporaryAllMultiplierExpiryTime = Time.time + durationSeconds;
        }

        private double GetTemporaryBrainPowerFactor()
        {
            if (temporaryBrainPowerPercent == 0d || Time.time >= temporaryBrainPowerExpiryTime)
            {
                temporaryBrainPowerPercent = 0d;
                return 1d;
            }

            return 1d + temporaryBrainPowerPercent;
        }

        private double GetTemporaryAllMultiplierFactor()
        {
            if (temporaryAllMultiplierPercent == 0d || Time.time >= temporaryAllMultiplierExpiryTime)
            {
                temporaryAllMultiplierPercent = 0d;
                return 1d;
            }

            return 1d + temporaryAllMultiplierPercent;
        }

        /// <summary>
        /// Reversibly subtracts from idle Brain-Power-per-second income (floored at 0) --
        /// used by the "lock one random building" Illumisnotti events to temporarily suppress
        /// exactly that building's contribution. Pair with RestoreIdleBPPS once the lock expires.
        /// Distinct from AddIdleBPPS, which is the permanent purchase-time grant.
        /// </summary>
        public void SuppressIdleBPPS(double amount) => idleBpps = Math.Max(0d, idleBpps - amount);

        /// <summary>Reverses a prior SuppressIdleBPPS call once a building lock expires.</summary>
        public void RestoreIdleBPPS(double amount) => idleBpps += amount;

        /// <summary>Reversibly subtracts from Cash-per-second income (floored at 0). Pair with RestoreCashPerSecond.</summary>
        public void SuppressCashPerSecond(double amount) => cashPerSecond = Math.Max(0d, cashPerSecond - amount);

        /// <summary>Reverses a prior SuppressCashPerSecond call.</summary>
        public void RestoreCashPerSecond(double amount) => cashPerSecond += amount;

        /// <summary>
        /// Adds Points directly. Unlike Brain Power/Cash, Points previously only ever changed
        /// via ConvertCashToPoints -- added for DebugCheats, since there was no direct way to
        /// grant Points for testing without reverse-calculating a Cash amount against the
        /// current (rebirth-tier-dependent) conversion rate.
        /// </summary>
        public void AddPoints(double amount)
        {
            if (amount <= 0d)
            {
                return;
            }

            currentPoints += amount;
            OnPointsChanged?.Invoke(currentPoints);
        }

        /// <summary>
        /// Converts up to <paramref name="amount"/> Cash into Points at the current conversion
        /// rate. Clamped to however much Cash is actually available (never refuses outright);
        /// returns false only if there's nothing to convert. Fires both OnCashChanged and
        /// OnPointsChanged, since both balances change.
        /// </summary>
        public bool ConvertCashToPoints(double amount)
        {
            double convertedAmount = Math.Min(amount, currentCash);
            if (convertedAmount <= 0d)
            {
                return false;
            }

            currentCash -= convertedAmount;
            currentPoints += convertedAmount * pointsConversionRate * shopCashToPointsMultiplier * shopAllPointGainsMultiplier;

            OnCashChanged?.Invoke(currentCash);
            OnPointsChanged?.Invoke(currentPoints);
            OnCashConverted?.Invoke(convertedAmount);
            return true;
        }

        /// <summary>
        /// Performs a full progression wipe: zeroes spendable and cumulative Brain Power and idle
        /// BPPS (so reset building tiers stop contributing income), and permanently increases
        /// the Brain Power, Cash, and Points-conversion rebirth bonuses. Cash/Points balances
        /// themselves are intentionally left untouched -- only Brain Power and building tiers
        /// reset on Rebirth per spec.
        /// </summary>
        public void ExecuteRebirth(double multiplierBonus, double cashMultiplierBonus, double pointsConversionRateBonus)
        {
            _rebirthMultiplier += multiplierBonus;
            cashMultiplier += cashMultiplierBonus;
            pointsConversionRate += pointsConversionRateBonus;

            brainPower = 0d;
            cumulativeBrainPower = 0d;
            idleBpps = 0d;

            OnBrainPowerChanged?.Invoke(brainPower);
            OnCumulativeBrainPowerChanged?.Invoke(cumulativeBrainPower);
        }

        /// <summary>
        /// Directly restores all currency-tier state from a save file. Bypasses AddBrainPower's/
        /// AddCash's rebirth-multiplier scaling, since these are already-final saved values, not
        /// new income.
        /// </summary>
        public void LoadState(
            double restoredBrainPower,
            double restoredCumulativeBrainPower,
            double restoredRebirthMultiplier,
            double restoredCash,
            double restoredCashMultiplier,
            double restoredPoints,
            double restoredPointsConversionRate,
            bool restoredAutoConvertCash)
        {
            brainPower = restoredBrainPower;
            cumulativeBrainPower = restoredCumulativeBrainPower;
            _rebirthMultiplier = restoredRebirthMultiplier;

            currentCash = restoredCash;
            cashMultiplier = restoredCashMultiplier;
            currentPoints = restoredPoints;
            pointsConversionRate = restoredPointsConversionRate;
            autoConvertCash = restoredAutoConvertCash;

            // A restored save with existing progress means the player has already had their
            // "first tap" in an earlier session; don't let the next real tap re-fire it.
            if (cumulativeBrainPower > 0d)
            {
                hasEarnedFirstBrainPower = true;
            }

            OnBrainPowerChanged?.Invoke(brainPower);
            OnCumulativeBrainPowerChanged?.Invoke(cumulativeBrainPower);
            OnCashChanged?.Invoke(currentCash);
            OnPointsChanged?.Invoke(currentPoints);
        }

        /// <summary>
        /// Spends Brain Power on infrastructure, converting the cost 1:1 into permanent
        /// PlayerIQ. Returns true when the spend succeeds; insufficient Brain Power
        /// leaves both values unchanged.
        /// </summary>
        public bool SpendBrainPowerOnInfrastructure(double cost)
        {
            if (!SpendBrainPower(cost))
            {
                return false;
            }

            PlayerIQManager.Instance?.ModifyPlayerIQ((float)cost);
            return true;
        }

        /// <summary>
        /// Removes Brain Power from the player's spendable balance, clamped at zero. Unlike
        /// SpendBrainPower, this always succeeds (e.g. for event penalties) and never refuses
        /// due to insufficient funds. Does not affect CumulativeBrainPower.
        /// </summary>
        public void RemoveBrainPower(double amount)
        {
            if (amount <= 0d)
            {
                return;
            }

            brainPower = Math.Max(0d, brainPower - amount);
            OnBrainPowerChanged?.Invoke(brainPower);
        }

        /// <summary>
        /// Attempts to spend Brain Power. Returns true when the purchase succeeds.
        /// </summary>
        public bool SpendBrainPower(double amount)
        {
            if (amount <= 0d || brainPower < amount)
            {
                return false;
            }

            brainPower -= amount;
            OnBrainPowerChanged?.Invoke(brainPower);
            return true;
        }

        /// <summary>Returns true when the player has enough Brain Power for the given cost.</summary>
        public bool CanAffordBrainPower(double amount)
        {
            return amount > 0d && brainPower >= amount;
        }

        /// <summary>
        /// Attempts to spend Cash (e.g. on Cash Shop items). Returns true when the spend
        /// succeeds. Mirrors SpendBrainPower's validate-then-spend behavior. Added 2026-06-21 --
        /// Cash previously only ever decreased via ConvertCashToPoints' internal subtraction,
        /// with no general-purpose spend method, since nothing spent Cash directly until now.
        /// </summary>
        public bool SpendCash(double amount)
        {
            if (amount <= 0d || currentCash < amount)
            {
                return false;
            }

            currentCash -= amount;
            OnCashChanged?.Invoke(currentCash);
            return true;
        }

        /// <summary>Returns true when the player has enough Cash for the given cost.</summary>
        public bool CanAffordCash(double amount)
        {
            return amount > 0d && currentCash >= amount;
        }

        /// <summary>
        /// Attempts to spend Points (e.g. on World Restoration). Returns true when the spend
        /// succeeds. Mirrors SpendBrainPower's validate-then-spend behavior.
        /// </summary>
        public bool SpendPoints(double amount)
        {
            if (amount <= 0d || currentPoints < amount)
            {
                return false;
            }

            currentPoints -= amount;
            OnPointsChanged?.Invoke(currentPoints);
            return true;
        }

        /// <summary>Returns true when the player has enough Points for the given cost.</summary>
        public bool CanAffordPoints(double amount)
        {
            return amount > 0d && currentPoints >= amount;
        }

        /// <summary>Adds Neurons to the player's balance.</summary>
        public void AddNeurons(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            neurons += amount;
            OnNeuronsChanged?.Invoke(neurons);
        }

        /// <summary>
        /// Attempts to spend Neurons. Returns true when the purchase succeeds.
        /// </summary>
        public bool SpendNeurons(int amount)
        {
            if (amount <= 0 || neurons < amount)
            {
                return false;
            }

            neurons -= amount;
            OnNeuronsChanged?.Invoke(neurons);
            return true;
        }

        /// <summary>Returns true when the player has enough Neurons for the given cost.</summary>
        public bool CanAffordNeurons(int amount)
        {
            return amount > 0 && neurons >= amount;
        }

        private void SubscribeToGameTick()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogError("[CurrencyManager] GameManager.Instance is null; cannot subscribe to tick.", this);
                return;
            }

            GameManager.Instance.OnSecondTick -= HandleSecondTick;
            GameManager.Instance.OnSecondTick += HandleSecondTick;
        }

        private void UnsubscribeFromGameTick()
        {
            if (GameManager.Instance == null)
            {
                return;
            }

            GameManager.Instance.OnSecondTick -= HandleSecondTick;
        }

        private void HandleSecondTick()
        {
            double productionMultiplier = GetIQProductionMultiplier();

            if (idleBpps > 0d)
            {
                // offlineBPPSMultiplier stacks with the IQ multiplier rather than replacing it
                // (effectiveMultiplier = iqMultiplier * offlineBPPSMultiplier), per spec -- BPPS
                // payout only, not Cash, since this feature is explicitly Brain-Power-scoped.
                AddBrainPower(idleBpps * productionMultiplier * offlineBPPSMultiplier);
            }

            if (cashPerSecond > 0d)
            {
                AddCash(cashPerSecond * productionMultiplier);
            }

            if (autoConvertCash)
            {
                secondsSinceLastAutoConvert += 1f;
                if (secondsSinceLastAutoConvert >= AutoConvertIntervalSeconds)
                {
                    secondsSinceLastAutoConvert = 0f;
                    ConvertCashToPoints(currentCash);
                }
            }
        }

        /// <summary>
        /// Idle BPPS/CPS payout is scaled by current PlayerIQ / 100, clamped to a max of 1 so IQ
        /// growing past 100 during play never boosts production above normal -- only a decayed
        /// IQ (below 100, only possible right after the offline-decay-on-load hook) reduces it.
        /// This is what makes returning from being offline cost something real: idle income
        /// comes back at a reduced rate (down to 60% at the decay floor) until tapping restores
        /// PlayerIQ to 100, rather than crediting a full 100% idle gain the instant the app
        /// reopens. Tap income itself is untouched -- taps are how the player recovers IQ in the
        /// first place, so they must stay at full value.
        /// </summary>
        private static double GetIQProductionMultiplier()
        {
            PlayerIQManager iqManager = PlayerIQManager.Instance;
            if (iqManager == null)
            {
                return 1d;
            }

            double normalized = iqManager.PlayerIQ / 100d;
            if (normalized > 1d) return 1d;
            if (normalized < 0d) return 0d;
            return normalized;
        }
    }
}
