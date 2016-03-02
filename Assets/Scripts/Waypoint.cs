using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class Waypoint : MonoBehaviour {
    private Text waypointDisplay;
    private bool GUIfound;

	void Start () {
        InitGUI();
    }

    private void InitGUI() {
        if (GameObject.Find("WaypointGenerator") != null) {
            waypointDisplay = transform.parent.Find("Waypoint GUI/Waypoint Count").GetComponent<Text>();
            GUIfound = true;
        } else {
            GUIfound = false;
        }
    }

    //if MUV touches waypoint, add to counter and destroy waypoint
    void OnTriggerEnter(Collider collider) {
        if (collider.tag == "MUV") {          
            if (GUIfound) {
                int count = int.Parse(waypointDisplay.text);
                count++;
                waypointDisplay.text = count.ToString();
            }
            Destroy(gameObject);
        }          
     }
}
