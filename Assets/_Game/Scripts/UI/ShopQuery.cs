using System;
using System.Collections.Generic;
using BrainDrain.Core;
using BrainDrain.Systems;

namespace BrainDrain.UI
{
    /// <summary>
    /// Read-only shop catalog and row-state snapshots for the 3-tab view layer.
    /// Invoked only from event-driven refresh paths — never polled from Update.
    /// </summary>
    public static class ShopQuery
    {
        public enum ShopTab
        {
            BpUpgrades = 0,
            CashInvestments = 1,
            RpRestorations = 2
        }

        public enum ShopRowState
        {
            Locked,
            Affordable,
            TooExpensive,
            Complete
        }

        public readonly struct ShopRowSnapshot
        {
            public readonly string ItemId;
            public readonly ShopTab Tab;
            public readonly ShopRowState State;
            public readonly string DisplayName;
            public readonly string Description;
            public readonly string CostLabel;
            public readonly string CountLabel;
            public readonly bool BuyInteractable;

            public ShopRowSnapshot(
                string itemId,
                ShopTab tab,
                ShopRowState state,
                string displayName,
                string description,
                string costLabel,
                string countLabel,
                bool buyInteractable)
            {
                ItemId = itemId;
                Tab = tab;
                State = state;
                DisplayName = displayName;
                Description = description;
                CostLabel = costLabel;
                CountLabel = countLabel;
                BuyInteractable = buyInteractable;
            }
        }

        private static readonly List<ShopRowSnapshot> RowBuffer = new(16);
        private static readonly List<BuildingData> SortedBuildingsBuffer = new(8);

        public static IReadOnlyList<ShopRowSnapshot> GetRows(ShopTab tab)
        {
            RowBuffer.Clear();

            switch (tab)
            {
                case ShopTab.BpUpgrades:
                    BuildBuildingRows(cashTab: false);
                    break;
                case ShopTab.CashInvestments:
                    BuildBuildingRows(cashTab: true);
                    break;
                case ShopTab.RpRestorations:
                    BuildRestorationRows();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(tab), tab, null);
            }

            return RowBuffer;
        }

        private static void BuildBuildingRows(bool cashTab)
        {
            UpgradeManager upgradeManager = UpgradeManager.Instance;
            CurrencyManager currency = CurrencyManager.Instance;
            if (upgradeManager == null)
            {
                return;
            }

            IReadOnlyList<BuildingData> templates = upgradeManager.BuildingTemplates;
            if (templates == null || templates.Count == 0)
            {
                return;
            }

            SortedBuildingsBuffer.Clear();
            for (int i = 0; i < templates.Count; i++)
            {
                BuildingData data = templates[i];
                if (data == null)
                {
                    continue;
                }

                bool isCash = UpgradeManager.IsCashCost(data);
                if (isCash != cashTab)
                {
                    continue;
                }

                SortedBuildingsBuffer.Add(data);
            }

            SortedBuildingsBuffer.Sort((a, b) =>
            {
                if (a == null) return b == null ? 0 : 1;
                if (b == null) return -1;
                return a.unlockCumulativeBrainPower.CompareTo(b.unlockCumulativeBrainPower);
            });

            ShopTab tab = cashTab ? ShopTab.CashInvestments : ShopTab.BpUpgrades;

            for (int i = 0; i < SortedBuildingsBuffer.Count; i++)
            {
                BuildingData data = SortedBuildingsBuffer[i];
                RowBuffer.Add(BuildBuildingSnapshot(data, upgradeManager, currency, tab));
            }
        }

        private static ShopRowSnapshot BuildBuildingSnapshot(
            BuildingData data,
            UpgradeManager upgradeManager,
            CurrencyManager currency,
            ShopTab tab)
        {
            bool unlocked = currency != null
                && currency.CumulativeBrainPower >= data.unlockCumulativeBrainPower;
            double cost = upgradeManager.GetCurrentCost(data);
            int level = upgradeManager.GetBuildingLevel(data);
            bool affordable = unlocked && upgradeManager.CanAffordBuilding(data);

            ShopRowState state = !unlocked
                ? ShopRowState.Locked
                : affordable
                    ? ShopRowState.Affordable
                    : ShopRowState.TooExpensive;

            string itemId = BuildBuildingItemId(data.buildingName);
            string displayName = unlocked ? data.buildingName : "??? CLASSIFIED ???";
            string description = unlocked
                ? BuildBuildingDescription(data, level, currency)
                : "Access restricted by the Ministry.";
            string costLabel = unlocked
                ? FormatBuildingCost(data, cost)
                : $"{NumberFormatter.Format(data.unlockCumulativeBrainPower)} BP REQUIRED";
            string countLabel = $"OWNED: {level}";
            bool buyInteractable = unlocked;

            return new ShopRowSnapshot(
                itemId,
                tab,
                state,
                displayName,
                description,
                costLabel,
                countLabel,
                buyInteractable);
        }

