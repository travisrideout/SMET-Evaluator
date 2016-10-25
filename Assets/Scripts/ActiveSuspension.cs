using UnityEngine;
using System.Collections;

public class ActiveSuspension : MonoBehaviour {

    public GameObject rigid;

    //Wheel colliders of the vehicle.
    public WheelCollider[] wheelColliders_L;
    public WheelCollider[] wheelColliders_R;

    // Store default spring rate values
    private float[] defaultSpringRate_L;
    private float[] defaultSpringRate_R;

    public float responseRate = 1000.0f;    // N/m/s
    public float minSpringRate = 5000.0f;
    public float maxSpringRate = 100000.0f;
    public float rollDeadband = 5.0f;
    public float pitchDeadband = 5.0f;
    public float travelEndDeadband = 0.01f;
    public float debug = 0.0f;

    // Use this for initialization
    void Start() {
        defaultSpringRate_L = new float[wheelColliders_L.Length];
        defaultSpringRate_R = new float[wheelColliders_R.Length];

        for (int i = 0; i < wheelColliders_L.Length; i++) {
            defaultSpringRate_L[i] = wheelColliders_L[i].suspensionSpring.spring;
        }
        for (int i = 0; i < wheelColliders_R.Length; i++) {
            defaultSpringRate_R[i] = wheelColliders_R[i].suspensionSpring.spring;
        }
    }
	
	// Update is called once per frame
	void FixedUpdate () {
        //get current roll value
        float roll = rigid.transform.eulerAngles.z;
        if (roll < 180)
            roll = -roll;
        else
            roll = 360 - roll;
        roll = -roll;

        WheelHit hit;        

        //Adjust spring rates to try and level vehicle
        if (Mathf.Abs(roll) > rollDeadband) {
            float leftTarget = 0.0f;
            float rightTarget = 0.0f;
            if (roll > rollDeadband) {
                leftTarget = minSpringRate;
                rightTarget = maxSpringRate;
            } else {
                leftTarget = maxSpringRate;
                rightTarget = minSpringRate;
            }

            for(int i = 0; i < wheelColliders_L.Length; i++) {
                if (wheelColliders_L[i].GetGroundHit(out hit)) {
                    float springDistance = Vector3.Distance(hit.point + (transform.up * wheelColliders_L[i].radius), wheelColliders_L[i].transform.position);
                    if (springDistance > travelEndDeadband && roll > 0) {
                        JointSpring target = wheelColliders_L[i].suspensionSpring;
                        target.spring = Mathf.MoveTowards(target.spring, leftTarget, responseRate * Time.fixedDeltaTime);
                        wheelColliders_L[i].suspensionSpring = target;
                    } else if (springDistance < wheelColliders_L[i].suspensionDistance - travelEndDeadband && roll < 0) {
                        JointSpring target = wheelColliders_L[i].suspensionSpring;
                        target.spring = Mathf.MoveTowards(target.spring, leftTarget, responseRate * Time.fixedDeltaTime);
                        wheelColliders_L[i].suspensionSpring = target;
                    }
                }                               
            }
            for (int i = 0; i < wheelColliders_R.Length; i++) {
                if (wheelColliders_R[i].GetGroundHit(out hit)) {
                    float springDistance = Vector3.Distance(hit.point + (transform.up * wheelColliders_R[i].radius), wheelColliders_R[i].transform.position);
                    if (springDistance > travelEndDeadband && roll < 0) {
                        JointSpring target = wheelColliders_R[i].suspensionSpring;
                        target.spring = Mathf.MoveTowards(target.spring, rightTarget, responseRate * Time.fixedDeltaTime);
                        wheelColliders_R[i].suspensionSpring = target;
                    } else if (springDistance < wheelColliders_R[i].suspensionDistance - travelEndDeadband && roll > 0) {
                        JointSpring target = wheelColliders_R[i].suspensionSpring;
                        target.spring = Mathf.MoveTowards(target.spring, rightTarget, responseRate * Time.fixedDeltaTime);
                        wheelColliders_R[i].suspensionSpring = target;
                    }
                }
            }
        } else {
            for (int i = 0; i < wheelColliders_L.Length; i++) {
                JointSpring target = wheelColliders_L[i].suspensionSpring;
                target.spring = Mathf.MoveTowards(target.spring, defaultSpringRate_L[i], responseRate * Time.fixedDeltaTime);
                wheelColliders_L[i].suspensionSpring = target;
                target = wheelColliders_R[i].suspensionSpring;
                target.spring = Mathf.MoveTowards(target.spring, defaultSpringRate_R[i], responseRate * Time.fixedDeltaTime);
                wheelColliders_R[i].suspensionSpring = target;
            }
        }
    }
}
