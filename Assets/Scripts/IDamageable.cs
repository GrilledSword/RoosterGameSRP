public interface IDamageable
{
    /// <summary>
    /// Az objektum frakci�ja.
    /// </summary>
    Faction Faction { get; }

    /// <summary>
    /// Sebz�st alkalmaz az objektumon.
    /// </summary>
    /// <param name="damage">A sebz�s m�rt�ke.</param>
    /// <param name="sourceFaction">A sebz�st okoz� forr�s frakci�ja.</param>
    void TakeDamage(float damage, Faction sourceFaction);
}