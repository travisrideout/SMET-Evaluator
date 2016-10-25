using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DIAGNOSTICS : MonoBehaviour {
    public WheelCollider wc;

    public Slider brakeTorque;
    public Slider motorTorque;
    public Slider wheelSlip;
    public Text wheelSpeed;

    void Start () {
	    
	}
	
	
	void Update () {
        brakeTorque.value = wc.brakeTorque;
        motorTorque.value = wc.motorTorque;
        wheelSpeed.text = wc.rpm.ToString("000");//(((wc.rpm * wc.radius * Mathf.PI * 2) / 60 ) * 2.23694f).ToString("0.0") + " MPH";
        WheelHit wh;
        wc.GetGroundHit(out wh);
        wheelSlip.value = wh.forwardSlip;
	}
}
