using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class SplashScreen : MonoBehaviour {
    public GameObject logo;
    private int impactCount = 0;
    private AudioSource impactAudio;
    public AudioClip impactAudioClip1;
    public AudioClip impactAudioClip2;

    void Start() {
        CameraFade.StartAlphaFade(Color.black, true, 2f);
        StartCoroutine(BlockWait());        
    }

    IEnumerator BlockWait() {
        yield return new WaitForSeconds(2f);
        CameraFade.StartAlphaFade(Color.black, false, 2f, 2f, 1);
    }

    void OnCollisionEnter(Collision collision) {
        if (impactCount < 3) {   
            if (impactCount == 1) {
                impactAudio = RTCCreateAudioSource.NewAudioSource(logo, "Impact AudioSource", 5, .5f, impactAudioClip1, false, true, true);
            } else {
                impactAudio = RTCCreateAudioSource.NewAudioSource(logo, "Impact AudioSource", 5, .5f, impactAudioClip2, false, true, true);
            }
        }
        impactCount++;
        Debug.Log("Impact " + impactCount);
    }
}
