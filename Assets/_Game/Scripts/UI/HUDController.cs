using UnityEngine;
using TMPro;
using BrainDrain.Core;
using System;

namespace BrainDrain.UI
{
    public sealed class HUDController : MonoBehaviour
    {
        [Header("UI Text Fields")]
        [SerializeField] private TextMeshProUGUI capacityText;
        [SerializeField] private TextMeshProUGUI iqText;
        [SerializeField] private TextMeshProUGUI rankText;

        public TextMeshProUGUI CapacityText
        {
            get => capacityText;
            set => capacityText = value;
        }

        public TextMeshProUGUI IQText
        {
            get => iqText;
            set => iqText = value;
        }

        public TextMeshProUGUI RankText
        {
            get => rankText;
            set => rankText = value;
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameInitialized += InitializeHUD;
                InitializeHUD();
            }
            else
            {
                InitializeHUD();
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameInitialized -= InitializeHUD;
            }
        }

        private void InitializeHUD()
        {
            UnsubscribeFromEvents();

            var currency = CurrencyManager.Instance;
            if (currency != null)
            {
                UpdateCapacityText(currency.Brains);
                UpdateRankText(currency.CumulativeBrains);
                currency.OnBrainsChanged += UpdateCapacityText;
                currency.OnCumulativeBrainsChanged += UpdateRankText;
            }

            var decay = FindAnyObjectByType<IQDecaySystem>();
            if (decay != null)
            {
                UpdateIQText(decay.CurrentIQ);
                decay.OnIQChanged += UpdateIQText;
            }
        }

        private void UnsubscribeFromEvents()
        {
            var currency = CurrencyManager.Instance;
            if (currency != null)
            {
                currency.OnBrainsChanged -= UpdateCapacityText;
                currency.OnCumulativeBrainsChanged -= UpdateRankText;
            }

            var decay = FindAnyObjectByType<IQDecaySystem>();
            if (decay != null)
            {
                decay.OnIQChanged -= UpdateIQText;
            }
        }

        private void UpdateCapacityText(double brains)
        {
            if (capacityText != null)
            {
                double percent = Math.Min(100.0, (brains / 500000.0) * 100.0);
                capacityText.text = $"{percent:F1}% ABSORBED";
            }
        }

        private void UpdateIQText(float iq)
        {
            if (iqText != null)
            {
                iqText.text = $"IQ: {iq:F0}";
            }
        }

        private void UpdateRankText(double cumulativeBrains)
        {
            if (rankText != null && GameManager.Instance != null)
            {
                rankText.text = GameManager.Instance.GetRankName(cumulativeBrains).ToUpper();
            }
        }
    }
}

