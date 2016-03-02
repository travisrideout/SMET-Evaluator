using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class WaypointGenerator : MonoBehaviour {
    public int numberOfWaypoints = 10;
    public GameObject waypoint;
    public float edgeBuffer = 30;

    private Text totalCount;

    private float xMin;
    private float xMax;
    private float zMin;
    private float zMax;

    void Start () {
        InitGUI();
        GetTerrainSize();
        GenerateWaypoints();
	}

    private void InitGUI() {
        totalCount = transform.Find("Waypoint GUI/Waypoint Total").GetComponent<Text>();
        totalCount.text = "/ " + numberOfWaypoints;
    }

    //find size of terrain
    private void GetTerrainSize() {        
        Vector3 terrainSize;
        terrainSize = Terrain.activeTerrain.terrainData.size;
        //set min/max size for random point generation
        xMin = Terrain.activeTerrain.transform.position.x + edgeBuffer;
        xMax = Terrain.activeTerrain.transform.position.x + terrainSize.x - edgeBuffer;
        zMin = Terrain.activeTerrain.transform.position.z + edgeBuffer;
        zMax = Terrain.activeTerrain.transform.position.z + terrainSize.z - edgeBuffer;
    }

    private void GenerateWaypoints() {
        for (int i = 0; i < numberOfWaypoints; i++) {
            Vector3 position = new Vector3(Random.Range(xMin, xMax), 0.0f, Random.Range(zMin, zMax)); //create random point within terrain bounds
            position.y = Terrain.activeTerrain.SampleHeight(position) + 0.75f;  //set height of point to above ground
            GameObject newWaypoint = Instantiate(waypoint, position, Quaternion.identity) as GameObject;    //create a waypoint prefab at random point
            newWaypoint.transform.parent = gameObject.transform;    //Child new waypoint to waypoints gameobject
            newWaypoint.name = "Waypoint " + i; //number waypoint for easy debug
            Bounds bounds = newWaypoint.GetComponent<Renderer>().bounds;
            Collider[] checkOverlap = Physics.OverlapSphere(newWaypoint.transform.position, 0.5f);  //get collisions with new waypoint
            foreach (Collider col in checkOverlap) {
                if (col.gameObject == newWaypoint.gameObject) {     //ignore collisions with itself
                    continue;
                } else if (col.CompareTag("Ground")) {    //if colliding with a tree, delete and roll back for count to create another
                    Destroy(newWaypoint);
                    i--;
                    break;
                } else if (bounds.Intersects(col.gameObject.GetComponent<Renderer>().bounds)) { //if colliding with a object, delete and roll back for count to create another
                    Destroy(newWaypoint);
                    i--;
                    break;
                }
            }
        }
    }
}
