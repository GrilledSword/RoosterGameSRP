public interface ISaveable
{
    // Adatok összegyûjtése mentéshez
    void SaveData(ref GameData data);

    // Adatok betöltése és alkalmazása
    void LoadData(GameData data);
}
