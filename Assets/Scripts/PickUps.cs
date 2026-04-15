using UnityEngine;

public class PickUps : MonoBehaviour
{
    private GameObject[] pickUpGameObjects;
    private GameObject pickUpFolder;

    private const float radius = 5f;

    private void Start()
    {
        // Create a parent folder in the hierarchy
        pickUpFolder = new GameObject("Pick Ups");

        int total = Game.Config.numberOfObjects;
        pickUpGameObjects = new GameObject[total];

        for (int i = 0; i < total; i++)
        {
            // Create the pickup object
            GameObject pickUp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pickUp.name = $"Pick Up {i + 1}";
            pickUp.transform.SetParent(pickUpFolder.transform);

            // Position in a circle
            float angle = i * Mathf.PI * 2f / total;
            pickUp.transform.localPosition = new Vector3(
                Mathf.Cos(angle) * radius,
                0.5f,
                Mathf.Sin(angle) * radius
            );

            // Visual setup
            pickUp.transform.localEulerAngles = new Vector3(45f, 45f, 45f);
            pickUp.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

            // Tag
            pickUp.tag = Game.Tag.PickUp;

            // Collider
            BoxCollider bc = pickUp.GetComponent<BoxCollider>();
            bc.isTrigger = true;

            // Color
            Renderer rend = pickUp.GetComponent<MeshRenderer>();
            rend.material.color = new Color32(255, 255, 0, 255);

            // Rigidbody (kinematic so it doesn't fall)
            Rigidbody rb = pickUp.AddComponent<Rigidbody>();
            rb.isKinematic = true;

            pickUpGameObjects[i] = pickUp;
        }
    }

    private void Update()
    {
        // Rotate all pickups
        for (int i = 0; i < pickUpGameObjects.Length; i++)
        {
            if (pickUpGameObjects[i] != null)
            {
                pickUpGameObjects[i].transform.Rotate(
                    new Vector3(15f, 30f, 45f) * Time.deltaTime
                );
            }
        }
    }
}
