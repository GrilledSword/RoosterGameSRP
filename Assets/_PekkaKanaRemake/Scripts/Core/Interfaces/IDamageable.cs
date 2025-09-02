public interface IDamageable
{
    /// <summary>
    /// Az objektum frakciója.
    /// </summary>
    Faction Faction { get; }

    /// <summary>
    /// Sebzést alkalmaz az objektumon.
    /// </summary>
    /// <param name="damage">A sebzés mértéke.</param>
    /// <param name="sourceFaction">A sebzést okozó forrás frakciója.</param>
    void TakeDamage(float damage, Faction sourceFaction);
}