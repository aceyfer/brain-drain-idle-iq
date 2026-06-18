using System;
using UnityEngine;

namespace BrainDrain.Core
{
    /// <summary>
    /// Tracks soft (Brains) and premium (Neurons) currency and applies idle BPS income each second.
    /// </summary>
    public sealed class CurrencyManager : MonoBehaviour
    {
        private double brains;
        private double cumulativeBrains;
        private int neurons;
        private double idleBps;
        private double _rebirthMultiplier = 1.0;

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

        /// <summary>Current spendable Brains balance.</summary>
        public double Brains => brains;

        /// <summary>Lifetime Brains earned. Never decreases when spending.</summary>
        public double CumulativeBrains => cumulativeBrains;

        /// <summary>Current Neurons balance.</summary>
        public int Neurons => neurons;

        /// <summary>Total idle Brains generated per second from buildings and upgrades.</summary>
        public double IdleBPS => idleBps;

        /// <summary>Permanent income multiplier accumulated across rebirths.</summary>
        public double RebirthMultiplier => _rebirthMultiplier;

        /// <summary>Fired when the spendable Brains balance changes. Passes the new total.</summary>
        public event Action<double> OnBrainsChanged;

        /// <summary>Fired when lifetime Brains earned increases. Passes the new cumulative total.</summary>
        public event Action<double> OnCumulativeBrainsChanged;

        /// <summary>Fired when the Neurons balance changes. Passes the new total.</summary>
        public event Action<int> OnNeuronsChanged;

        private void Start()
        {
            SubscribeToGameTick();
        }

        private void OnDestroy()
        {
            UnsubscribeFromGameTick();
        }

        /// <summary>Adds idle Brains-per-second income from a building or upgrade.</summary>
        public void AddIdleBPS(double amount)
        {
            if (amount <= 0d)
            {
                return;
            }

            idleBps += amount;
        }

        /// <summary>Adds Brains to the player's balance and lifetime cumulative total.</summary>
        public void AddBrains(double amount)
        {
            if (amount <= 0d)
            {
                return;
            }

            double multipliedAmount = amount * _rebirthMultiplier;

            brains += multipliedAmount;
            cumulativeBrains += multipliedAmount;

            OnBrainsChanged?.Invoke(brains);
            OnCumulativeBrainsChanged?.Invoke(cumulativeBrains);
        }

        /// <summary>
        /// Performs a full progression wipe: zeroes spendable and cumulative Brains and
        /// permanently increases the rebirth income multiplier by <paramref name="multiplierBonus"/>.
        /// </summary>
        public void ExecuteRebirth(double multiplierBonus)
        {
            _rebirthMultiplier += multiplierBonus;

            brains = 0d;
            cumulativeBrains = 0d;

            OnBrainsChanged?.Invoke(brains);
            OnCumulativeBrainsChanged?.Invoke(cumulativeBrains);
        }

        /// <summary>
        /// Removes Brains from the player's spendable balance, clamped at zero. Unlike
        /// SpendBrains, this always succeeds (e.g. for event penalties) and never refuses
        /// due to insufficient funds. Does not affect CumulativeBrains.
        /// </summary>
        public void RemoveBrains(double amount)
        {
            if (amount <= 0d)
            {
                return;
            }

            brains = Math.Max(0d, brains - amount);
            OnBrainsChanged?.Invoke(brains);
        }

        /// <summary>
        /// Attempts to spend Brains. Returns true when the purchase succeeds.
        /// </summary>
        public bool SpendBrains(double amount)
        {
            if (amount <= 0d || brains < amount)
            {
                return false;
            }

            brains -= amount;
            OnBrainsChanged?.Invoke(brains);
            return true;
        }

        /// <summary>Returns true when the player has enough Brains for the given cost.</summary>
        public bool CanAffordBrains(double amount)
        {
            return amount > 0d && brains >= amount;
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
            if (idleBps <= 0d)
            {
                return;
            }

            AddBrains(idleBps);
        }
    }
}
