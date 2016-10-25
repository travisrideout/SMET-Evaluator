#pragma warning disable 0414 // private field assigned but not used

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]

public class MUVController2 : MonoBehaviour {
    //Rigidbody.
    private Rigidbody rigid;

    //Debugging
    public bool testBool = true;
    private int debugCount = 0;
    public float instPower = 0;
    public float smoothPower = 0;
    private float powerSum = 0;
    public float powerUsed = 0;
    public float systemEfficiency = 60.0f;  
    public float batteryCapacity = 10000; //Wh
    public float batteryPercentRemaining = 100;
    public float batteryTimeRemaining = 0;
    public float torque = 0;
            
    public bool runEngineAtAwake = true;
    public bool engineRunning = false;
    public bool slave = false;    
    private bool engineStarting = false;

    //Wheel Transforms Of The Vehicle.	
    public Transform[] wheelTransform_L;
    public Transform[] wheelTransform_R;

    //Wheel colliders of the vehicle.
    public WheelCollider[] wheelColliders_L;
    public WheelCollider[] wheelColliders_R;

    //All Wheel Colliders.
    private List<WheelCollider> allWheelColliders = new List<WheelCollider>();

    //Useless Gear Wheels.
    public Transform[] uselessGearTransform_L;
    public Transform[] uselessGearTransform_R;

    //Track Bones.
    public Transform[] trackBoneTransform_L;
    public Transform[] trackBoneTransform_R;

    //Springs
    public Transform[] shockSpring_L;
    public Transform[] shockSpring_R;
    
    //Track Customization.
    public GameObject leftTrackMesh;
    public GameObject rightTrackMesh;
    public float trackOffset = 0.025f;
    public float trackScrollSpeedMultiplier = 1f;

    //Wheels Rotation.
    private float[] rotationValueL;
    private float[] rotationValueR;

    //Suspension
    private float tlPivotLength = 0.2159f;  //length between trailing link pivot point and road wheel axis
    private float tlAngleOffset = 17.72f;   //angle between trailing link y-axis and TL pivot point - road wheel center axis  
    private float springPivotLength = 0.06477f; //length between trailing link pivot point and spring mount point
    private float springUncompressedLength = 0.1805f;   //free length of spring
    //TODO: programmatically find radius, make track thickness public
    private float roadWheelRadius = 0.102f;
    private float trackThickness = 0.02f;

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
    private AudioSource engineStartUpAudio;
    private AudioSource engineIdleAudio;
    private AudioSource engineRunningAudio;

    public AudioClip engineStartUpAudioClip;
    public AudioClip engineIdleAudioClip;
    public AudioClip engineRunningAudioClip;

    //Sound Limits.
    public float minEngineSoundPitch = .5f;
    public float maxEngineSoundPitch = 1.15f;
    public float minEngineSoundVolume = .05f;
    public float maxEngineSoundVolume = .85f;
    public float maxBrakeSoundVolume = .35f;

    //Smokes.
    public GameObject wheelSlip;
    private List<ParticleSystem> wheelParticles = new List<ParticleSystem>();
    public ParticleSystem exhaustSmoke;

    void Start() {
        WheelCollidersInit();
        SoundsInit();
        if (wheelSlip) {
            SmokeInit();
        }

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
        if (!engineRunning)
            engineStartUpAudio = RTCCreateAudioSource.NewAudioSource(gameObject, "Engine Start AudioSource", 5, .5f, engineStartUpAudioClip, false, true, true);
        yield return new WaitForSeconds(.2f);
        engineRunning = true;
        yield return new WaitForSeconds(1f);
        engineStarting = false;
    }

    void SmokeInit() {
        for (int i = 0; i < allWheelColliders.Count; i++) {
            GameObject wp = (GameObject)Instantiate(wheelSlip, allWheelColliders[i].transform.position, transform.rotation) as GameObject;
            wheelParticles.Add(wp.GetComponent<ParticleSystem>());
        }

        for (int i = 0; i < allWheelColliders.Count; i++) {
            wheelParticles[i].transform.position = allWheelColliders[i].transform.position;
            wheelParticles[i].transform.parent = allWheelColliders[i].transform;
        }
    }

