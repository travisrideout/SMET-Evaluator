#pragma warning disable 0414 // private field assigned but not used

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]

public class E_EDM_WController : MonoBehaviour {
    //Rigidbody.
    private Rigidbody rigid;

    //Debugging
    public bool testBool = true;
    private int debugCount = 0;
    public float instPower = 0;
    public float smoothPower = 0;
    private float powerSum = 0;
    public float powerUsed = 0;
    public float systemEfficiency = 70.5f;  
    public float batteryCapacity = 10000; //Wh
    public float batteryPercentRemaining = 100;
    public float batteryTimeRemaining = 0;
    public float torque = 0;
            
    public bool runEngineAtAwake = true;
    public bool engineRunning = false;   
    private bool engineStarting = false;

    //Wheel Transforms Of The Vehicle.	
    public Transform[] wheelTransform_L;
    public Transform[] wheelTransform_R;

    //Wheel colliders of the vehicle.
    public WheelCollider[] wheelColliders_L;
    public WheelCollider[] wheelColliders_R;

    //All Wheel Colliders.
    private List<WheelCollider> allWheelColliders = new List<WheelCollider>();
    
    //Wheels Rotation.
    private float[] rotationValueL;
    private float[] rotationValueR;    

    //Center Of Mass.
    public Transform COM;
    
    //Lights
    public GameObject frontLights;
    public GameObject rearLights;

    //Engine
    public AnimationCurve engineTorqueCurve;
    public AnimationCurve brakeTorqueCurve;
    public float engineTorque = 200.0f;
    public float brakeTorque = 5000.0f;
    public float accelRate = 0.1f;
    public float minEngineRPM = 1000.0f;
    public float maxEngineRPM = 3000.0f;
    public float maxSpeed = 5.0f;
    private float speed;
    private float acceleration = 0f;
    private float lastVelocity = 0f;
    private float engineRPM = 0.0f;

    //Inputs
    public float gasInput = 0f;
    public float brakeInput = 0f;
    public float steerInput = 0f;

    //Sound Effects
    private AudioSource engineIdleAudio;
    private AudioSource engineRunningAudio;
    
    public AudioClip engineIdleAudioClip;
    public AudioClip engineRunningAudioClip;

    //Sound Limits.
    public float minEngineSoundPitch = .5f;
    public float maxEngineSoundPitch = 1.15f;
    public float minEngineSoundVolume = .05f;
    public float maxEngineSoundVolume = .85f;
    public float maxBrakeSoundVolume = .35f;
    
    void Start() {
        WheelCollidersInit();
        SoundsInit();

        rigid = GetComponent<Rigidbody>();

        rigid.maxAngularVelocity = 5f;
        rigid.centerOfMass = new Vector3((COM.localPosition.x) * transform.localScale.x, (COM.localPosition.y) * transform.localScale.y, (COM.localPosition.z) * transform.localScale.z);

        rotationValueL = new float[wheelColliders_L.Length];
        rotationValueR = new float[wheelColliders_R.Length];

        if (runEngineAtAwake) {
            KillOrStartEngine();
        }        
    }

    //TODO: Set each wheel collider radius = wheeltransform radius + track thickness
    void WheelCollidersInit() {
        WheelCollider[] wheelcolliders = GetComponentsInChildren<WheelCollider>();
        foreach (WheelCollider wc in wheelcolliders) {
            allWheelColliders.Add(wc);
        }
        allWheelColliders[0].ConfigureVehicleSubsteps(5, 12, 15);
    }

    void SoundsInit() {
        engineIdleAudio = RTCCreateAudioSource.NewAudioSource(gameObject, "engineIdleAudio", 5f, .5f, engineIdleAudioClip, true, true, false);
        engineRunningAudio = RTCCreateAudioSource.NewAudioSource(gameObject, "engineRunningAudio", 5f, 0f, engineRunningAudioClip, true, true, false);
    }    

    public void KillOrStartEngine() {
        if (engineRunning && !engineStarting) {
            engineRunning = false;
        } else if (!engineStarting) {
            StartCoroutine("StartEngine");
        }
    }

    IEnumerator StartEngine() {
        engineRunning = false;
        engineStarting = true;
        engineRunning = true;
        yield return new WaitForSeconds(1f);
        engineStarting = false;
    }

    void Update() {
        WheelAlign();
        Sounds();
        Lights();
        Reset();
    }

    void Reset() {
        if (Input.GetKeyDown(KeyCode.R)) {            
            transform.Translate(0, 1, 0);   // Move the rigidbody up by 1 metres                       
            transform.rotation = Quaternion.Euler(0.0f, transform.eulerAngles.y, 0.0f); //flip upright but keep heading 
        }
    }

    void FixedUpdate() {
        Engine();
        Braking();
        Inputs();
    }    

