using UnityEngine;
using System.Collections;

public class DataManagement : MonoBehaviour {    

    public static void SaveData() {
        PlayerPrefs.SetFloat("MASTER VOLUME", AudioListener.volume);
    }

    public static void LoadData() {
        PlayerPrefs.Save();
        //AudioListener.volume = PlayerPrefs.GetFloat("MASTER VOLUME");
    }
}
