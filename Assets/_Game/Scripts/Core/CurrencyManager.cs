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

            double multipliedAmount = amount * _rebirthMultiplier;

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

            currentCash += amount * cashMultiplier;
            OnCashChanged?.Invoke(currentCash);
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
            currentPoints += convertedAmount * pointsConversionRate;

            OnCashChanged?.Invoke(currentCash);
            OnPointsChanged?.Invoke(currentPoints);
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
            if (idleBpps > 0d)
            {
                AddBrainPower(idleBpps);
            }

            if (cashPerSecond > 0d)
            {
                AddCash(cashPerSecond);
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
    }
}