    void Update() {
        WheelAlign();
        Sounds();
        Lights();
        if (!slave) {
            Reset();            
        }
    }

    void Reset() {
        if (Input.GetKeyDown(KeyCode.R)) {            
            transform.Translate(0, 1, 0);   // Move the rigidbody up by 1 metres                       
            transform.rotation = Quaternion.Euler(0.0f, transform.eulerAngles.y, 0.0f); //flip upright but keep heading 
        }
    }

    void FixedUpdate() {
        AnimateGears();
        Engine();
        Braking();
        Inputs();
        Smoke();
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
            } else if (gasInput < .1f && Mathf.Abs(steerInput) < .1f && brakeInput < 0.1f) { // stop coasting
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
    void Inputs() {
        //Motor Input.
        gasInput = Mathf.Clamp(Input.GetAxis("Vertical"), 0f, 1f);

        //Brake Input - Amount of brake or reverse expressed in positive magnitude
        brakeInput = -Mathf.Clamp(Input.GetAxis("Vertical"), -1f, 0f);

        //Steering Input, right positive, left negitive, flipped for Slave
        if (!slave) {
            steerInput = -Input.GetAxis("Horizontal");
        } else {
            steerInput = Input.GetAxis("Horizontal");
        }

        
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

    void AnimateGears() {
        for (int i = 0; i < uselessGearTransform_R.Length; i++) {
            uselessGearTransform_R[i].transform.rotation = wheelColliders_R[2].transform.rotation * Quaternion.Euler(rotationValueR[Mathf.CeilToInt((wheelColliders_R.Length) / 2)], 0, 0);
        }

        for (int i = 0; i < uselessGearTransform_L.Length; i++) {
            uselessGearTransform_L[i].transform.rotation = wheelColliders_L[2].transform.rotation * Quaternion.Euler(rotationValueL[Mathf.CeilToInt((wheelColliders_L.Length) / 2)], 0, 0);
        }
    }

    void WheelAlign() {
        RaycastHit hit;

        //Right Wheels Transform.
        for (int k = 0; k < wheelColliders_R.Length; k++) {
            Vector3 ColliderCenterPoint = wheelColliders_R[k].transform.TransformPoint(wheelColliders_R[k].center);
            Transform trailingLink = wheelTransform_R[k].parent;
            float trailingLinkAngle;
            float wheelColliderYOffset = wheelColliders_R[k].transform.localPosition.y - trailingLink.transform.localPosition.y;
            float wheelColliderZOffset;
            float trailingLinkRestAngle = Mathf.Asin((wheelColliders_R[k].suspensionDistance + wheelColliders_R[k].radius - wheelColliderYOffset - roadWheelRadius - trackThickness) / tlPivotLength);
            
            if (Physics.Raycast(ColliderCenterPoint, -wheelColliders_R[k].transform.up, out hit, (wheelColliders_R[k].suspensionDistance + wheelColliders_R[k].radius) * transform.localScale.y) && hit.transform.root != transform) {
                trailingLinkAngle = Mathf.Asin((hit.distance - wheelColliderYOffset - roadWheelRadius - trackThickness) / tlPivotLength);   //find proper trailing link angle based on hit distance
                trailingLink.localRotation = Quaternion.Euler(0f, 270f, 90.0f - ((trailingLinkAngle * Mathf.Rad2Deg) + tlAngleOffset));     //set trailing link angle
                wheelColliderZOffset = Mathf.Cos(trailingLinkAngle) * tlPivotLength;    //find z offset to keep wheel collider centered on wheel transform
                wheelColliders_R[k].transform.localPosition = new Vector3(wheelColliders_R[k].transform.localPosition.x, wheelColliders_R[k].transform.localPosition.y, trailingLink.transform.localPosition.z + wheelColliderZOffset); //set wheel collider location
                float shockCompression = Mathf.Tan(trailingLinkRestAngle - trailingLinkAngle) * springPivotLength;   //calculate how much spring is compressed as current trailing link rotation
                float shockShrinkPercent = shockCompression / springUncompressedLength;  //convert compression length into percent of uncompressed length
                shockSpring_R[k].transform.localScale = new Vector3(1.0f, 1.0f - shockShrinkPercent, 1.0f); //set shock scale based on compression

                if (trackBoneTransform_R.Length > 0)
                    trackBoneTransform_R[k].transform.position = hit.point + (wheelColliders_R[k].transform.up * trackOffset) * transform.localScale.y;
            } else {
                trailingLink.localRotation = Quaternion.Euler(0f, 270f, 90.0f - ((trailingLinkRestAngle * Mathf.Rad2Deg) + tlAngleOffset));
                wheelColliderZOffset = Mathf.Cos(trailingLinkRestAngle) * tlPivotLength;
                wheelColliders_R[k].transform.localPosition = new Vector3(wheelColliders_R[k].transform.localPosition.x, wheelColliders_R[k].transform.localPosition.y, trailingLink.transform.localPosition.z + wheelColliderZOffset);
                shockSpring_R[k].transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);

                if (trackBoneTransform_R.Length > 0)
                    trackBoneTransform_R[k].transform.position = ColliderCenterPoint - (wheelColliders_R[k].transform.up * (wheelColliders_R[k].suspensionDistance + wheelColliders_R[k].radius - trackOffset)) * transform.localScale.y;
            }

            wheelTransform_R[k].transform.rotation = wheelColliders_R[k].transform.rotation * Quaternion.Euler(rotationValueR[Mathf.CeilToInt((wheelColliders_R.Length) / 2)], 0, 0);
            rotationValueR[k] += wheelColliders_R[k].rpm * (6) * Time.deltaTime;
        }

        //Left Wheels Transform.
        for (int i = 0; i < wheelColliders_L.Length; i++) {
            Vector3 ColliderCenterPoint = wheelColliders_L[i].transform.TransformPoint(wheelColliders_L[i].center);
            Transform trailingLink = wheelTransform_L[i].parent;
            float trailingLinkAngle;
            float wheelColliderYOffset = wheelColliders_L[i].transform.localPosition.y - trailingLink.transform.localPosition.y;
            float wheelColliderZOffset;
            float trailingLinkRestAngle = Mathf.Asin((wheelColliders_L[i].suspensionDistance + wheelColliders_L[i].radius - wheelColliderYOffset - roadWheelRadius - trackThickness) / tlPivotLength);
            
            if (Physics.Raycast(ColliderCenterPoint, -wheelColliders_L[i].transform.up, out hit, (wheelColliders_L[i].suspensionDistance + wheelColliders_L[i].radius) * transform.localScale.y) && hit.transform.root != transform) {
                trailingLinkAngle = Mathf.Asin((hit.distance - wheelColliderYOffset - roadWheelRadius - trackThickness) / tlPivotLength);   
                trailingLink.localRotation = Quaternion.Euler(0f, 270f, 90.0f - ((trailingLinkAngle * Mathf.Rad2Deg) + tlAngleOffset));     
                wheelColliderZOffset = Mathf.Cos(trailingLinkAngle) * tlPivotLength;    
                wheelColliders_L[i].transform.localPosition = new Vector3(wheelColliders_L[i].transform.localPosition.x, wheelColliders_L[i].transform.localPosition.y, trailingLink.transform.localPosition.z + wheelColliderZOffset); 
                float shockCompression = Mathf.Tan(trailingLinkRestAngle - trailingLinkAngle) * springPivotLength;
                float shockShrinkPercent = shockCompression / springUncompressedLength;
                shockSpring_L[i].transform.localScale = new Vector3(1.0f, 1.0f - shockShrinkPercent, 1.0f);

                if (trackBoneTransform_L.Length > 0) {
                    trackBoneTransform_L[i].transform.position = hit.point + (wheelColliders_L[i].transform.up * trackOffset) * transform.localScale.y;
                }
            } else {
                trailingLink.localRotation = Quaternion.Euler(0f, 270f, 90.0f - ((trailingLinkRestAngle * Mathf.Rad2Deg) + tlAngleOffset));
                wheelColliderZOffset = Mathf.Cos(trailingLinkRestAngle) * tlPivotLength;
                wheelColliders_L[i].transform.localPosition = new Vector3(wheelColliders_L[i].transform.localPosition.x, wheelColliders_L[i].transform.localPosition.y, trailingLink.transform.localPosition.z + wheelColliderZOffset);
                shockSpring_L[i].transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);

                if (trackBoneTransform_L.Length > 0)
                    trackBoneTransform_L[i].transform.position = ColliderCenterPoint - (wheelColliders_L[i].transform.up * (wheelColliders_L[i].suspensionDistance + wheelColliders_L[i].radius - trackOffset)) * transform.localScale.y;
            }

            wheelTransform_L[i].transform.rotation = wheelColliders_L[i].transform.rotation * Quaternion.Euler(rotationValueL[Mathf.CeilToInt((wheelColliders_L.Length) / 2)], 0, 0);
            rotationValueL[i] += wheelColliders_L[i].rpm * (6) * Time.deltaTime;
        }

        //Scrolling Track Texture Offset.
        if (leftTrackMesh && rightTrackMesh) {
            leftTrackMesh.GetComponent<Renderer>().material.SetTextureOffset("_MainTex", new Vector2((rotationValueL[Mathf.CeilToInt((wheelColliders_L.Length) / 2)] / 1000) * trackScrollSpeedMultiplier, 0));
            rightTrackMesh.GetComponent<Renderer>().material.SetTextureOffset("_MainTex", new Vector2((rotationValueR[Mathf.CeilToInt((wheelColliders_R.Length) / 2)] / 1000) * trackScrollSpeedMultiplier, 0));
            leftTrackMesh.GetComponent<Renderer>().material.SetTextureOffset("_BumpMap", new Vector2((rotationValueL[Mathf.CeilToInt((wheelColliders_L.Length) / 2)] / 1000) * trackScrollSpeedMultiplier, 0));
            rightTrackMesh.GetComponent<Renderer>().material.SetTextureOffset("_BumpMap", new Vector2((rotationValueR[Mathf.CeilToInt((wheelColliders_R.Length) / 2)] / 1000) * trackScrollSpeedMultiplier, 0));
        }
    }

    void Smoke() {
        if (wheelParticles.Count > 0) {
            for (int i = 0; i < allWheelColliders.Count; i++) {

                WheelHit CorrespondingGroundHit;
                allWheelColliders[i].GetGroundHit(out CorrespondingGroundHit);
                ParticleSystem.EmissionModule we = wheelParticles[i].emission;

                if (speed > 1f && allWheelColliders[i].isGrounded) {
                    we.enabled = true;
                    var rate = we.rate;
                    rate.constantMax = Mathf.Lerp(0f, 10f, speed / 25f);
                    we.rate = rate;
                } else {
                    we.enabled = false;
                }
            }
        }

        if (exhaustSmoke) {
            ParticleSystem.EmissionModule ee = exhaustSmoke.emission;
            var rate = ee.rate;
            rate.constantMax = Mathf.Lerp(0f, 5f, engineRPM / maxEngineRPM);
            ee.rate = rate;
            exhaustSmoke.startSpeed = Mathf.Lerp(5f, 20f, engineRPM / maxEngineRPM);
            exhaustSmoke.startSize = Mathf.Lerp(.01f, .5f, engineRPM / maxEngineRPM);
        }
    }
}