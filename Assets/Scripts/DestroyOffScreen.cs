using UnityEngine;
using System.Collections;

public class DestroyOffScreen : MonoBehaviour {

    private bool seen = false;
    private Renderer render;    

	// Use this for initialization
	void Start () {
        render = GetComponent<Renderer>();
	}
	
	// Update is called once per frame
    void Update() {
        if (render.isVisible)
            seen = true;

        if (seen && !render.isVisible)
            Destroy(gameObject);
    }
}
