using System;
using System.Collections.Generic;

namespace BrainDrain.Core.Events
{
    // -------------------------------------------------------------------------
    // Payload structs — where T : struct on EventBus<T> prevents boxing on Raise.
    // -------------------------------------------------------------------------

    public enum Currency { BrainPower, Cash, Points, Neurons }

    public struct CurrencyChanged
    {
        public Currency currency;
    }

    public struct ItemPurchased
    {
        public int itemId;
    }

    public struct StageAdvanced
    {
        public int    newStage;
        public int    newRank;
        public string title;
        public string cogsBeat;
    }

    // -------------------------------------------------------------------------
    // EventBus<T>
    //
    // Zero-alloc on Raise:
    //   - T : struct prevents boxing at the call site.
    //   - Indexed for-loop over List<Action<T>> allocates nothing per frame.
    //   - Subscribe guards duplicates via List.Contains (O(n) wire-up, not hot path).
    // -------------------------------------------------------------------------

    /// <summary>
    /// Static generic pub/sub for struct events. Zero-allocation payloads; simulation
    /// systems raise, UI/audio subscribe. No direct cross-system references.
    /// </summary>
    public static class EventBus<T> where T : struct
    {
        private static readonly List<Action<T>> Handlers = new List<Action<T>>(8);

        public static void Subscribe(Action<T> handler)
        {
            if (handler == null || Handlers.Contains(handler))
            {
                return;
            }

            Handlers.Add(handler);
        }

        public static void Unsubscribe(Action<T> handler)
        {
            if (handler == null)
            {
                return;
            }

            Handlers.Remove(handler);
        }

        public static void Raise(T payload)
        {
            for (int i = Handlers.Count - 1; i >= 0; i--)
            {
                Handlers[i](payload);
            }
        }
    }
}
