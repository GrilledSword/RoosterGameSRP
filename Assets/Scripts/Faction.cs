[System.Flags]
public enum Faction
{
    None = 0,
    Player = 1 << 0,
    Enemy = 1 << 1,
    Environment = 1 << 2,
    Ally = 1 << 3
}