using System.IO;
using UnityEngine;
using JohnnyGame.Core;
using JohnnyGame.Data;
using JohnnyGame.Debugging;

namespace JohnnyGame.Simulation
{
    /// <summary>
    /// JSON-based save implementation using JsonUtility.
    /// File location: Application.persistentDataPath/save.json
    /// Falls back gracefully if the save file is corrupt.
    /// </summary>
    public sealed class JsonSave : ISave
    {
        private string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

        public void Save(SaveData data)
        {
            try
            {
                string json = JsonUtility.ToJson(data, prettyPrint: true);
                File.WriteAllText(SavePath, json);
                GameLogger.Log(GameLogger.Category.Save, $"Saved to {SavePath}");
            }
            catch (System.Exception ex)
            {
                GameLogger.LogError(GameLogger.Category.Save, $"Save failed: {ex.Message}");
            }
        }

        public SaveData Load()
        {
            if (!HasSave()) return null;

            try
            {
                string json = File.ReadAllText(SavePath);
                var data    = JsonUtility.FromJson<SaveData>(json);

                if (data == null)
                {
                    GameLogger.LogWarning(GameLogger.Category.Save, "Save file was empty or invalid — starting fresh.");
                    return null;
                }

                GameLogger.Log(GameLogger.Category.Save, $"Loaded save v{data.schemaVersion} from {SavePath}");
                return data;
            }
            catch (System.Exception ex)
            {
                GameLogger.LogError(GameLogger.Category.Save, $"Load failed ({ex.Message}) — starting fresh.");
                return null;
            }
        }

        public bool HasSave() => File.Exists(SavePath);

        public void DeleteSave()
        {
            if (HasSave())
            {
                File.Delete(SavePath);
                GameLogger.Log(GameLogger.Category.Save, "Save file deleted.");
            }
        }
    }
}
