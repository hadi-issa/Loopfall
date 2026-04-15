using UnityEngine;

public class Scene : MonoBehaviour
{
    private GameObject mainCamera;
    private GameObject player;
    private Vector3 mainCameraOffset;

    private void Start()
    {
        CreateCamera();
        CreateGround();
        CreateWalls();
        CreatePickUps();
        CreatePlayer();
    }

    private void LateUpdate()
    {
        if (mainCamera != null && player != null)
            mainCamera.transform.position = player.transform.position + mainCameraOffset;
    }

    private void CreateGround()
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(2f, 1f, 2f);

        Renderer rend = ground.GetComponent<MeshRenderer>();
        rend.material.color = new Color32(0, 32, 64, 255);
    }

    private void CreateWalls()
    {
        gameObject.AddComponent<Walls>();
    }

    private void CreatePickUps()
    {
        gameObject.AddComponent<PickUps>();
    }

    private void CreatePlayer()
    {
        player = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        player.name = "Player";
        player.tag = Game.Tag.Player;

        // Add Player script
        player.AddComponent<Player>();

        // Add Rigidbody if missing
        if (!player.TryGetComponent<Rigidbody>(out _))
            player.AddComponent<Rigidbody>();

        // Calculate camera offset AFTER player exists
        mainCameraOffset = mainCamera.transform.position - player.transform.position;
    }

    private void CreateCamera()
    {
        mainCamera = new GameObject("Main Camera");
        mainCamera.transform.localPosition = new Vector3(0f, 10f, -10f);
        mainCamera.transform.localEulerAngles = new Vector3(45f, 0f, 0f);
        mainCamera.AddComponent<Camera>();
    }
}

