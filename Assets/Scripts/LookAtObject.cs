using UnityEngine;
using System.Collections;

public class LookAtObject : MonoBehaviour {

    public GameObject Target;
    //public float OffsetX = 0;
    //public float OffsetY = 0;
    //public float OffsetZ = 0;
    public float RotateX = 0;
    public float RotateY = 0;
    public float RotateZ = 0;
    public bool LockXRotation = false;
    public bool LockYRotation = false;
    public bool LockZRotation = false;

    //private Rigidbody go;
    private Transform trans;
    //private float initalXRotation;
    //private float initalYRotation;
    //private float initalZRotation;

    void Start() {
        //go = GetComponent<Rigidbody>();
        //initalXRotation = Target.transform.rotation.x;
        //initalYRotation = Target.transform.rotation.y;
        //initalZRotation = Target.transform.rotation.z;
    }
  	
	// Update is called once per frame
	void Update () {
        trans = Target.transform;
        //trans.position = new Vector3(trans.position.x + OffsetX, trans.position.y + OffsetY, trans.position.z + OffsetZ);
        
        if (Target) {
            gameObject.transform.LookAt(trans);
            gameObject.transform.Rotate(RotateX, RotateY, RotateZ);
            if (LockXRotation) {
                gameObject.transform.RotateAround(new Vector3(0, 0, 0), new Vector3(1, 0, 0), 0.0f); //initalXRotation
            }
            if (LockYRotation) {
                gameObject.transform.RotateAround(new Vector3(0, 0, 0), new Vector3(0, 1, 0), 0.0f);
            }
            if (LockZRotation) {
                gameObject.transform.RotateAround(new Vector3(0, 0, 0), new Vector3(0, 0, 1), 0.0f);
            }

        } else {
            Debug.Log("Select an Object to Point at");
        }
	}
}
