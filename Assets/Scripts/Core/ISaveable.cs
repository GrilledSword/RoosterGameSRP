public interface ISaveable
{
    // Adatok �sszegy�jt�se ment�shez
    void SaveData(ref GameData data);

    // Adatok bet�lt�se �s alkalmaz�sa
    void LoadData(GameData data);
}
