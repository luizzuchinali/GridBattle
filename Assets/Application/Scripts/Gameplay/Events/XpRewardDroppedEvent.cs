using System;
using System.Collections.Generic;
using GridBattle.Gameplay.Progression;
using UnityEngine;

namespace GridBattle.Gameplay.Events
{
    /// <summary>
    /// Raised by XpRewardSystem when an enemy dies and drops XP for the player.
    /// The presentation layer (e.g. XpOrbsVfx) can take over delivery: it calls
    /// <see cref="MarkPresented"/> and then calls <see cref="Collect"/> for each
    /// packet when it "arrives". If nobody takes over, the XP is credited
    /// immediately.
    /// </summary>
    public class XpRewardDroppedEvent
    {
        private readonly Action<XpPacket> _collect;

        /// <summary>
        /// World-space position the XP came from.
        /// </summary>
        public Vector3 Origin { get; }

        public IReadOnlyList<XpPacket> Packets { get; }

        public bool IsPresented { get; private set; }

        public XpRewardDroppedEvent(Vector3 origin, IReadOnlyList<XpPacket> packets, Action<XpPacket> collect)
        {
            Origin = origin;
            Packets = packets;
            _collect = collect;
        }

        /// <summary>
        /// The presentation takes over delivery and becomes responsible for calling
        /// <see cref="Collect"/> for each packet.
        /// </summary>
        public void MarkPresented() => IsPresented = true;

        /// <summary>
        /// Credits the packet to the player.
        /// </summary>
        public void Collect(XpPacket packet) => _collect(packet);
    }
}
