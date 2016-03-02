using UnityEngine;
using System.Collections;

public class DayNightSelect : MonoBehaviour {
    public enum TimeOfDay { Day, Night}
    public TimeOfDay timeOfDay;
    public GameObject sun;
    public GameObject moon;
    public Material daySky;
    public Material nightSky;

	void Start () {        
        SetTime();
    }

    void OnValidate() {
        SetTime();
    }

    private void SetTime() {        
        if (timeOfDay == TimeOfDay.Day) {
            sun.SetActive(true);
            moon.SetActive(false);
            RenderSettings.ambientSkyColor = Color.white;
            RenderSettings.ambientIntensity = 0.4f;
            RenderSettings.reflectionIntensity = 0.2f;
            RenderSettings.skybox = daySky; 
            //RenderSettings.a
        } else if (timeOfDay == TimeOfDay.Night) {
            sun.SetActive(false);
            moon.SetActive(true);
            RenderSettings.ambientSkyColor = new Color32(125, 125, 130, 255);
            RenderSettings.ambientIntensity = 0.1f;
            RenderSettings.reflectionIntensity = 0.1f;
            RenderSettings.skybox = nightSky;
        }
    }

    public void toggleTime() {
        if (timeOfDay == TimeOfDay.Day) {
            timeOfDay = TimeOfDay.Night;
        } else {
            timeOfDay = TimeOfDay.Day;
        }
        SetTime();
    }
}
