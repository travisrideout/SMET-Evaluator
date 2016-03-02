#pragma warning disable 0414 // private field assigned but not used

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CROWSController : MonoBehaviour {

    //RWS movement objects and variables
    public GameObject yaw;
    public GameObject pitch;
    public float xSpeed = 120.0f;
    public float ySpeed = 120.0f;
    public float yMinLimit = -20f;
    public float yMaxLimit = 80f;
    public float distanceMin = .5f;
    public float distanceMax = 15f;
    
    float x = 0.0f;
    float y = 0.0f;

    private HingeJoint yawHinge;
    private HingeJoint pitchHinge;
    private JointSpring yawSpring;
    private JointSpring pitchSpring;

    //Gun Objects and variables
    public enum BulletType { Physical, Raycast }; // physical bullets of raycasts
    public BulletType typeOfBullet;

    // Objects, effects and tracers
    public GameObject bullet = null;        // the weapons bullet object
    public Renderer muzzleFlash = null;     // the muzzle flash for this weapon
    public Light lightFlash = null;         // the light flash for this weapon
    public Transform muzzlePoint = null;    // the muzzle point of this weapon
    public Transform ejectPoint = null;     // the ejection point
    public Rigidbody shell = null;          // the weapons empty shell object
    public GameObject impactEffect = null;  // impact effect, used for raycast bullet types
    public GameObject bulletHole = null;    // bullet hole for raycast bullet types
    public GameObject weaponTarget = null;

    //Machinegun Vars
    private bool isFiring = false;          // is the machine gun firing?  used for decreasing accuracy while sustaining fire

    // basic stats
    public int range = 300;                 // range for raycast bullets... bulletType = Ray
    public float damage = 20.0f;            // bullet damage
    public float maxPenetration = 3.0f;     // how many impacts the bullet can survive
    public float fireRate = 0.5f;           // how fast the gun shoots... time between shots can be fired
    public int impactForce = 50;            // how much force applied to a rigid body
    public float bulletSpeed = 200.0f;      // how fast are your bullets
    public int bulletsPerClip = 50;         // number of bullets in each clip
    public int numberOfClips = 5;           // number of clips you start with
    public int maxNumberOfClips = 10;       // maximum number of clips you can hold
    private int bulletsLeft;                // bullets in the gun-- current clip

    public float baseSpread = 1.0f;         // how accurate the weapon starts out... smaller the number the more accurate
    public float maxSpread = 4.0f;          // maximum inaccuracy for the weapon
    public float spreadPerSecond = 0.2f;    // if trigger held down, increase the spread of bullets
    public float spread = 0.0f;             // current spread of the gun
    public float decreaseSpreadPerSec = 0.5f;// amount of accuracy regained per frame when the gun isn't being fired 

    public float reloadTime = 1.0f;         // time it takes to reload the weapon
    private bool isReloading = false;       // am I in the process of reloading
    // used for tracer rendering
    public int shotsFired = 0;              // shots fired since last tracer round
    public int roundsPerTracer = 1;         // number of rounds per tracer
    private int m_LastFrameShot = -1;       // last frame a shot was fired
    private float nextFireTime = 0.0f;      // able to fire again on this frame
    private float[] bulletInfo = new float[6];// all of the info sent to a fired bullet

    // Use this for initialization
    void Start() {
        Vector3 angles = transform.eulerAngles;
        x = angles.y;
        y = angles.x;

        yawHinge = yaw.GetComponent<HingeJoint>();
        pitchHinge = pitch.GetComponent<HingeJoint>();
        yawSpring = yawHinge.spring;
        pitchSpring = pitchHinge.spring;  

        bulletsLeft = bulletsPerClip;       // load gun on startup
    
    }

    // check whats the player is doing every frame
    bool Update() {
        // Did the user press fire.... and what kind of weapon are they using ?  ===============       
        if (Input.GetButton("Fire1")) {
            MachineGun_Fire();   // fire machine gun                 
        }

        //used to decrease weapon accuracy as long as the trigger remains down =====================
        if (Input.GetButtonDown("Fire1")) {
            isFiring = true; // fire is down, gun is firing
        }
        if (Input.GetButtonUp("Fire1")) {
            isFiring = false; // if fire is up... gun is not firing
        }
        if (isFiring) // if the gun is firing
         {
            spread += spreadPerSecond; // gun is less accurate with the trigger held down
        } else {
            spread -= decreaseSpreadPerSec; // gun regains accuracy when trigger is released
        }
        //===========================================================================================
        return true;
    }

    void LateUpdate() {
        //move RWS based on mouse position
        if (yaw != null && pitch != null) {

            x += Input.GetAxis("Mouse X") * xSpeed * 0.02f;
            y -= Input.GetAxis("Mouse Y") * ySpeed * 0.02f;

            y = ClampAngle(y, yMinLimit, yMaxLimit);
            
            yawSpring.targetPosition = x;
            pitchSpring.targetPosition = y;

            yawHinge.spring = yawSpring;
            pitchHinge.spring = pitchSpring;
        }

        //update weapon flashes
        if (muzzleFlash || lightFlash)  // need to have a muzzle or light flash in order to enable or disable them 
         {
            // We shot this frame, enable the muzzle flash
            if (m_LastFrameShot == Time.frameCount) {
                muzzleFlash.transform.localRotation = Quaternion.AngleAxis(Random.value * 57.3f, Vector3.forward);
                muzzleFlash.enabled = true;// enable the muzzle and light flashes
                lightFlash.enabled = true;
            } else {
                muzzleFlash.enabled = false; // disable the light and muzzle flashes
                lightFlash.enabled = false;
            }
        }

        //Limit accuracy degradation
        if (spread >= maxSpread) {
            spread = maxSpread;  //if current spread is greater then max... set to max
        } else {
            if (spread <= baseSpread) {
                spread = baseSpread; //if current spread is less then base, set to base
            }
        }
    }

    //rollover angle values
    public static float ClampAngle(float angle, float min, float max) {
        if (angle < -360F)
            angle += 360F;
        if (angle > 360F)
            angle -= 360F;
        return Mathf.Clamp(angle, min, max);
    }

    // fire the machine gun
    void MachineGun_Fire() {
        if (bulletsLeft <= 0) {
            StartCoroutine("reload");
            return;
        }
        // If there is more than one bullet between the last and this frame
        // Reset the nextFireTime
        if (Time.time - fireRate > nextFireTime)
            nextFireTime = Time.time - Time.deltaTime;
        // Keep firing until we used up the fire time
        while (nextFireTime < Time.time) {
            switch (typeOfBullet) {
                case BulletType.Physical:
                    StartCoroutine("FireOneShot");  // fire a physical bullet
                    break;
                case BulletType.Raycast:
                    StartCoroutine("FireOneRay");  // fire a raycast.... change to FireOneRay
                    break;
                default:
                    Debug.Log("error in bullet type");
                    break;
            }
            shotsFired++;
            bulletsLeft--;
            nextFireTime += fireRate;
            EjectShell();
        }

    }

    // Create and fire a bullet
    IEnumerator FireOneShot() {
        Vector3 position = muzzlePoint.position; // position to spawn bullet is at the muzzle point of the gun       
        // set the gun's info into an array to send to the bullet
        bulletInfo[0] = damage;
        bulletInfo[1] = impactForce;
        bulletInfo[2] = maxPenetration;
        bulletInfo[3] = maxSpread;
        bulletInfo[4] = spread;
        bulletInfo[5] = bulletSpeed;
        //bullet info is set up in start function
        GameObject newBullet = Instantiate(bullet, position, transform.parent.rotation) as GameObject; // create a bullet
        newBullet.SendMessageUpwards("SetUp", bulletInfo); // send the gun's info to the bullet
        //newBullet.GetComponent<Bullet>().Owner = gunOwner; // owner of the bullet is this gun's owner object

        //    if (shotsFired >= roundsPerTracer) // tracer round every so many rounds fired... is there a tracer this round fired?
        //     {
        //        newBullet.renderer.enabled = true; // turn on tracer effect
        //        shotsFired = 0;                    // reset tracer counter
        //    } else {
        //        newBullet.renderer.enabled = false; // turn off tracer effect
        //    }
        //    if (audio) {
        //        audio.Play();  // if there is a gun shot sound....play it
        //    }
        
        if ((bulletsLeft == 0)) {
            StartCoroutine("reload");  // if out of bullets.... reload
            yield break;
        }

        // Register that we shot this frame,
        // so that the LateUpdate function enabled the muzzleflash renderer for one frame
        m_LastFrameShot = Time.frameCount;
    }
    // Create and Fire a raycast
    IEnumerator FireOneRay() {
        // Register that we shot this frame,
        // so that the LateUpdate function enabled the muzzleflash renderer for one frame
        m_LastFrameShot = Time.frameCount;

        string[] Info = new string[2];
        int hitCount = 0;
        bool tracerWasFired = false;
        Vector3 position = muzzlePoint.position; // position to spawn bullet is at the muzzle point of the gun
        Vector3 direction = muzzlePoint.TransformDirection(Random.Range(-maxSpread, maxSpread) * spread, Random.Range(-maxSpread, maxSpread) * spread, 1);
        Vector3 dir = muzzlePoint.forward;  //(weaponTarget.transform.position - position) + direction;
        // set the gun's info into an array to send to the bullet
        bulletInfo[0] = damage;
        bulletInfo[1] = impactForce;
        bulletInfo[2] = maxPenetration;
        bulletInfo[3] = maxSpread;
        bulletInfo[4] = spread;
        bulletInfo[5] = bulletSpeed;
        if (shotsFired >= roundsPerTracer) {
            FireOneTracer(bulletInfo);
            shotsFired = 0;
            tracerWasFired = true;
        }

        RaycastHit[] hits = Physics.RaycastAll(position, dir, range);
        // Debug.DrawRay(position, dir, Color.blue, range);
        for (int i = 0; i < hits.Length; i++) {
            if (hitCount >= maxPenetration) {
                yield break;
            }
            RaycastHit hit = hits[i];
            //Debug.Log( "Bullet hit " + hit.collider.gameObject.name + " at " + hit.point.ToString() );
            // notify hit
            if (!tracerWasFired) { // tracers are set to show impact effects... we dont want to show more then 1 per bullet fired
                ShowHits(hit); // show impacts effects if no tracer was fired this round
            }
            //Info[1] = damage.ToString();
            //hit.collider.SendMessageUpwards("ImHit", Info, SendMessageOptions.DontRequireReceiver);
            // Debug.Log("if " + hitCount + " > " + maxHits + " then destroy bullet...");    
            hitCount++;
        }       
    }

    // create and "fire" an empty shell
    void EjectShell() {
        Vector3 position = ejectPoint.position; // ejectile spawn point at gun's ejection point

        if (shell) {
            Rigidbody newShell = Instantiate(shell, position, transform.parent.rotation) as Rigidbody; // create empty shell
            //give ejectile a slightly random ejection velocity and direction
            newShell.velocity = transform.TransformDirection(Random.Range(-2, 2), Random.Range(-4, -1), 1); //+ 1.0f
            //newShell.
        }
    }
    // tracer rounds for raycast bullets
    void FireOneTracer(float[] info) {
        Vector3 position = muzzlePoint.position;
        GameObject newTracer = Instantiate(bullet, position, transform.parent.rotation) as GameObject; // create a bullet
        //newTracer.SendMessageUpwards("SetUp", info); // send the gun's info to the bullet
        //newTracer.SendMessageUpwards("SetTracer");  // tell the bullet it is only a tracer
    }
    //effects for raycast bullets
    void ShowHits(RaycastHit hit) {
        switch (hit.transform.tag) {
            case "bullet":
                // do nothing if 2 bullets collide
                break;
            case "Player":
                // add blood effect
                break;
            case "wood":
                // add wood impact effects
                break;
            case "stone":
                // add stone impact effect
                break;
            case "ground":
                // add dirt or ground  impact effect
                break;
            default: // default impact effect and bullet hole
                Instantiate(impactEffect, hit.point + 0.1f * hit.normal, Quaternion.FromToRotation(Vector3.up, hit.normal));
                GameObject newBulletHole = Instantiate(bulletHole, hit.point, Quaternion.FromToRotation(Vector3.up, hit.normal)) as GameObject;
                newBulletHole.transform.parent = hit.transform;
                break;
        }
    }
    // reload your weapon
    IEnumerator reload() {
        if (isReloading) {
            yield break; // if already reloading... exit and wait till reload is finished
        }
        if (numberOfClips > 0) {
            isReloading = true; // we are now reloading
            numberOfClips--; // take away a clip
            yield return new WaitForSeconds(reloadTime); // wait for set reload time
            bulletsLeft = bulletsPerClip; // fill up the gun
        }
        isReloading = false; // done reloading
    }
}
