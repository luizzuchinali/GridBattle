namespace GridBattle.Gameplay.Terrain
{
    /// <summary>Category of a terrain cell (GDD Mechanic 5).</summary>
    public enum ETerrainKind
    {
        /// <summary>Blocked cell: nobody occupies or crosses it.</summary>
        Obstacle,

        /// <summary>Whoever is on it takes damage and/or a negative state.</summary>
        Hazard,

        /// <summary>Whoever is on it receives a beneficial state.</summary>
        Bonus
    }

    /// <summary>When a terrain cell applies its effect to the character standing on it.</summary>
    public enum ETerrainTrigger
    {
        /// <summary>Right when a character enters the cell (logical move).</summary>
        OnEnter,

        /// <summary>At the start of the standing character's own turn.</summary>
        OnTurnStart,

        /// <summary>At the end of the standing character's own turn.</summary>
        OnTurnEnd
    }
}
