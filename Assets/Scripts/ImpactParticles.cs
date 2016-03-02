using UnityEngine;
using System.Collections;

public class ImpactParticles : MonoBehaviour {
    public Transform impactPrefab;
    public AudioClip impactAudioClip1;

    private int impactCount = 0;
    private AudioSource impactAudio;
    
    void OnCollisionEnter(Collision collision) {
        if (impactCount < 3) {
            ContactPoint contact = collision.contacts[0];
            Quaternion rot = Quaternion.FromToRotation(Vector3.up, contact.normal);
            Vector3 pos = contact.point;
            Instantiate(impactPrefab, pos, rot);    
            if (impactCount == 1) {
                impactAudio = RTCCreateAudioSource.NewAudioSource(gameObject, "Impact AudioSource", 5, .5f, impactAudioClip1, false, true, true);
            } 
        }
        impactCount++;
        Debug.Log("Impact " + impactCount);
    }    
}
