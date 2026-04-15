using UnityEngine;

public class Walls : MonoBehaviour
{
    private GameObject wallFolder;

    private void Start()
    {
        // Create a parent folder for organization
        wallFolder = new GameObject("Walls");

        CreateWall("North Wall", 0f, 10f, 20.5f, 0.5f);
        CreateWall("South Wall", 0f, -10f, 20.5f, 0.5f);
        CreateWall("East Wall", 10f, 0f, 0.5f, 20.5f);
        CreateWall("West Wall", -10f, 0f, 0.5f, 20.5f);
    }

    private void CreateWall(string name, float px, float pz, float sx, float sz)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.SetParent(wallFolder.transform);

        // Position and size
        wall.transform.localPosition = new Vector3(px, 1f, pz);
        wall.transform.localScale = new Vector3(sx, 2f, sz);

        // Optional: give walls a color
        Renderer rend = wall.GetComponent<MeshRenderer>();
        rend.material.color = new Color32(128, 128, 128, 255);

        // Add collider (already present by default)
        BoxCollider bc = wall.GetComponent<BoxCollider>();
        bc.isTrigger = false;
    }
}