        private static string BuildBuildingDescription(BuildingData data, int level, CurrencyManager currency)
        {
            double bppsMult = currency != null
                ? currency.RebirthMultiplier * currency.ShopAllMultiplier
                : 1d;
            double cashMult = currency != null
                ? currency.CashMultiplier * currency.ShopCashMultiplier * currency.ShopAllMultiplier
                : 1d;

            double singleBpps = data.baseBrainPowerPerSecond * bppsMult;
            double singleCash = data.baseCashPerSecond * cashMult;

            string perLevel = string.Empty;
            if (data.baseBrainPowerPerSecond > 0d)
            {
                perLevel += $"+{NumberFormatter.Format(singleBpps)} BP/s";
            }

            if (data.baseCashPerSecond > 0d)
            {
                if (perLevel.Length > 0)
                {
                    perLevel += "  ";
                }

                perLevel += $"+${NumberFormatter.Format(singleCash)}/s";
            }

            string totalLine = string.Empty;
            if (level > 0)
            {
                double totalBpps = level * data.baseBrainPowerPerSecond * bppsMult;
                double totalCash = level * data.baseCashPerSecond * cashMult;

                string totalBp = data.baseBrainPowerPerSecond > 0d
                    ? $"+{NumberFormatter.Format(totalBpps)} BP/s"
                    : string.Empty;
                string totalCashLabel = data.baseCashPerSecond > 0d
                    ? $"+${NumberFormatter.Format(totalCash)}/s"
                    : string.Empty;
                string totalParts = totalBp.Length > 0 && totalCashLabel.Length > 0
                    ? $"{totalBp}  {totalCashLabel}"
                    : $"{totalBp}{totalCashLabel}";
                totalLine = $"\n<color=#FFD700>TOTAL ({level}×): {totalParts}</color>";
            }

            return $"{data.description}\n<color=#00F0FF><b>Per level: {perLevel}</b></color>{totalLine}";
        }

        private static string FormatBuildingCost(BuildingData data, double cost)
        {
            return UpgradeManager.IsCashCost(data)
                ? $"${NumberFormatter.Format(cost)}"
                : $"{NumberFormatter.Format(cost)} BP";
        }

        private static void BuildRestorationRows()
        {
            WorldRestorationManager restoration = WorldRestorationManager.Instance;
            CurrencyManager currency = CurrencyManager.Instance;
            if (restoration == null)
            {
                return;
            }

            IReadOnlyList<WorldRestorationStage> stages = restoration.Stages;
            for (int i = 0; i < stages.Count; i++)
            {
                WorldRestorationStage stage = stages[i];
                if (stage == null || stage.pointsRequired <= 0d)
                {
                    continue;
                }

                RowBuffer.Add(BuildRestorationSnapshot(stage, restoration, currency));
            }
        }

        private static ShopRowSnapshot BuildRestorationSnapshot(
            WorldRestorationStage stage,
            WorldRestorationManager restoration,
            CurrencyManager currency)
        {
            double cumulative = restoration.CumulativePointsSpentOnRestoration;
            double currentPoints = currency != null ? currency.CurrentPoints : 0d;
            double needed = stage.pointsRequired - cumulative;
            bool complete = needed <= 0d;
            bool affordable = !complete && currentPoints > 0d;

            ShopRowState state = complete
                ? ShopRowState.Complete
                : affordable
                    ? ShopRowState.Affordable
                    : ShopRowState.TooExpensive;

            string itemId = BuildRestorationItemId(stage.stageIndex);
            string displayName = stage.stageName.ToUpper();
            string description = complete
                ? "Restoration milestone reached."
                : $"Invest RP to advance the city toward Stage {stage.stageIndex + 1}.";
            string costLabel = complete
                ? "COMPLETE"
                : $"{NumberFormatter.Format(stage.pointsRequired)} RP TOTAL";

            return new ShopRowSnapshot(
                itemId,
                ShopTab.RpRestorations,
                state,
                displayName,
                description,
                costLabel,
                string.Empty,
                affordable);
        }

        public static string BuildBuildingItemId(string buildingName) => $"building:{buildingName}";

        public static string BuildRestorationItemId(int stageIndex) => $"restoration:{stageIndex}";
    }
}
