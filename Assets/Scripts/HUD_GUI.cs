using UnityEngine;
//using System.Collections;
using UnityEngine.UI;

public class HUD_GUI : MonoBehaviour {
    public GameObject target;
    public Image pitchImage;
    public Image rollImage;
    public Text speedo;
    public Text pitch;
    public Text roll;
    public Text heading;
    public float pitchHardLimit = 40.0f;
    public float pitchSoftLimit = 30.0f;
    public float rollHardLimit = 40.0f;
    public float rollSoftLimit = 30.0f;
    public AudioClip tipAlarmAudioClip;

    public enum HEADING { N, NE, E, SE, S, SW, W, NW }

    private float pitched;
    private float rolled;
    private bool pitchAlarm;
    private bool rollAlarm;
    private AudioSource pitchTipAlarm;
    private AudioSource rollTipAlarm;

    void Start () {
        InitSounds();
    }
	
	void Update () {
        Clinometer();
    }

    private void InitSounds() {
        pitchTipAlarm = RTCCreateAudioSource.NewAudioSource(gameObject, "tipAlarmAudio", 5f, 1f, tipAlarmAudioClip, true, false, false);
        pitchTipAlarm.spatialBlend = 0;
        rollTipAlarm = RTCCreateAudioSource.NewAudioSource(gameObject, "tipAlarmAudio", 5f, 1f, tipAlarmAudioClip, true, false, false);
        rollTipAlarm.spatialBlend = 0;
    }

    private void Clinometer() {
        speedo.text = (target.GetComponent<Rigidbody>().velocity.magnitude * 2.23694f).ToString("0.0") + " MPH";
        heading.text = System.Enum.GetName(typeof(HEADING), (int)(target.transform.eulerAngles.y / 45.0f)); //cast to int truncates

        //Pitch
        pitchImage.transform.rotation = Quaternion.Euler(0, 0, target.transform.eulerAngles.x);
        float pitched = target.transform.eulerAngles.x;
        if (pitched < 180)
            pitched = -pitched;
        else
            pitched = 360 - pitched;
        pitched = -pitched;
        pitch.text = pitched.ToString("0.0");
        if (Mathf.Abs(pitched) < pitchSoftLimit) {
            pitch.color = Color.green;
            if (pitchTipAlarm.isPlaying) {
                pitchTipAlarm.Stop();
            }
        } else if (Mathf.Abs(pitched) < pitchHardLimit) {
            pitch.color = Color.yellow;
            pitchTipAlarm.pitch = 0.75f;
            if (!pitchTipAlarm.isPlaying) {                
                pitchTipAlarm.Play();
            }                
        } else {
            pitch.color = Color.red;
            pitchTipAlarm.pitch = 1.0f;
            if (!pitchTipAlarm.isPlaying) {
                pitchTipAlarm.Play();
            }
        }

        //Roll
        rollImage.transform.rotation = Quaternion.Euler(0, 0, target.transform.eulerAngles.z);
        float rolled = target.transform.eulerAngles.z;
        if (rolled < 180)
            rolled = -rolled;
        else
            rolled = 360 - rolled;
        rolled = -rolled;
        roll.text = rolled.ToString("0.0");
        if (Mathf.Abs(rolled) < rollSoftLimit) {
            roll.color = Color.green;
            if (rollTipAlarm.isPlaying) {
                rollTipAlarm.Stop();
            }
        } else if (Mathf.Abs(rolled) < rollHardLimit) {
            roll.color = Color.yellow;
            rollTipAlarm.pitch = 0.75f;
            if (!rollTipAlarm.isPlaying) {
                rollTipAlarm.Play();
            }
        } else {
            roll.color = Color.red;
            rollTipAlarm.pitch = 1.0f;
            if (!rollTipAlarm.isPlaying) {
                rollTipAlarm.Play();
            }
        }
    }
}
