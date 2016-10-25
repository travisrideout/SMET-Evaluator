using UnityEngine;
using System.Collections;

public class CamerasController : MonoBehaviour {
    public Transform target;    // The target we are following
    [Header("Cameras")]
    public GameObject operatorCam;
    public GameObject orbitCam;
    public GameObject muvCam;
    [Header("Operator Cam Settings")]
    public float followDistance = 8.0f;   //Follow distance
    public float operatorHeight = 1.85f;    // the height we want the camera to be above the target
    public float angularOffset = -1.0f;
    public float rotationDamping = 0.1f;
    public float heightDamping = 1.0f;
    [Header("Orbit Cam Settings")]
    public float orbitDistance = 5.0f;
    public float orbitXSpeed = 120.0f;
    public float orbitYSpeed = 120.0f;
    public float orbitYMinLimit = -20f;
    public float orbitYMaxLimit = 80f;
    public float orbitDistanceMin = .5f;
    public float orbitDistanceMax = 15f;
    private Vector2 orbitPosition = new Vector2(0, 0);
    [Header("MUV POV Cam Settings")]
    public Vector3 muvCamPosition = new Vector3(0,1.2f,0.4f);

    // Use this for initialization
    void Start() {
        InitOrbitCam();
    }

    private void InitOrbitCam() {
        if (orbitCam == null) {
            return;
        }
        Vector3 angles = orbitCam.transform.eulerAngles;
        orbitPosition.x = angles.y + 30.0f;
        orbitPosition.y = angles.x + 30.0f;
    }

    // Update is called once per frame
    void Update() {
        SelectCam();
    }

    void LateUpdate() {
        OperatorCamPosition();
        OrbitCamPosition();
        MUVCamPosition();
    }

    private void SelectCam() {
        if (Input.GetKeyDown(KeyCode.Alpha1)) {
            operatorCam.SetActive(true);
            orbitCam.SetActive(false);
            muvCam.SetActive(false);
        } else if (Input.GetKeyDown(KeyCode.Alpha2)) {
            operatorCam.SetActive(false);
            orbitCam.SetActive(true);
            muvCam.SetActive(false);
        } else if (Input.GetKeyDown(KeyCode.Alpha3)) {
            operatorCam.SetActive(false);
            orbitCam.SetActive(false);
            muvCam.SetActive(true);
        }
    }

    private void OperatorCamPosition() {
        if (!target || !operatorCam.activeSelf) // Early out if we don't have a target or cam is not active
            return;

        // Calculate the current rotation angles
        float wantedRotationAngle = target.eulerAngles.y + angularOffset + 180.0f;  //adding 180 here because model imported facing backwards
        float wantedHeight = target.position.y + operatorHeight;
        float currentRotationAngle = operatorCam.transform.eulerAngles.y;
        float currentHeight = operatorCam.transform.position.y;

        // Damp the rotation around the y-axis
        currentRotationAngle = Mathf.LerpAngle(currentRotationAngle, wantedRotationAngle, rotationDamping * Time.deltaTime);

        // find and set the target height        
        RaycastHit hit;
        if (Physics.Raycast(operatorCam.transform.position, Vector3.down, out hit)) {
            if (hit.transform.tag == ("Ground")) {
                wantedHeight = hit.point.y + operatorHeight;
            }
        }
        currentHeight = Mathf.Lerp(currentHeight, wantedHeight, heightDamping * Time.deltaTime);

        // Convert the angle into a rotation
        Quaternion currentRotation = Quaternion.Euler(0, currentRotationAngle, 0);

        // Set the position of the camera on the x-z plane to distance meters behind the target
        operatorCam.transform.position = target.position;
        operatorCam.transform.position -= currentRotation * Vector3.forward * followDistance;

        // Set the height of the camera
        operatorCam.transform.position = new Vector3(operatorCam.transform.position.x, currentHeight, operatorCam.transform.position.z);

        // Always look at the target
        operatorCam.transform.LookAt(target);
    }

    private void OrbitCamPosition() {
        if (!target || !orbitCam.activeSelf)   // Early out if we don't have a target or cam is not active
            return;

        if (Input.GetMouseButton(2)) {
            orbitPosition.x += Input.GetAxis("Mouse X") * orbitXSpeed * orbitDistance * 0.02f;
            orbitPosition.y -= Input.GetAxis("Mouse Y") * orbitYSpeed * 0.02f;

            orbitPosition.y = ClampAngle(orbitPosition.y, orbitYMinLimit, orbitYMaxLimit);

        }

        Quaternion rotation = Quaternion.Euler(orbitPosition.y, orbitPosition.x, 0);

        orbitDistance = Mathf.Clamp(orbitDistance - Input.GetAxis("Mouse ScrollWheel") * 5, orbitDistanceMin, orbitDistanceMax);

        //TODO: Orbit camera Obscuration/shearing avoidance 
        //Obscuration/shearing avoidance. Camera moves closer if view is blocked. Needs work
        //RaycastHit hit;
        //if (Physics.Linecast(target.position, transform.position, out hit))
        //{
        //    distance -= hit.distance;
        //}
        Vector3 negDistance = new Vector3(0.0f, 0.0f, -orbitDistance);
        Vector3 position = rotation * negDistance + target.position;

        orbitCam.transform.rotation = rotation;
        orbitCam.transform.position = position;

    }

    public static float ClampAngle(float angle, float min, float max) {
        if (angle < -360F)
            angle += 360F;
        if (angle > 360F)
            angle -= 360F;
        return Mathf.Clamp(angle, min, max);
    }

    private void MUVCamPosition() {
        muvCam.transform.position = target.transform.position + target.transform.rotation * muvCamPosition;
        muvCam.transform.rotation = target.transform.rotation * Quaternion.Euler(0,180,0);  //rotating addition 180 in y because model imported backwards
    }
    
}
