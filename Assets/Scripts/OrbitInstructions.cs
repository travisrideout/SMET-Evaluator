using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class OrbitInstructions : MonoBehaviour {

    public GameObject camera1;
    private Text test;

    void Start()
    {
        test = GetComponent<Text>();
    }

    void Update () {
        if (camera1.activeInHierarchy)
        {            
            test.enabled = true;
        }
        else
        {
            test.enabled = false;
        }
	}
}
