namespace GridBattle.Gameplay
{
    /// <summary>
    /// Who is allowed to act right now.
    /// </summary>
    public enum ETurnOwner
    {
        Player,
        Enemies,

        /// <summary>Nobody: the battle's outcome is decided (or no battle is running).</summary>
        None
    }
}