    void Engine() {     
        for (int i = 0; i < wheelColliders_L.Length; i++) {
            ApplyMotorTorque(wheelColliders_L[i], engineTorque, true);
            powerSum += Mathf.Abs(wheelColliders_L[i].rpm * wheelColliders_L[i].motorTorque) / 9.5488f;            
        }

        for (int i = 0; i < wheelColliders_R.Length; i++) {
            ApplyMotorTorque(wheelColliders_R[i], engineTorque, false);
            powerSum += Mathf.Abs(wheelColliders_R[i].rpm * wheelColliders_R[i].motorTorque) / 9.5488f;
        }

        instPower = powerSum;  //calculate average instantaneous power in Watts
        smoothPower = Mathf.Lerp(smoothPower, instPower, Time.deltaTime);
        powerSum = 0;
        powerUsed += (smoothPower * (Time.fixedDeltaTime / 3600)) / systemEfficiency; //power used in Wh
        batteryPercentRemaining = (batteryCapacity - powerUsed) / batteryCapacity;
        batteryTimeRemaining = (batteryCapacity - powerUsed) / smoothPower;
    }

    //TODO: Need to apply correct fraction of motor torque to wheels that are grounded
    public void ApplyMotorTorque(WheelCollider wc, float torque, bool leftSide) {
        float wheelSpeed = Mathf.Abs(((wc.rpm * wc.radius * Mathf.PI * 2) / 60) * 2.23694f); // m/s to mph
        speed = rigid.velocity.magnitude * 2.23694f; // m/s to mph  

        //Over speed limiter
        if (wheelSpeed > maxSpeed || !engineRunning) {
            torque = 0;
        }

        //Traction control - to limit wheel spin 
        //WheelHit wh;
        //wc.GetGroundHit(out wh);
        //if (!wc.isGrounded) { //Mathf.Abs(wh.forwardSlip) > 0.5f || 
        //    torque = 0;
        //    //Debug.Log("slip: " + wc.name + " " + wh.forwardSlip);
        //}

        if (gasInput > 0.1f) {
            if (leftSide) {
                wc.motorTorque = torque * Mathf.Clamp01(gasInput + Mathf.Clamp01(-steerInput)) * engineTorqueCurve.Evaluate(speed);
            } else {
                wc.motorTorque = torque * Mathf.Clamp01(gasInput + Mathf.Clamp01(steerInput)) * engineTorqueCurve.Evaluate(speed);
            }
        } else if (brakeInput > 0.1f) {
            if (leftSide) {
                wc.motorTorque = -torque * Mathf.Clamp01(brakeInput + Mathf.Clamp01(steerInput)) * engineTorqueCurve.Evaluate(speed);
            } else {
                wc.motorTorque = -torque * Mathf.Clamp01(brakeInput + Mathf.Clamp01(-steerInput)) * engineTorqueCurve.Evaluate(speed);
            }
        } else if (Mathf.Abs(steerInput) > 0.1f) {
            if (leftSide) {
                wc.motorTorque = -torque * steerInput * 0.75f;// * engineTorqueCurve.Evaluate(wheelSpeed);
            } else {
                wc.motorTorque = torque * steerInput * 0.75f;// * engineTorqueCurve.Evaluate(wheelSpeed);
            }
        } else {
            wc.motorTorque = 0;
        }        
        
        //invert motor torque because Unity is left hand rule
        wc.motorTorque = -wc.motorTorque;
    }   

    //TODO: Brake to eliminate rotation in wrong direction, roll back
    public void Braking() {  
        for (int i = 0; i < allWheelColliders.Count; i++) {            
            if (speed > maxSpeed) { // slow on downhills
                allWheelColliders[i].brakeTorque = brakeTorque;                
            } else if (gasInput < .05f && Mathf.Abs(steerInput) < .05f && brakeInput < 0.05f) { // stop coasting
                allWheelColliders[i].brakeTorque = Mathf.Infinity;
            } else {    // release brakes
                allWheelColliders[i].brakeTorque = 0.0f;
            }
        }

        //Assist turning using braking
        //Brake left track if going forward and turning left or going backwards and turning right
        if ((steerInput > 0.1f && gasInput > 0.1f) || (steerInput < -0.1f && brakeInput > 0.1f)) {
            for (int i = 0; i < wheelColliders_L.Length; i++) {
                float wheelSpeed = Mathf.Abs(((wheelColliders_L[i].rpm * wheelColliders_L[i].radius * Mathf.PI * 2) / 60) * 2.23694f); // m/s to mph
                wheelColliders_L[i].motorTorque = 0;
                wheelColliders_L[i].brakeTorque = brakeTorque * Mathf.Abs(steerInput) * brakeTorqueCurve.Evaluate(wheelSpeed);
            }
        //Brake right track if going forward and turning right or going backwards and turning left
        } else if ((steerInput < -0.1f && gasInput > 0.1f) || (steerInput > 0.1f && brakeInput > 0.1f)) {
            for (int i = 0; i < wheelColliders_R.Length; i++) {
                float wheelSpeed = Mathf.Abs(((wheelColliders_R[i].rpm * wheelColliders_R[i].radius * Mathf.PI * 2) / 60) * 2.23694f); // m/s to mph
                wheelColliders_R[i].motorTorque = 0;
                wheelColliders_R[i].brakeTorque = brakeTorque * Mathf.Abs(steerInput) * brakeTorqueCurve.Evaluate(wheelSpeed);
            }
        }        
    }    

