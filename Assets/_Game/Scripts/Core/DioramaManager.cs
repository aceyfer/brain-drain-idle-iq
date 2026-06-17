using UnityEngine;
using System;

namespace BrainDrain.Core
{
    public sealed class DioramaManager : MonoBehaviour
    {
        [Header("Diorama References")]
        [SerializeField] private GameObject[] dioramaObjects;

        public GameObject[] DioramaObjects
        {
            get => dioramaObjects;
            set => dioramaObjects = value;
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameInitialized += InitializeDioramas;
                InitializeDioramas();
            }
            else
            {
                InitializeDioramas();
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameInitialized -= InitializeDioramas;
            }
        }

        private void InitializeDioramas()
        {
            UnsubscribeFromEvents();

            var currency = CurrencyManager.Instance;
            if (currency != null)
            {
                UpdateActiveDiorama(currency.CumulativeBrains);
                currency.OnCumulativeBrainsChanged += UpdateActiveDiorama;
            }
        }

        private void UnsubscribeFromEvents()
        {
            var currency = CurrencyManager.Instance;
            if (currency != null)
            {
                currency.OnCumulativeBrainsChanged -= UpdateActiveDiorama;
            }
        }

        private void UpdateActiveDiorama(double cumulativeBrains)
        {
            if (dioramaObjects == null || dioramaObjects.Length == 0)
            {
                return;
            }

            int activeIndex = 0;
            if (GameManager.Instance != null)
            {
                var ranks = GameManager.Instance.RankDefinitions;
                if (ranks != null && ranks.Length > 0)
                {
                    for (int i = 0; i < ranks.Length; i++)
                    {
                        if (cumulativeBrains >= ranks[i].threshold)
                        {
                            activeIndex = i;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }

            for (int i = 0; i < dioramaObjects.Length; i++)
            {
                if (dioramaObjects[i] != null)
                {
                    dioramaObjects[i].SetActive(i == activeIndex);
                }
            }
        }
    }
}
