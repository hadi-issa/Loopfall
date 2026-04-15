using Game;
using UnityEngine;

public class Scene : MonoBehaviour
{
    private const string RuntimeRootName = "LoopRuntime";

    private Transform runtimeRoot = null!;
    private Camera mainCamera = null!;
    private Player player = null!;
    private Vector3 cameraOffset = new(0f, 7.5f, -10.5f);

    private void Start()
    {
        MemoryManager.EnsureExists();
        LoopManager.EnsureExists();

        runtimeRoot = FindOrCreate(RuntimeRootName).transform;
        mainCamera = ConfigureCamera();
        player = ConfigurePlayer();
        BuildTestMap();
        EnsureHud();
    }

    private void LateUpdate()
    {
        if (mainCamera == null || player == null)
        {
            return;
        }

        Vector3 targetPosition = player.transform.position + cameraOffset;
        mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, targetPosition, 8f * Time.deltaTime);
        mainCamera.transform.LookAt(player.transform.position + Vector3.up * 0.6f);
    }

    private Camera ConfigureCamera()
    {
        Camera existingCamera = Camera.main;
        if (existingCamera == null)
        {
            GameObject cameraObject = new("Main Camera");
            existingCamera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
        }

        existingCamera.transform.position = cameraOffset;
        existingCamera.transform.rotation = Quaternion.Euler(22f, 0f, 0f);
        return existingCamera;
    }

    private Player ConfigurePlayer()
    {
        GameObject playerObject = GameObject.Find("Player");
        if (playerObject == null)
        {
            playerObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            playerObject.name = "Player";
        }

        playerObject.transform.SetParent(runtimeRoot, true);
        playerObject.transform.position = new Vector3(0f, 2f, -7f);
        playerObject.transform.localScale = Vector3.one * 1.1f;

        SphereCollider collider = playerObject.GetComponent<SphereCollider>();
        if (collider == null)
        {
            collider = playerObject.AddComponent<SphereCollider>();
        }

        collider.material = CreatePlayerMaterial();

        Rigidbody body = playerObject.GetComponent<Rigidbody>();
        if (body == null)
        {
            body = playerObject.AddComponent<Rigidbody>();
        }

        Player playerController = playerObject.GetComponent<Player>();
        if (playerController == null)
        {
            playerController = playerObject.AddComponent<Player>();
        }

        playerController.SetCameraTransform(mainCamera.transform);

        MeshRenderer renderer = playerObject.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = CreateColorMaterial("LoopfallPlayerMaterial", new Color(0.76f, 0.82f, 0.9f));
        }

        return playerController;
    }

    private void BuildTestMap()
    {
        ClearChildren(runtimeRoot, "Map");
        Transform mapRoot = new GameObject("Map").transform;
        mapRoot.SetParent(runtimeRoot, false);

        PhysicsMaterial stableGround = CreateGroundMaterial();
        CreatePlatform(mapRoot, "SpawnPlatform", new Vector3(0f, 0f, -7f), new Vector3(8f, 1f, 8f), Quaternion.identity, stableGround, new Color(0.16f, 0.2f, 0.24f));
        CreatePlatform(mapRoot, "ApproachPlatform", new Vector3(0f, 0f, 0f), new Vector3(7f, 1f, 7f), Quaternion.identity, stableGround, new Color(0.2f, 0.22f, 0.26f));
        CreatePlatform(mapRoot, "SlopeWest", new Vector3(-5f, 0.2f, 3f), new Vector3(5f, 1f, 8f), Quaternion.Euler(0f, 0f, 16f), stableGround, new Color(0.22f, 0.25f, 0.29f));
        CreatePlatform(mapRoot, "SlopeEast", new Vector3(5f, 0.8f, 5f), new Vector3(4f, 1f, 8f), Quaternion.Euler(10f, 0f, -14f), stableGround, new Color(0.18f, 0.22f, 0.25f));

        GameObject decayPlatform = CreatePlatform(
            mapRoot,
            "DecayPlatform",
            new Vector3(0f, 1.7f, 10f),
            new Vector3(5f, 0.8f, 5f),
            Quaternion.Euler(0f, 0f, 8f),
            stableGround,
            new Color(0.32f, 0.22f, 0.18f));

        Rigidbody decayBody = decayPlatform.AddComponent<Rigidbody>();
        decayBody.isKinematic = true;
        decayBody.useGravity = true;
        decayBody.interpolation = RigidbodyInterpolation.Interpolate;
        decayBody.collisionDetectionMode = CollisionDetectionMode.Continuous;

        DecayController decay = decayPlatform.AddComponent<DecayController>();
        decay.Configure(8f, 1.5f);

        CreatePlatform(mapRoot, "ShrineIsland", new Vector3(0f, 2.2f, 16f), new Vector3(6f, 1f, 6f), Quaternion.identity, stableGround, new Color(0.22f, 0.23f, 0.28f));
        CreateMemoryFragment(mapRoot, new Vector3(-4.5f, 1.7f, -2.5f), FragmentType.Stabilizing);
        CreateMemoryFragment(mapRoot, new Vector3(4.2f, 2.5f, 7.4f), FragmentType.Corrupted);
        CreateMemoryFragment(mapRoot, new Vector3(0.3f, 3.2f, 10.1f), FragmentType.Stabilizing);
        CreateShrine(mapRoot, new Vector3(0f, 3f, 16f));
    }

    private void CreateShrine(Transform parent, Vector3 position)
    {
        GameObject shrineRoot = new("Shrine");
        shrineRoot.transform.SetParent(parent, false);
        shrineRoot.transform.position = position;

        GameObject baseObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        baseObject.name = "ShrineBase";
        baseObject.transform.SetParent(shrineRoot.transform, false);
        baseObject.transform.localScale = new Vector3(1.4f, 0.15f, 1.4f);
        baseObject.GetComponent<Collider>().material = CreateGroundMaterial();
        baseObject.GetComponent<MeshRenderer>().sharedMaterial = CreateColorMaterial("ShrineBaseMaterial", new Color(0.45f, 0.44f, 0.5f));

        GameObject triggerObject = new("InteractionTrigger");
        triggerObject.transform.SetParent(shrineRoot.transform, false);
        SphereCollider trigger = triggerObject.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 2.2f;

        Shrine shrine = triggerObject.AddComponent<Shrine>();
        shrine.Configure(FragmentType.Stabilizing);
    }

    private void CreateMemoryFragment(Transform parent, Vector3 position, FragmentType fragmentType)
    {
        GameObject fragmentObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fragmentObject.name = $"{fragmentType} Fragment";
        fragmentObject.transform.SetParent(parent, false);
        fragmentObject.transform.position = position;
        fragmentObject.transform.localScale = Vector3.one * 0.55f;
        fragmentObject.transform.rotation = Quaternion.Euler(35f, 35f, 0f);

        BoxCollider collider = fragmentObject.GetComponent<BoxCollider>();
        collider.isTrigger = true;

        Rigidbody body = fragmentObject.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;

        MemoryFragment fragment = fragmentObject.AddComponent<MemoryFragment>();
        fragment.Configure(fragmentType);

        Color color = fragmentType == FragmentType.Stabilizing
            ? new Color(0.5f, 0.9f, 0.8f)
            : new Color(0.82f, 0.42f, 0.45f);

        fragmentObject.GetComponent<MeshRenderer>().sharedMaterial = CreateColorMaterial($"{fragmentType}FragmentMaterial", color);
    }

    private GameObject CreatePlatform(
        Transform parent,
        string name,
        Vector3 position,
        Vector3 scale,
        Quaternion rotation,
        PhysicsMaterial physicsMaterial,
        Color color)
    {
        GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
        platform.name = name;
        platform.transform.SetParent(parent, false);
        platform.transform.position = position;
        platform.transform.rotation = rotation;
        platform.transform.localScale = scale;

        BoxCollider collider = platform.GetComponent<BoxCollider>();
        collider.material = physicsMaterial;

        platform.GetComponent<MeshRenderer>().sharedMaterial = CreateColorMaterial($"{name}Material", color);
        return platform;
    }

    private void EnsureHud()
    {
        PrototypeHud hud = FindFirstObjectByType<PrototypeHud>();
        if (hud == null)
        {
            GameObject hudObject = new("PrototypeHud");
            hud = hudObject.AddComponent<PrototypeHud>();
        }

        hud.hideFlags = HideFlags.None;
    }

    private GameObject FindOrCreate(string objectName)
    {
        GameObject existing = GameObject.Find(objectName);
        return existing != null ? existing : new GameObject(objectName);
    }

    private void ClearChildren(Transform parent, string childName)
    {
        Transform existing = parent.Find(childName);
        if (existing == null)
        {
            return;
        }

        Destroy(existing.gameObject);
    }

    private PhysicsMaterial CreatePlayerMaterial()
    {
        PhysicsMaterial material = new("LoopfallPlayerPhysics")
        {
            dynamicFriction = 0.08f,
            staticFriction = 0.08f,
            bounciness = 0f,
            frictionCombine = PhysicsMaterialCombine.Minimum,
            bounceCombine = PhysicsMaterialCombine.Minimum,
        };

        return material;
    }

    private PhysicsMaterial CreateGroundMaterial()
    {
        PhysicsMaterial material = new("LoopfallGroundPhysics")
        {
            dynamicFriction = 0.65f,
            staticFriction = 0.7f,
            bounciness = 0f,
            frictionCombine = PhysicsMaterialCombine.Average,
            bounceCombine = PhysicsMaterialCombine.Minimum,
        };

        return material;
    }

    private Material CreateColorMaterial(string materialName, Color color)
    {
        Material material = new(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
        {
            name = materialName,
            color = color,
        };

        return material;
    }
}