    //TODO: Add deadband range in percent, update gas/brake in engine
    // All reversed from MUV because of model
    void Inputs() {
        //Motor Input.
        gasInput = Mathf.MoveTowards(gasInput, -Mathf.Clamp(Input.GetAxis("Vertical"), -1f, 0f), accelRate * Time.deltaTime); 

        //Brake Input - Amount of brake or reverse expressed in positive magnitude
        brakeInput = Mathf.MoveTowards(brakeInput, Mathf.Clamp(Input.GetAxis("Vertical"), 0f, 1f), accelRate * Time.deltaTime);

        //Steering Input, right positive, left negitive, 
        steerInput = Input.GetAxis("Horizontal");
        
    }

    //Toggle lights on key press
    void Lights() {
        if (Input.GetKeyDown(KeyCode.K)) {
            if (frontLights.activeSelf) {
                frontLights.SetActive(false);
            } else {
                frontLights.SetActive(true);
            }
        }
        if (Input.GetKeyDown(KeyCode.L)) {
            if (rearLights.activeSelf) {
                rearLights.SetActive(false);
            } else {
                rearLights.SetActive(true);
            }
        }
    }

    public void Sounds() {
        float rpm = 0.0f;
        rpm = ((maxEngineRPM - minEngineRPM) * Mathf.Max(gasInput, Mathf.Abs(brakeInput), Mathf.Abs(steerInput))) + minEngineRPM;
        engineRPM = Mathf.Lerp(engineRPM, (rpm + UnityEngine.Random.Range(-50f, 50f)), Time.deltaTime * 2f);

        //Engine Audio Volume.
        if (engineRunningAudioClip) {
            engineRunningAudio.volume = Mathf.Lerp(engineRunningAudio.volume, Mathf.Clamp(Mathf.Clamp01(gasInput + Mathf.Abs(steerInput / 2f) + brakeInput), minEngineSoundVolume, maxEngineSoundVolume), Time.deltaTime * 10f);
            
            if (engineRunning)
                engineRunningAudio.pitch = Mathf.Lerp(engineRunningAudio.pitch, Mathf.Lerp(minEngineSoundPitch, maxEngineSoundPitch, (engineRPM) / (maxEngineRPM)), Time.deltaTime * 10f);
            else
                engineRunningAudio.pitch = Mathf.Lerp(engineRunningAudio.pitch, 0, Time.deltaTime * 10f);
        }

        if (engineIdleAudioClip) {
            engineIdleAudio.volume = Mathf.Lerp(engineIdleAudio.volume, Mathf.Clamp((1 + (-Mathf.Max(gasInput, brakeInput))), minEngineSoundVolume, 1f), Time.deltaTime * 10f);
            
            if (engineRunning)
                engineIdleAudio.pitch = Mathf.Lerp(engineIdleAudio.pitch, Mathf.Lerp(minEngineSoundPitch, maxEngineSoundPitch, (engineRPM) / (maxEngineRPM)), Time.deltaTime * 10f);
            else
                engineIdleAudio.pitch = Mathf.Lerp(engineIdleAudio.pitch, 0, Time.deltaTime * 10f);
        }        
    }

    void WheelAlign() {
        //Right Wheels Transform.
        for (int k = 0; k < wheelColliders_R.Length; k++) {
            wheelTransform_R[k].transform.rotation = wheelColliders_R[k].transform.rotation * Quaternion.Euler(rotationValueR[Mathf.CeilToInt((wheelColliders_R.Length) / 2)], 0, 0);
            rotationValueR[k] -= wheelColliders_R[k].rpm * (6) * Time.deltaTime;
        }

        //Left Wheels Transform.
        for (int i = 0; i < wheelColliders_L.Length; i++) {            
            wheelTransform_L[i].transform.rotation = wheelColliders_L[i].transform.rotation * Quaternion.Euler(rotationValueL[Mathf.CeilToInt((wheelColliders_L.Length) / 2)], 0, 0);
            rotationValueL[i] -= wheelColliders_L[i].rpm * (6) * Time.deltaTime;
        }        
    }    
}