using System;
using System.Collections.Generic;
using UnityEngine;

namespace GridBattle.Managers.Audio
{
    /// <summary>
    /// How one <see cref="ESfx"/> sounds: its clips (one is picked at random each
    /// play) and the limits that stop it from stacking into noise.
    /// </summary>
    [Serializable]
    public sealed class SfxCue
    {
        [SerializeField]
        private ESfx sfx;

        [SerializeField]
        [Tooltip("Variations of the sound. One is picked at random each time (never the same twice in a row when there are several). Empty = silent.")]
        private List<AudioClip> clips = new();

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Volume of this effect, relative to the library SFX level and the player's volume option.")]
        private float volume = 1f;

        [SerializeField]
        [Range(0.5f, 2f)]
        [Tooltip("Base pitch (1 = original).")]
        private float pitch = 1f;

        [SerializeField]
        [Range(0f, 0.5f)]
        [Tooltip("Random pitch variation: each play uses base pitch x (1 +/- this value).")]
        private float pitchVariance = 0.05f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Minimum seconds between two plays of this effect. Requests that come sooner are dropped (avoids stacking when several units act at once).")]
        private float minInterval = 0.05f;

        [SerializeField]
        [Min(1)]
        [Tooltip("Maximum voices of this effect playing at the same time. Further requests are dropped until one ends.")]
        private int maxSimultaneous = 3;

        public SfxCue()
        {
        }

        /// <summary>A cue with sensible limits for <paramref name="sfx"/> and no clips yet.</summary>
        public SfxCue(ESfx sfx)
        {
            this.sfx = sfx;
            ApplyDefaults(sfx);
        }

        public ESfx Sfx => sfx;
        public IReadOnlyList<AudioClip> Clips => clips;
        public float Volume => volume;
        public float Pitch => pitch;
        public float PitchVariance => pitchVariance;
        public float MinInterval => minInterval;
        public int MaxSimultaneous => Mathf.Max(1, maxSimultaneous);

        /// <summary>At least one clip is assigned.</summary>
        public bool HasClips
        {
            get
            {
                for (var i = 0; i < clips.Count; i++)
                {
                    if (clips[i] != null) return true;
                }

                return false;
            }
        }

        /// <summary>Initial tuning per effect: rare stingers never stack, frequent effects are throttled.</summary>
        private void ApplyDefaults(ESfx effect)
        {
            switch (effect)
            {
                case ESfx.Walk:
                    Tune(0.6f, 0.08f, 0.08f, 2);
                    break;
                case ESfx.BasicAttack:
                    Tune(0.9f, 0.05f, 0.05f, 3);
                    break;
                case ESfx.SkillCast:
                    Tune(1f, 0.08f, 0.05f, 2);
                    break;
                case ESfx.PlayerHurt:
                    Tune(1f, 0.1f, 0.05f, 2);
                    break;
                case ESfx.EnemyHurt:
                    Tune(0.8f, 0.05f, 0.08f, 3);
                    break;
                case ESfx.CriticalHit:
                    Tune(1f, 0.15f, 0.03f, 1);
                    break;
                case ESfx.EnemyDeath:
                    Tune(0.9f, 0.06f, 0.08f, 3);
                    break;
                case ESfx.PlayerDeath:
                case ESfx.RunVictory:
                case ESfx.RunDefeat:
                case ESfx.ClassUnlocked:
                case ESfx.LevelUp:
                    Tune(1f, 0.5f, 0f, 1);
                    break;
                case ESfx.XpOrb:
                    Tune(0.5f, 0.04f, 0.1f, 4);
                    break;
                case ESfx.TalentOfferOpen:
                case ESfx.TalentChosen:
                    Tune(1f, 0.2f, 0f, 1);
                    break;
                case ESfx.Reroll:
                case ESfx.Ban:
                case ESfx.Skip:
                    Tune(0.8f, 0.1f, 0.03f, 1);
                    break;
                case ESfx.StateApplied:
                case ESfx.StateExpired:
                    Tune(0.7f, 0.1f, 0.05f, 2);
                    break;
                case ESfx.HazardCell:
                case ESfx.BonusCell:
                    Tune(0.8f, 0.15f, 0.03f, 1);
                    break;
                case ESfx.NodeSelect:
                    Tune(0.7f, 0.05f, 0.03f, 1);
                    break;
                case ESfx.NodeConfirm:
                    Tune(0.9f, 0.2f, 0.03f, 1);
                    break;
                case ESfx.Heal:
                    Tune(0.8f, 0.15f, 0.03f, 2);
                    break;
                case ESfx.ConsumableGained:
                case ESfx.ConsumableUse:
                    Tune(0.9f, 0.1f, 0.03f, 1);
                    break;
                case ESfx.ButtonTap:
                    Tune(0.6f, 0.03f, 0.03f, 2);
                    break;
                case ESfx.WindowOpen:
                case ESfx.WindowClose:
                    Tune(0.7f, 0.1f, 0.03f, 1);
                    break;
                case ESfx.ScreenTransition:
                    Tune(0.8f, 0.2f, 0.03f, 1);
                    break;
            }
        }

        private void Tune(float newVolume, float newMinInterval, float newPitchVariance, int newMaxSimultaneous)
        {
            volume = newVolume;
            minInterval = newMinInterval;
            pitchVariance = newPitchVariance;
            maxSimultaneous = newMaxSimultaneous;
        }
    }
}
