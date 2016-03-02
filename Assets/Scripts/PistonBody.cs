using UnityEngine;
using System.Collections;

public class PistonBody : MonoBehaviour {

    public GameObject Target;
    public float RotateX = 81;
  	
	// Update is called once per frame
	void Update () {        
        if (Target) {
            this.gameObject.transform.LookAt(Target.transform,-this.gameObject.transform.parent.forward);
            this.gameObject.transform.Rotate(RotateX,0,0);           

        } else {
            Debug.Log("Select an Object to Point at");
        }
	}
}
