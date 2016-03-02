using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

//TODO: Add master volume and alarm volume sliders
//TODO: Add show/hide Clinometer
//TODO: Put onto panel, have it slide in like main menu panels StartCoroutine(gameObject.GetComponent<MainMenu>().PanelFlyIn());

public class PauseMenu : MonoBehaviour {
    public Canvas menu;
    private bool paused;

	void Start () {
        DataManagement.LoadData();
        menu.gameObject.SetActive(false);
        Time.timeScale = 1;
        paused = false;
        
    }
	
	void Update () {
        if (Input.GetKeyDown(KeyCode.P)) {
            if (!paused) {
                menu.gameObject.SetActive(true);
                Time.timeScale = 0;
                paused = true;
            } else {
                menu.gameObject.SetActive(false);
                Time.timeScale = 1;
                paused = false;
            }            
        }
	}

    public void Resume() {
        Time.timeScale = 1;
        paused = false;
    }

    public void MainMenu() {
        Resume();   //get time moving again
        SceneManager.LoadScene(1);  //go to main menu
    }

    public void RestartLevel() {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
