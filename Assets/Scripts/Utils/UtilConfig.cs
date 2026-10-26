using System;
using System.IO;
using UnityEngine;

public static class UtilConfig
{
    private static MainConfig data;

    public static void loadConfig(){
        string m_Path = Path.Combine(Application.dataPath, "Configuration", "config.json");
        try 
        {
            if (!File.Exists(m_Path))
            {
                throw new FileNotFoundException($"Konfiguračný súbor nebol nájdený na ceste: {m_Path}");
            }
            string content;

            using (var sr = new StreamReader(m_Path))
            {
                content = sr.ReadToEnd();
            }

            data = JsonUtility.FromJson<MainConfig>(content);

            if (data == null || data.resourceConfig == null) 
            {
                throw new Exception("JSON je neplatný alebo mu chýbajú kľúčové sekcie (workerConfig/resourceConfig).");
            }

            Debug.Log("JSON bol úspešne načítaný:\n" + data.ToString());
        }
        catch (FileNotFoundException ex)
        {
            Debug.LogError($"[Config Error] Súbor neexistuje: {ex.Message}");
        }
        catch (IOException ex)
        {
            Debug.LogError($"[Config Error] Problém so systémom súborov: {ex.Message}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Config Error] Neočakávaná chyba pri načítaní/validácii: {ex.Message}");
        }
    }

    public static int getCurrentEnergy()
    {
        if (data == null) {
            return -1;
        }
        return data.resourceConfig.currentEnergy;
    }

    public static int getMaxEnergy()
    {
        if (data == null) {
            return -1;
        }
        return data.resourceConfig.maxEnergy;
    }
    public static int getCoins()
    {
        if (data == null) {
            return -1;
        }
        return data.resourceConfig.coins;
    }
    public static int getMaxWorkers()
    {
        if (data == null) {
            return -1;
        }
        return data.workerConfig.maxWorkers;
    }

    public static MainConfig getConfig()
    {
    return data;
    }
}
