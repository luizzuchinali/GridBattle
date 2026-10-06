using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Events;
using GridBattle.Gameplay.Turns;
using UnityEngine;

namespace GridBattle.Gameplay.Progression
{
    /// <summary>
    /// XP reward rule: when an enemy dies, the player receives the EnemyConfig's
    /// XpReward, split into packets. Delivery is announced via
    /// <see cref="XpRewardDroppedEvent"/> so the presentation controls the timing
    /// (each packet is credited when its orb reaches the bar); without a
    /// presentation, it is credited right away. While XP that will level the
    /// player up (or the XP of the battle's last enemy) is in flight, the turn
    /// flow is held (<see cref="TurnBlockers"/>) so the level up resolves first.
    /// </summary>
    public class XpRewardSystem : MonoBehaviour
    {
        [SerializeField]
        [Min(1)]
        [Tooltip("Maximum XP per packet. Each packet becomes an orb in the UI.")]
        private int xpPerPacket = 5;

        private void OnEnable()
        {
            EventBus.Subscribe<CharacterDiedEvent>(OnCharacterDied);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<CharacterDiedEvent>(OnCharacterDied);
        }

        private void OnCharacterDied(CharacterDiedEvent e)
        {
            if (e.Character is not Enemy enemy) return;

            var enemyConfig = enemy.EnemyConfig;
            if (enemyConfig == null) return;

            var player = FindAnyObjectByType<PlayerCharacter>();
            if (player == null) return;

            var xpToNextLevel = player.XpToNextLevel;
            if (xpToNextLevel <= 0) return;

            var reward = enemy.XpReward;
            if (reward <= 0) return;

            var packets = XpPacket.Split(reward, xpPerPacket, player.CurrentXp, xpToNextLevel);

            // At the level cap the XP gives no level, so only the battle's last kill holds the turn.
            var willLevelUp = !player.IsMaxLevel && player.CurrentXp + reward >= xpToNextLevel;
            var holdsTurn = willLevelUp || !BattleController.AnyEnemyAlive();
            var blocker = holdsTurn ? TurnBlockers.Acquire("XP delivery") : null;
            var pending = packets.Length;

            var drop = new XpRewardDroppedEvent(enemy.transform.position, packets, packet =>
            {
                if (player != null)
                    player.GainXp(packet.Amount);
                if (--pending == 0)
                    blocker?.Dispose();
            });

            EventBus.Raise(drop);
            if (drop.IsPresented) return;

            foreach (var packet in packets)
                drop.Collect(packet);
        }
    }
}
