using UnityEngine;

namespace GridBattle.Gameplay.Progression
{
    /// <summary>
    /// A fraction of an XP reward. The reward is credited packet by packet
    /// (one "orb" per packet in the UI), so the bar fills gradually.
    /// </summary>
    public readonly struct XpPacket
    {
        public int Amount { get; }

        /// <summary>
        /// Bar progress (0..1) after crediting this packet and the previous ones,
        /// ignoring level wrap-around.
        /// </summary>
        public float ProgressAfter { get; }

        public XpPacket(int amount, float progressAfter)
        {
            Amount = amount;
            ProgressAfter = progressAfter;
        }

        /// <summary>
        /// Splits <paramref name="reward"/> into ceil(reward / xpPerPacket) packets
        /// (minimum 1). The division remainder goes 1 by 1 to the first packets, so
        /// no XP is lost to rounding.
        /// </summary>
        public static XpPacket[] Split(int reward, int xpPerPacket, int currentXp, int xpToNextLevel)
        {
            var count = Mathf.Max(1, Mathf.CeilToInt((float)reward / xpPerPacket));
            var perPacket = reward / count;
            var remainder = reward % count;

            var packets = new XpPacket[count];
            var accumulated = 0;
            for (var i = 0; i < count; i++)
            {
                var amount = perPacket + (i < remainder ? 1 : 0);
                accumulated += amount;

                var progressAfter = Mathf.Clamp01((float)(currentXp + accumulated) / xpToNextLevel);
                packets[i] = new XpPacket(amount, progressAfter);
            }

            return packets;
        }
    }
}
