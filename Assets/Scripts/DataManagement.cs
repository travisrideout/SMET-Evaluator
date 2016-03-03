using UnityEngine;
using System.Collections;

public class DataManagement : MonoBehaviour {    

    /// <summary>
    /// Save data to player preferences
    /// </summary>
    public static void SaveData() {
        PlayerPrefs.SetFloat("MASTER VOLUME", AudioListener.volume);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Load data saved in player preferences
    /// </summary>
    public static void LoadData() {        
        AudioListener.volume = PlayerPrefs.GetFloat("MASTER VOLUME");
    }
}
