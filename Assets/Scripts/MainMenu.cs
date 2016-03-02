using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour {
    public GameObject[] payload;
    public RectTransform mainPanel;
    public RectTransform optionsPanel;
    public Button optionsButton;
    public RectTransform controlsPanel;
    public Button controlsButton;
    public RectTransform selectMissionPanel;
    public Button selectMissionButton;    
    public Button[] missions;

    private int payloadIndex = -1;
    private bool optionsSelected = false;
    private bool controlsSelected = false;
    private bool selectMissionSelected = false;
    private int levelIndex = 0;

    void Start () {
        DataManagement.LoadData();
        InitData();
        CameraFade.StartAlphaFade(Color.black, true, 2);
	    StartCoroutine(PayloadCycle());  
    } 

    private void InitData() {
        optionsPanel.transform.FindChild("Master Volume").GetComponent<Slider>().value = AudioListener.volume;
        Debug.Log("Initalizing saved data");
    }

    // loop through showing payloads
    IEnumerator PayloadCycle() {
        if (payloadIndex != -1) {
            payload[payloadIndex].SetActive(true);
        } 
        yield return new WaitForSeconds(5f);
        if (payloadIndex != -1) {
            payload[payloadIndex].SetActive(false);
        }
        if (payloadIndex < payload.Length-1) {
            payloadIndex++;
        } else {
            payloadIndex = -1;
        }
        StartCoroutine(PayloadCycle());
    }    
    
    public void LoadLevel() {
        CameraFade.StartAlphaFade(Color.black, false, 2, 0, levelIndex + 2);
    }

    public void OptionsOnClick() {
        Vector3 hidden = new Vector3(Screen.width + optionsPanel.pivot.x * optionsPanel.rect.width + 10, 0f, 0f);
        Vector3 shown = new Vector3(0, 0, 0);
        optionsSelected = !optionsSelected;
        StartCoroutine(PanelFlyIn(optionsPanel, shown, hidden, optionsSelected, 5000));
        MainPanelHide(!optionsSelected);
    }

    public void ControlsOnClick() {
        Vector3 hidden = new Vector3(Screen.width + controlsPanel.pivot.x * controlsPanel.rect.width + 10, 0f, 0f);
        Vector3 shown = new Vector3(0, 0, 0);
        controlsSelected = !controlsSelected;
        StartCoroutine(PanelFlyIn(controlsPanel, shown, hidden, controlsSelected, 5000));
        MainPanelHide(!controlsSelected);
    }

    public void SelectMissionOnClick() {
        Vector3 hidden = new Vector3(Screen.width + selectMissionPanel.pivot.x * selectMissionPanel.rect.width + 10, 0f, 0f);
        Vector3 shown = new Vector3(0, 0, 0);
        selectMissionSelected = !selectMissionSelected;
        StartCoroutine(PanelFlyIn(selectMissionPanel, shown, hidden, selectMissionSelected, 5000));
        MainPanelHide(!selectMissionSelected);
    }

    /// <summary>
    /// Cycle through missions, shows image and description of mission
    /// </summary>
    /// <param name="button"> Button that called method </param>
    public void MissionOnClick(Button button) {
        for(int i = 0; i<missions.Length; i++) {
            if (button.name == missions[i].name) {
                ColorButton(button, true);
                button.transform.parent.FindChild("Image").gameObject.SetActive(true);
                button.transform.parent.FindChild("Description").gameObject.SetActive(true);
                levelIndex = i;
                Debug.Log(i);
            } else {
                ColorButton(missions[i], false);
                missions[i].transform.parent.FindChild("Image").gameObject.SetActive(false);
                missions[i].transform.parent.FindChild("Description").gameObject.SetActive(false);
            }
        }        
    }

    /// <summary>
    /// Slide main panel on/off screen
    /// </summary>
    /// <param name="show"> True to show panel, false to hide </param>
    private void MainPanelHide(bool show) {
        Vector3 hidden = new Vector3(-Screen.width / 2 - mainPanel.rect.width, 0, 0);
        Vector3 shown = new Vector3(-Screen.width / 2, 0, 0);
        StartCoroutine(PanelFlyIn(mainPanel, shown, hidden, show, 5000));
    }

    public void MasterVolume(Slider volume) {
        AudioListener.volume = volume.value;
        DataManagement.SaveData();
    }  
    
    /// <summary>
    /// Change color of button to show if selected
    /// </summary>
    /// <param name="button">Button to change color</param>
    /// <param name="selected">True for selected state, false for unselected</param>
    private void ColorButton(Button button, bool selected) {
        if (selected) {            
            var colors = button.GetComponent<Button>().colors;
            colors.highlightedColor = new Color32(145, 145, 145, 255);
            colors.normalColor = new Color32(154, 154, 154, 255);
            button.GetComponent<Button>().colors = colors;
        } else {            
            var colors = button.GetComponent<Button>().colors;
            colors.highlightedColor = new Color32(245, 245, 245, 255);
            colors.normalColor = Color.white;
            button.GetComponent<Button>().colors = colors;
        }
    }

    /// <summary>
    /// Slide panel on/off screen
    /// </summary>
    /// <param name="panel">Panel to slide</param>
    /// <param name="shown">Position at which the panel will be shown</param>
    /// <param name="hidden">Position at which the panel will be hidden</param>
    /// <param name="show">True to show panel, false to hide</param>
    /// <param name="speed">Speed which to move panel at, 5000 good start point</param>
    /// <returns></returns>
    public IEnumerator PanelFlyIn(RectTransform panel, Vector3 shownPosition, Vector3 hiddenPosition, bool show, float speed) {
        Vector3 start;
        Vector3 end;
        if (show) {
            start = hiddenPosition;
            end = shownPosition;
        } else {
            start = shownPosition; 
            end = hiddenPosition;
        }
        float startTime = Time.time;
        float journeyLength = Vector3.Distance(start, end);
        float fracJourney;
        do {
            float distCovered = (Time.time - startTime) * speed;
            fracJourney = distCovered / journeyLength;
            panel.localPosition = Vector3.Lerp(start, end, fracJourney);
            yield return new WaitForEndOfFrame();
        } while (fracJourney < 1);
        panel.localPosition = end;
    }

    public void Quit() {
        Debug.Log("Application Closing");
        Application.Quit();
    }
}
