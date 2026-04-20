using Game;
using UnityEngine;

public class Scene : MonoBehaviour
{
    private const string RuntimeRootName = "LoopRuntime";
    private const float CameraLookHeight = Config.SoccerBallDiameter * 0.95f;
    private const float PondWaterCenterY = 0.272f;
    private const float PondWaterHalfHeight = 0.014f;
    private const float PondLilyPadHalfHeight = 0.008f;
    private const float PondLilyPadSurfaceOffset = 0.006f;
    private const float PondWaterHazardTopY = PondWaterCenterY + PondWaterHalfHeight + 0.002f;
    private const float FountainWaterHazardTopY = 0.462f;
    private static Vector3 PlayerSpawnPosition => new(0f, 0.92f, ScaleWorld(-16f));
    private static Vector3 PondCenter => new(-1.4f, 0f, 16.6f);
    private static Vector3 PondFragmentPlatformCenter => new(-4.05f, 0.74f, 18.95f);
    private static Vector3 PondFragmentPosition => new(PondFragmentPlatformCenter.x, 1.18f, PondFragmentPlatformCenter.z);

    private Transform runtimeRoot = null!;
    private Camera mainCamera = null!;
    private Player player = null!;
    private Vector3 cameraOffset = new(0f, 2.7f, -4.9f);

    private void Awake()
    {
        runtimeRoot = FindOrCreate(RuntimeRootName).transform;
        ResetRuntimeRoot();
        mainCamera = ConfigureCamera();
        player = ConfigurePlayer();
        SnapCameraToPlayer();
    }

    private void Start()
    {
        MemoryManager.EnsureExists();
        LoopManager.EnsureExists();
        LoopfallAudio.EnsureExists();
        LoopfallMusic.EnsureExists().PlayGameplayMusic(false);

        ConfigureEnvironment();
        BuildLoopfallGarden();
        SnapPlayerToSpawn();
        SnapCameraToPlayer();
    }

    private void LateUpdate()
    {
        if (mainCamera == null || player == null)
        {
            return;
        }

        Vector3 targetPosition = player.transform.position + cameraOffset;
        mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, targetPosition, 8.5f * Time.deltaTime);
        mainCamera.transform.LookAt(player.transform.position + Vector3.up * CameraLookHeight);
    }

    private Camera ConfigureCamera()
    {
        Camera existingCamera = Camera.main;
        if (existingCamera == null)
        {
            existingCamera = FindFirstObjectByType<Camera>();
        }

        if (existingCamera == null)
        {
            GameObject cameraObject = new("Main Camera");
            existingCamera = cameraObject.AddComponent<Camera>();
        }

        existingCamera.gameObject.tag = "MainCamera";
        existingCamera.transform.position = cameraOffset;
        existingCamera.transform.rotation = Quaternion.Euler(24f, 0f, 0f);
        existingCamera.fieldOfView = 46f;
        existingCamera.nearClipPlane = 0.03f;
        existingCamera.farClipPlane = 400f;

        AudioListener activeListener = existingCamera.GetComponent<AudioListener>();
        if (activeListener == null)
        {
            activeListener = existingCamera.gameObject.AddComponent<AudioListener>();
        }

        foreach (AudioListener listener in FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
        {
            listener.enabled = listener == activeListener;
        }

        return existingCamera;
    }

    private void ConfigureEnvironment()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.34f, 0.37f, 0.42f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.19f, 0.22f, 0.28f);
        RenderSettings.fogStartDistance = ScaleWorld(42f);
        RenderSettings.fogEndDistance = ScaleWorld(95f);

        Light keyLight = FindFirstObjectByType<Light>();
        if (keyLight == null)
        {
            GameObject lightObject = new("Loopfall Sun");
            keyLight = lightObject.AddComponent<Light>();
            keyLight.type = LightType.Directional;
        }

        keyLight.transform.rotation = Quaternion.Euler(42f, -32f, 0f);
        keyLight.color = new Color(0.82f, 0.86f, 0.96f);
        keyLight.intensity = 1.15f;
        keyLight.shadows = LightShadows.Soft;
    }

    private Player ConfigurePlayer()
    {
        GameObject playerObject = GameObject.Find("Player");
        if (playerObject == null)
        {
            playerObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            playerObject.name = "Player";
        }

        playerObject.transform.SetParent(runtimeRoot, false);
        playerObject.transform.localPosition = PlayerSpawnPosition;
        playerObject.transform.localRotation = Quaternion.identity;
        playerObject.transform.localScale = Vector3.one * Config.SoccerBallDiameter;
        EnsurePlayerVisual(playerObject);

        SphereCollider collider = playerObject.GetComponent<SphereCollider>();
        if (collider == null)
        {
            collider = playerObject.AddComponent<SphereCollider>();
        }

        collider.center = Vector3.zero;
        collider.radius = 0.5f;
        collider.material = CreatePlayerMaterial();

        Rigidbody body = playerObject.GetComponent<Rigidbody>();
        if (body == null)
        {
            body = playerObject.AddComponent<Rigidbody>();
        }

        body.isKinematic = false;
        body.detectCollisions = true;
        body.useGravity = true;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;

        Player playerController = playerObject.GetComponent<Player>();
        if (playerController == null)
        {
            playerController = playerObject.AddComponent<Player>();
        }

        playerController.enabled = true;
        playerController.SetCameraTransform(mainCamera.transform);

        MeshRenderer renderer = playerObject.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            Material ballMaterial = CreateColorMaterial("LoopfallPlayerMaterial", new Color(0.74f, 0.82f, 0.92f));
            TrySetEmission(ballMaterial, new Color(0.08f, 0.1f, 0.14f));
            renderer.sharedMaterial = ballMaterial;
        }

        return playerController;
    }

    private void SnapPlayerToSpawn()
    {
        if (player == null)
        {
            player = ConfigurePlayer();
            return;
        }

        Transform playerTransform = player.transform;
        player.enabled = true;
        playerTransform.SetParent(runtimeRoot, false);
        playerTransform.localPosition = PlayerSpawnPosition;
        playerTransform.localRotation = Quaternion.identity;
        playerTransform.localScale = Vector3.one * Config.SoccerBallDiameter;

        Rigidbody body = player.GetComponent<Rigidbody>();
        if (body != null)
        {
            body.isKinematic = false;
            body.detectCollisions = true;
            body.useGravity = true;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        SphereCollider sphereCollider = player.GetComponent<SphereCollider>();
        if (sphereCollider != null)
        {
            sphereCollider.enabled = true;
        }
    }

    private void SnapCameraToPlayer()
    {
        if (mainCamera == null || player == null)
        {
            return;
        }

        mainCamera.transform.position = player.transform.position + cameraOffset;
        mainCamera.transform.LookAt(player.transform.position + Vector3.up * CameraLookHeight);
    }

    private void ResetRuntimeRoot()
    {
        runtimeRoot.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        runtimeRoot.localScale = Vector3.one;
    }

    private void EnsurePlayerVisual(GameObject playerObject)
    {
        MeshRenderer renderer = playerObject.GetComponent<MeshRenderer>();
        MeshFilter filter = playerObject.GetComponent<MeshFilter>();
        if (renderer != null && filter != null)
        {
            return;
        }

        Transform visual = playerObject.transform.Find("Visual");
        if (visual == null)
        {
            GameObject visualObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visualObject.name = "Visual";
            visualObject.transform.SetParent(playerObject.transform, false);
            visualObject.transform.localPosition = Vector3.zero;
            visualObject.transform.localRotation = Quaternion.identity;
            visualObject.transform.localScale = Vector3.one;

            Collider visualCollider = visualObject.GetComponent<Collider>();
            if (visualCollider != null)
            {
                Destroy(visualCollider);
            }

            visual = visualObject.transform;
        }
    }

    private void BuildLoopfallGarden()
    {
        ClearChildren(runtimeRoot, "Map");
        Transform mapRoot = new GameObject("Map").transform;
        mapRoot.SetParent(runtimeRoot, false);

        PhysicsMaterial stableGround = CreateGroundMaterial();
        PhysicsMaterial slickGround = CreateSlickGroundMaterial();

        CreateIslandBase(mapRoot, stableGround);
        CreateCentralHub(mapRoot, stableGround);
        CreateBranchPaths(mapRoot, stableGround, slickGround);
        CreatePondGarden(mapRoot, stableGround);
        CreateFlowerWalk(mapRoot, stableGround);
        CreateStoneField(mapRoot, stableGround);
        CreatePalmCourt(mapRoot, stableGround);
        CreateMoundGarden(mapRoot, stableGround, slickGround);
        CreateMazeGarden(mapRoot, stableGround);
        CreateDecorativeFragmentsAndShrine(mapRoot);
    }

    private void CreateIslandBase(Transform parent, PhysicsMaterial groundMaterial)
    {
        CreatePlatform(parent, "IslandBase", new Vector3(0f, -1.75f, 0f), new Vector3(44f, 3.2f, 52f), Quaternion.identity, groundMaterial, new Color(0.16f, 0.18f, 0.2f));
        CreatePlatform(parent, "IslandTop", new Vector3(0f, -0.15f, 0f), new Vector3(39f, 0.8f, 47f), Quaternion.identity, groundMaterial, new Color(0.56f, 0.64f, 0.42f));

        CreatePlatform(parent, "NorthRetainingWall", new Vector3(0f, 1.05f, 23.2f), new Vector3(39f, 2.2f, 1.2f), Quaternion.identity, groundMaterial, new Color(0.34f, 0.36f, 0.38f));
        CreatePlatform(parent, "SouthRetainingWall", new Vector3(0f, 1.05f, -23.2f), new Vector3(39f, 2.2f, 1.2f), Quaternion.identity, groundMaterial, new Color(0.34f, 0.36f, 0.38f));
        CreatePlatform(parent, "WestRetainingWall", new Vector3(-19.2f, 1.05f, 0f), new Vector3(1.2f, 2.2f, 47f), Quaternion.identity, groundMaterial, new Color(0.34f, 0.36f, 0.38f));
        CreatePlatform(parent, "EastRetainingWall", new Vector3(19.2f, 1.05f, 0f), new Vector3(1.2f, 2.2f, 47f), Quaternion.identity, groundMaterial, new Color(0.34f, 0.36f, 0.38f));
    }

    private void CreateCentralHub(Transform parent, PhysicsMaterial groundMaterial)
    {
        Color plazaStone = new(0.84f, 0.83f, 0.78f);
        Color fountainStone = new(0.67f, 0.72f, 0.76f);
        Color fountainAccent = new(0.76f, 0.79f, 0.84f);
        Color fountainWaterColor = new(0.36f, 0.62f, 0.78f);

        CreateCylinder(parent, "CentralPlaza", new Vector3(0f, 0.02f, 0f), new Vector3(9.8f, 0.35f, 9.8f), groundMaterial, plazaStone);
        CreateCylinder(parent, "PlazaInnerRing", new Vector3(0f, 0.27f, 0f), new Vector3(7.25f, 0.07f, 7.25f), groundMaterial, new Color(0.67f, 0.69f, 0.72f));

        GameObject fountainBasinFloor = CreateCylinder(parent, "FountainBasinFloor", new Vector3(0f, 0.28f, 0f), new Vector3(3.18f, 0.12f, 3.18f), groundMaterial, fountainStone);
        GameObject fountainWater = CreateCylinder(parent, "FountainWater", new Vector3(0f, 0.42f, 0f), new Vector3(2.42f, 0.03f, 2.42f), groundMaterial, fountainWaterColor);
        GameObject fountainPedestal = CreateCylinder(parent, "FountainPedestal", new Vector3(0f, 0.92f, 0f), new Vector3(0.84f, 0.54f, 0.84f), groundMaterial, fountainAccent);
        GameObject fountainSpout = CreateCylinder(parent, "FountainSpout", new Vector3(0f, 1.62f, 0f), new Vector3(0.12f, 0.2f, 0.12f), groundMaterial, fountainAccent);
        GameObject fountainCore = CreateSphere(parent, "FountainCore", new Vector3(0f, 2.28f, 0f), new Vector3(0.48f, 0.48f, 0.48f), groundMaterial, new Color(0.72f, 0.9f, 0.95f));
        CreateWaterHazardDisk(
            parent,
            "FountainWaterHazard",
            new Vector3(0f, 0f, 0f),
            2.42f * 0.5f,
            FountainWaterHazardTopY,
            "Touched the fountain water");

        for (int i = 0; i < 16; i++)
        {
            float angle = i / 16f * Mathf.PI * 2f;
            Vector3 rimPosition = new Vector3(Mathf.Cos(angle) * 3.02f, 0.63f, Mathf.Sin(angle) * 3.02f);
            CreateBox(parent, $"FountainRim_{i}", rimPosition, new Vector3(0.58f, 0.27f, 1.18f), Quaternion.Euler(0f, -Mathf.Rad2Deg * angle, 0f), groundMaterial, fountainStone);
        }

        MakeVisualOnly(fountainWater);
        MakeVisualOnly(fountainSpout);
        MakeVisualOnly(fountainCore);

        TrySetEmission(fountainBasinFloor.GetComponent<MeshRenderer>().sharedMaterial, new Color(0.03f, 0.05f, 0.06f));
        TrySetEmission(fountainWater.GetComponent<MeshRenderer>().sharedMaterial, new Color(0.08f, 0.16f, 0.2f));
        TrySetEmission(fountainPedestal.GetComponent<MeshRenderer>().sharedMaterial, new Color(0.04f, 0.05f, 0.06f));
        TrySetEmission(fountainSpout.GetComponent<MeshRenderer>().sharedMaterial, new Color(0.05f, 0.07f, 0.09f));
        TrySetEmission(fountainCore.GetComponent<MeshRenderer>().sharedMaterial, new Color(0.1f, 0.2f, 0.26f));
        AttachLoopEmitter(fountainWater, LoopfallCue.FountainLoop, 0.18f, 0.96f, 1f, 3f, 26f);

        GameObject fountainEffects = new("FountainEffects");
        fountainEffects.transform.SetParent(parent, false);
        FountainController fountainController = fountainEffects.AddComponent<FountainController>();
        fountainController.Configure(
            fountainWater.transform,
            fountainWater.GetComponent<Renderer>(),
            fountainSpout.transform,
            fountainCore.transform,
            fountainCore.GetComponent<Renderer>());

        for (int i = 0; i < 12; i++)
        {
            float angle = i / 12f * Mathf.PI * 2f;
            Vector3 stonePosition = new Vector3(Mathf.Cos(angle) * 5.8f, 0.18f, Mathf.Sin(angle) * 5.8f);
            CreateBox(parent, $"PlazaMarker_{i}", stonePosition, new Vector3(0.45f, 0.18f, 1.2f), Quaternion.Euler(0f, -Mathf.Rad2Deg * angle, 0f), groundMaterial, new Color(0.73f, 0.72f, 0.69f));
        }
    }

    private void CreateBranchPaths(Transform parent, PhysicsMaterial stableGround, PhysicsMaterial slickGround)
    {
        CreatePath(parent, "SouthPath", new Vector3(0f, 0.05f, -10.8f), new Vector3(5.1f, 0.16f, 12f), Quaternion.identity, stableGround, new Color(0.86f, 0.85f, 0.8f));
        CreatePath(parent, "NorthPath", new Vector3(0f, 0.05f, 10.2f), new Vector3(5.1f, 0.16f, 11.2f), Quaternion.identity, stableGround, new Color(0.86f, 0.85f, 0.8f));
        CreatePath(parent, "WestPath", new Vector3(-9.2f, 0.05f, -8.2f), new Vector3(12.2f, 0.16f, 4.7f), Quaternion.Euler(0f, 28f, 0f), stableGround, new Color(0.86f, 0.85f, 0.8f));
        CreatePath(parent, "EastPath", new Vector3(10f, 0.05f, -2.4f), new Vector3(13.5f, 0.16f, 4.8f), Quaternion.Euler(0f, -8f, 0f), stableGround, new Color(0.86f, 0.85f, 0.8f));
        CreatePath(parent, "NorthEastPath", new Vector3(9.4f, 0.05f, 9.5f), new Vector3(11.2f, 0.16f, 4.4f), Quaternion.Euler(0f, -28f, 0f), stableGround, new Color(0.86f, 0.85f, 0.8f));
        // The approach stops at the bank now; the pond itself is crossed on lilypads.

        // The pond route is now lily-pad parkour; these posts frame the entrance as a nod to the old boardwalk.
        CreatePondEntranceLanterns(parent);
    }

    private void CreatePondGarden(Transform parent, PhysicsMaterial groundMaterial)
    {
        Vector3 pondCenter = PondCenter;
        float pondWaterSurfaceY = PondWaterCenterY + PondWaterHalfHeight;
        float pondLilyPadCenterY = pondWaterSurfaceY + PondLilyPadSurfaceOffset;
        CreateOrganicPatch(parent, "PondLawn", new Vector3(-1.2f, 0.02f, 16.3f), new Vector3(13.5f, 0.22f, 10.5f), Quaternion.Euler(0f, -12f, 0f), groundMaterial, new Color(0.48f, 0.68f, 0.4f));
        GameObject pondBasin = CreateCylinder(parent, "PondBasin", new Vector3(pondCenter.x, 0.23f, pondCenter.z), new Vector3(8.55f, 0.035f, 8.55f), groundMaterial, new Color(0.28f, 0.42f, 0.5f));
        CreateCylinder(parent, "PondFloor", new Vector3(pondCenter.x, 0.205f, pondCenter.z), new Vector3(6.45f, 0.025f, 6.45f), groundMaterial, new Color(0.25f, 0.36f, 0.42f));
        GameObject pondWater = CreateCylinder(parent, "PondWater", new Vector3(pondCenter.x, PondWaterCenterY, pondCenter.z), new Vector3(7.3f, PondWaterHalfHeight, 7.3f), groundMaterial, new Color(0.34f, 0.56f, 0.74f));
        CreatePondBankSegments(parent, pondCenter, 4.05f, groundMaterial);
        CreatePondEntrance(parent, pondCenter, 4.05f, groundMaterial);
        CreateCylinder(parent, "PondIsland", new Vector3(-0.3f, 0.365f, 15.9f), new Vector3(1.55f, 0.09f, 1.55f), groundMaterial, new Color(0.44f, 0.62f, 0.35f));
        CreateWaterHazardDisk(
            parent,
            "PondWaterHazard",
            pondCenter,
            7.3f * 0.5f,
            PondWaterHazardTopY,
            "Fell into the pond");
        MakeVisualOnly(pondBasin);
        MakeVisualOnly(pondWater);

        CreateLilyPad(parent, new Vector3(-1.2f, pondLilyPadCenterY, 15.2f), 0.46f);
        CreateLilyPad(parent, new Vector3(-1.85f, pondLilyPadCenterY, 15.95f), 0.43f);
        CreateLilyPad(parent, new Vector3(-2.5f, pondLilyPadCenterY, 16.65f), 0.4f);
        CreateLilyPad(parent, new Vector3(-3.15f, pondLilyPadCenterY, 17.3f), 0.38f);
        CreateLilyPad(parent, new Vector3(-3.85f, pondLilyPadCenterY, 17.95f), 0.36f);
        CreateLilyPad(parent, new Vector3(-4.55f, pondLilyPadCenterY, 18.6f), 0.34f);
        CreateLilyPad(parent, new Vector3(-5.15f, pondLilyPadCenterY, 19.15f), 0.31f);

        CreateLilyPad(parent, new Vector3(1.45f, pondLilyPadCenterY, 18.35f), 0.34f);
        CreateLilyPad(parent, new Vector3(2.05f, pondLilyPadCenterY, 16.95f), 0.31f);
        CreateLilyPad(parent, new Vector3(-4.1f, pondLilyPadCenterY, 15.1f), 0.37f);
        CreateLilyPad(parent, new Vector3(-0.25f, pondLilyPadCenterY, 17.95f), 0.28f);
        CreateLilyPad(parent, new Vector3(-0.2f, pondLilyPadCenterY, 13.15f), 0.26f);
        CreateLilyPad(parent, new Vector3(-0.75f, pondLilyPadCenterY, 13.75f), 0.24f);
        CreateLilyPad(parent, new Vector3(-1.35f, pondLilyPadCenterY, 14.35f), 0.23f);
        CreateLilyPad(parent, new Vector3(-2.05f, pondLilyPadCenterY, 14.75f), 0.22f);
        CreateLilyPad(parent, new Vector3(0.65f, pondLilyPadCenterY, 14.65f), 0.23f);
        CreateLilyPad(parent, new Vector3(1.05f, pondLilyPadCenterY, 15.45f), 0.21f);

        Vector3 entrancePad = pondCenter + ResolvePondEntranceDirection() * 3.15f;
        CreateLilyPad(parent, new Vector3(entrancePad.x, pondLilyPadCenterY, entrancePad.z), 0.3f);
        CreateScatteredLilyPads(parent, pondCenter, pondLilyPadCenterY);

        CreateFlowerCluster(parent, new Vector3(-13.4f, 0.2f, 16.6f), new Color(0.62f, 0.34f, 0.72f));
        CreateFlowerCluster(parent, new Vector3(-14.5f, 0.2f, 11.9f), new Color(0.56f, 0.24f, 0.64f));
        CreateFlowerCluster(parent, new Vector3(-11.1f, 0.2f, 19.1f), new Color(0.48f, 0.2f, 0.64f));
    }

    private void CreateFlowerWalk(Transform parent, PhysicsMaterial groundMaterial)
    {
        CreateOrganicPatch(parent, "TopLeftLawn", new Vector3(-12.6f, 0.02f, 11.4f), new Vector3(9.6f, 0.18f, 11.6f), Quaternion.Euler(0f, 14f, 0f), groundMaterial, new Color(0.53f, 0.72f, 0.42f));
        CreateScatteredPebbles(parent, new Vector3(-6.6f, 0.16f, 12.8f), 5, 1.4f);
        CreateScatteredPebbles(parent, new Vector3(-3.6f, 0.16f, 15.2f), 4, 1.2f);
    }

    private void CreateStoneField(Transform parent, PhysicsMaterial groundMaterial)
    {
        CreateOrganicPatch(parent, "StoneLawn", new Vector3(12f, 0.02f, 12.7f), new Vector3(10.8f, 0.2f, 10.5f), Quaternion.Euler(0f, -9f, 0f), groundMaterial, new Color(0.56f, 0.71f, 0.45f));

        Vector3[] stones =
        {
            new(9f, 0.36f, 14f),
            new(12.5f, 0.3f, 14.8f),
            new(15f, 0.32f, 13.1f),
            new(11.2f, 0.34f, 10.9f),
            new(14.6f, 0.3f, 9.5f),
            new(16.2f, 0.28f, 15.6f),
        };

        for (int i = 0; i < stones.Length; i++)
        {
            CreateSphere(parent, $"Stone_{i}", stones[i], new Vector3(2.2f, 0.85f, 1.7f), groundMaterial, new Color(0.78f, 0.8f, 0.82f));
        }

        CreateScatteredPebbles(parent, new Vector3(17.1f, 0.16f, 9.6f), 8, 2.4f);
    }

    private void CreatePalmCourt(Transform parent, PhysicsMaterial groundMaterial)
    {
        CreateOrganicPatch(parent, "PalmLawn", new Vector3(12.8f, 0.02f, -2.2f), new Vector3(10.6f, 0.2f, 13.2f), Quaternion.Euler(0f, -10f, 0f), groundMaterial, new Color(0.54f, 0.7f, 0.44f));
        CreatePath(parent, "PalmCrossPath", new Vector3(11.8f, 0.06f, -1.9f), new Vector3(10.4f, 0.14f, 2.7f), Quaternion.identity, groundMaterial, new Color(0.87f, 0.85f, 0.79f));

        CreatePalmTree(parent, new Vector3(13.2f, 0.2f, 3.8f), 2.8f);
        CreatePalmTree(parent, new Vector3(15.8f, 0.2f, 1f), 2.9f);
        CreatePalmTree(parent, new Vector3(10.6f, 0.2f, -1.8f), 2.6f);
        CreatePalmTree(parent, new Vector3(15.2f, 0.2f, -5.1f), 2.7f);
        CreatePalmTree(parent, new Vector3(12f, 0.2f, -6.2f), 2.5f);
    }

    private void CreateMoundGarden(Transform parent, PhysicsMaterial stableGround, PhysicsMaterial slickGround)
    {
        CreateOrganicPatch(parent, "MoundLawn", new Vector3(-10.6f, 0.02f, -10.4f), new Vector3(16.2f, 0.2f, 11.6f), Quaternion.Euler(0f, 20f, 0f), stableGround, new Color(0.55f, 0.72f, 0.45f));
        CreatePlatform(parent, "MoundSlope", new Vector3(-9.2f, 1.25f, -11.3f), new Vector3(12.8f, 2.2f, 7.5f), Quaternion.Euler(-2.5f, 18f, -18f), slickGround, new Color(0.74f, 0.76f, 0.8f));
        CreatePlatform(parent, "MoundRidge", new Vector3(-11.6f, 2.55f, -11.1f), new Vector3(8.4f, 1.6f, 4.5f), Quaternion.Euler(-4f, -14f, 22f), slickGround, new Color(0.68f, 0.71f, 0.75f));
        CreateScatteredPebbles(parent, new Vector3(-5.4f, 0.26f, -5.8f), 6, 2.2f);
    }

    private void CreateMazeGarden(Transform parent, PhysicsMaterial groundMaterial)
    {
        CreatePlatform(parent, "MazeBase", new Vector3(12.8f, 0.18f, -13.5f), new Vector3(12.2f, 0.6f, 11.8f), Quaternion.Euler(0f, -8f, 0f), groundMaterial, new Color(0.83f, 0.8f, 0.74f));
        CreatePlatform(parent, "MazeBorderNorth", new Vector3(12.8f, 1f, -8f), new Vector3(12.6f, 1.3f, 0.45f), Quaternion.Euler(0f, -8f, 0f), groundMaterial, new Color(0.28f, 0.5f, 0.22f));
        CreatePlatform(parent, "MazeBorderSouth", new Vector3(12.8f, 1f, -19f), new Vector3(12.6f, 1.3f, 0.45f), Quaternion.Euler(0f, -8f, 0f), groundMaterial, new Color(0.28f, 0.5f, 0.22f));
        CreatePlatform(parent, "MazeBorderWest", new Vector3(7f, 1f, -13.5f), new Vector3(0.45f, 1.3f, 11.6f), Quaternion.Euler(0f, -8f, 0f), groundMaterial, new Color(0.28f, 0.5f, 0.22f));
        CreatePlatform(parent, "MazeBorderEast", new Vector3(18.6f, 1f, -13.5f), new Vector3(0.45f, 1.3f, 11.6f), Quaternion.Euler(0f, -8f, 0f), groundMaterial, new Color(0.28f, 0.5f, 0.22f));

        Vector3 mazeCenter = new(12.8f, 0.78f, -13.5f);
        CreateMazeWall(parent, "MazeWall_0", mazeCenter + new Vector3(0f, 0f, 4f), new Vector3(8.4f, 1.15f, 0.38f), -8f, groundMaterial);
        CreateMazeWall(parent, "MazeWall_1", mazeCenter + new Vector3(-4f, 0f, 1f), new Vector3(0.38f, 1.15f, 7.2f), -8f, groundMaterial);
        CreateMazeWall(parent, "MazeWall_2", mazeCenter + new Vector3(3.4f, 0f, 1.4f), new Vector3(0.38f, 1.15f, 7.4f), -8f, groundMaterial);
        CreateMazeWall(parent, "MazeWall_3", mazeCenter + new Vector3(0.1f, 0f, -0.4f), new Vector3(7.2f, 1.15f, 0.38f), -8f, groundMaterial);
        CreateMazeWall(parent, "MazeWall_4", mazeCenter + new Vector3(-2f, 0f, -4.2f), new Vector3(6.2f, 1.15f, 0.38f), -8f, groundMaterial);
        CreateMazeWall(parent, "MazeWall_5", mazeCenter + new Vector3(5f, 0f, -3.2f), new Vector3(0.38f, 1.15f, 4.3f), -8f, groundMaterial);
        CreateMazeWall(parent, "MazeWall_6", mazeCenter + new Vector3(-5.2f, 0f, -1.8f), new Vector3(0.38f, 1.15f, 3.4f), -8f, groundMaterial);

        for (int row = 0; row < 5; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                Color flowerColor = Color.HSVToRGB((row * 0.16f + col * 0.05f) % 1f, 0.6f, 0.9f);
                Vector3 flowerPosition = new Vector3(8.2f + col * 1.3f, 0.52f, -9.4f - row * 2.2f);
                CreateFlowerMarker(parent, flowerPosition, flowerColor);
            }
        }
    }

    private void CreateDecorativeFragmentsAndShrine(Transform parent)
    {
        CreateMemoryFragment(parent, new Vector3(-11.4f, 0.9f, -8.4f), FragmentType.Stabilizing);
        CreatePondFragmentPlatform(parent);
        CreateMemoryFragment(parent, PondFragmentPosition, FragmentType.Stabilizing);
        CreateMemoryFragment(parent, new Vector3(15.1f, 0.95f, 2.3f), FragmentType.Corrupted);
        CreateMemoryFragment(parent, new Vector3(14.6f, 1.05f, -13.3f), FragmentType.Corrupted);
        CreateMemoryFragment(parent, new Vector3(10.2f, 0.9f, 13.8f), FragmentType.Stabilizing);

        GameObject shrinePlinth = CreateCylinder(parent, "ShrinePlinth", new Vector3(0f, 0.56f, 6.9f), new Vector3(2.3f, 0.22f, 2.3f), CreateGroundMaterial(), new Color(0.49f, 0.5f, 0.56f));
        TrySetEmission(shrinePlinth.GetComponent<MeshRenderer>().sharedMaterial, new Color(0.03f, 0.05f, 0.07f));
        CreateShrine(parent, new Vector3(0f, 0.96f, 6.9f));
    }

    private void CreateShrine(Transform parent, Vector3 position)
    {
        GameObject shrineRoot = new("Shrine");
        shrineRoot.transform.SetParent(parent, false);
        shrineRoot.transform.localPosition = ScaleWorld(position);

        GameObject baseObject = CreateCylinder(shrineRoot.transform, "ShrineBase", Vector3.zero, new Vector3(1.9f, 0.18f, 1.9f), CreateGroundMaterial(), new Color(0.42f, 0.42f, 0.48f));
        baseObject.transform.localPosition = Vector3.zero;

        GameObject marker = CreateSphere(shrineRoot.transform, "ShrineMarker", new Vector3(0f, 0.74f, 0f), new Vector3(0.75f, 0.75f, 0.75f), CreateGroundMaterial(), new Color(0.7f, 0.86f, 0.95f));
        TrySetEmission(marker.GetComponent<MeshRenderer>().sharedMaterial, new Color(0.08f, 0.18f, 0.24f));
        AttachLoopEmitter(marker, LoopfallCue.ShrineLoop, 0.12f, 1f, 1f, 2f, 18f);

        GameObject triggerObject = new("InteractionTrigger");
        triggerObject.transform.SetParent(shrineRoot.transform, false);
        SphereCollider trigger = triggerObject.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = ScaleWorld(2.4f);

        Shrine shrine = triggerObject.AddComponent<Shrine>();
        shrine.Configure(FragmentType.Stabilizing);
    }

    private void CreateMemoryFragment(Transform parent, Vector3 position, FragmentType fragmentType)
    {
        GameObject fragmentObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fragmentObject.name = $"{fragmentType} Fragment";
        fragmentObject.transform.SetParent(parent, false);
        fragmentObject.transform.localPosition = ScaleWorld(position);
        fragmentObject.transform.localScale = Vector3.one * 0.58f;
        fragmentObject.transform.localRotation = Quaternion.Euler(35f, 35f, 0f);

        BoxCollider collider = fragmentObject.GetComponent<BoxCollider>();
        collider.center = Vector3.zero;
        collider.size = Vector3.one;
        collider.isTrigger = true;

        Rigidbody body = fragmentObject.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;

        MemoryFragment fragment = fragmentObject.AddComponent<MemoryFragment>();
        fragment.Configure(fragmentType);

        Color color = fragmentType == FragmentType.Stabilizing
            ? new Color(0.48f, 0.9f, 0.82f)
            : new Color(0.84f, 0.38f, 0.42f);

        Material material = CreateColorMaterial($"{fragmentType}FragmentMaterial", color);
        TrySetEmission(material, color * 0.18f);
        fragmentObject.GetComponent<MeshRenderer>().sharedMaterial = material;
    }

    private GameObject CreatePath(Transform parent, string name, Vector3 position, Vector3 scale, Quaternion rotation, PhysicsMaterial physicsMaterial, Color color)
    {
        return CreatePlatform(parent, name, position, scale, rotation, physicsMaterial, color);
    }

    private GameObject CreatePlatform(Transform parent, string name, Vector3 position, Vector3 scale, Quaternion rotation, PhysicsMaterial physicsMaterial, Color color)
    {
        return CreateBox(parent, name, position, scale, rotation, physicsMaterial, color);
    }

    private GameObject CreateBox(Transform parent, string name, Vector3 position, Vector3 scale, Quaternion rotation, PhysicsMaterial physicsMaterial, Color color)
    {
        GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
        platform.name = name;
        platform.transform.SetParent(parent, false);
        platform.transform.localPosition = ScaleWorld(position);
        platform.transform.localRotation = rotation;
        platform.transform.localScale = ScaleWorld(scale);

        ConfigureBoxCollider(platform, physicsMaterial);

        platform.GetComponent<MeshRenderer>().sharedMaterial = CreateColorMaterial($"{name}Material", color);
        return platform;
    }

    private GameObject CreateCylinder(Transform parent, string name, Vector3 position, Vector3 scale, PhysicsMaterial physicsMaterial, Color color)
    {
        GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        platform.name = name;
        platform.transform.SetParent(parent, false);
        platform.transform.localPosition = ScaleWorld(position);
        platform.transform.localRotation = Quaternion.identity;
        platform.transform.localScale = ScaleWorld(scale);

        ConfigureStaticMeshCollider(platform, physicsMaterial);

        platform.GetComponent<MeshRenderer>().sharedMaterial = CreateColorMaterial($"{name}Material", color);
        return platform;
    }

    private GameObject CreateSphere(Transform parent, string name, Vector3 position, Vector3 scale, PhysicsMaterial physicsMaterial, Color color)
    {
        GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        platform.name = name;
        platform.transform.SetParent(parent, false);
        platform.transform.localPosition = ScaleWorld(position);
        platform.transform.localRotation = Quaternion.identity;
        platform.transform.localScale = ScaleWorld(scale);

        ConfigureStaticMeshCollider(platform, physicsMaterial);

        platform.GetComponent<MeshRenderer>().sharedMaterial = CreateColorMaterial($"{name}Material", color);
        return platform;
    }

    private void CreateOrganicPatch(Transform parent, string name, Vector3 position, Vector3 scale, Quaternion rotation, PhysicsMaterial physicsMaterial, Color color)
    {
        GameObject patch = CreateCylinder(parent, name, position, scale, physicsMaterial, color);
        patch.transform.localRotation = rotation;
    }

    private void CreateFlowerCluster(Transform parent, Vector3 center, Color color)
    {
        for (int i = 0; i < 5; i++)
        {
            float angle = i / 5f * Mathf.PI * 2f;
            Vector3 petalPosition = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 0.7f;
            CreateFlowerMarker(parent, petalPosition, color);
        }
    }

    private void CreateFlowerMarker(Transform parent, Vector3 position, Color color)
    {
        GameObject flower = CreateSphere(parent, $"Flower_{position.x:F1}_{position.z:F1}", position, new Vector3(0.35f, 0.18f, 0.35f), CreateGroundMaterial(), color);
        MakeVisualOnly(flower);
    }

    private void CreateLilyPad(Transform parent, Vector3 position, float scale)
    {
        GameObject pad = CreateCylinder(parent, $"LilyPad_{position.x:F1}_{position.z:F1}", position, new Vector3(scale, PondLilyPadHalfHeight, scale), CreateGroundMaterial(), new Color(0.42f, 0.72f, 0.42f));
        pad.transform.localRotation = Quaternion.Euler(0f, scale * 40f, 0f);
    }

    private void CreateScatteredLilyPads(Transform parent, Vector3 center, float y)
    {
        const int count = 18;
        const float goldenAngle = 137.508f * Mathf.Deg2Rad;

        for (int i = 0; i < count; i++)
        {
            float wobble = Mathf.PerlinNoise(i * 0.37f, 4.2f);
            float angle = i * goldenAngle + wobble * 0.9f;
            float radius = Mathf.Lerp(0.95f, 3.15f, Mathf.Repeat(i * 0.37f + wobble * 0.45f, 1f));
            Vector3 offset = new(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            Vector3 position = center + offset;

            if (Vector3.Distance(position, PondFragmentPlatformCenter) < 0.9f)
            {
                position += offset.normalized * 0.55f;
            }

            if (Vector3.Distance(position, new Vector3(-0.3f, 0f, 15.9f)) < 0.8f)
            {
                position += offset.normalized * 0.45f;
            }

            float scale = Mathf.Lerp(0.16f, 0.29f, Mathf.PerlinNoise(i * 0.21f, 9.1f));
            CreateLilyPad(parent, new Vector3(position.x, y, position.z), scale);
        }
    }

    private void CreatePondFragmentPlatform(Transform parent)
    {
        GameObject platform = CreateCylinder(parent, "PondFragmentPedestal", PondFragmentPlatformCenter, new Vector3(0.78f, 0.22f, 0.78f), CreateGroundMaterial(), new Color(0.56f, 0.66f, 0.48f));
        TrySetEmission(platform.GetComponent<MeshRenderer>().sharedMaterial, new Color(0.03f, 0.05f, 0.025f));
    }

    private void CreateBridgeLantern(Transform parent, Vector3 position)
    {
        GameObject post = CreateCylinder(parent, $"OldBoardwalkPost_{position.x:F1}_{position.z:F1}", position, new Vector3(0.11f, 0.52f, 0.11f), CreateGroundMaterial(), new Color(0.28f, 0.24f, 0.21f));
        post.transform.localRotation = Quaternion.Euler(0f, position.x * 16f, position.z > 14f ? -8f : 7f);

        GameObject cap = CreateSphere(post.transform, "FaintLanternCap", new Vector3(0f, 0.7f, 0f), new Vector3(0.22f, 0.08f, 0.22f), CreateGroundMaterial(), new Color(0.86f, 0.74f, 0.52f));
        GameObject glow = CreateSphere(post.transform, "FaintLanternGlow", new Vector3(0f, 0.82f, 0f), new Vector3(0.18f, 0.18f, 0.18f), CreateGroundMaterial(), new Color(0.96f, 0.82f, 0.46f));

        MakeVisualOnly(cap);
        MakeVisualOnly(glow);
        TrySetEmission(glow.GetComponent<MeshRenderer>().sharedMaterial, new Color(0.16f, 0.09f, 0.02f));
    }

    private void CreatePondEntranceLanterns(Transform parent)
    {
        Vector3 direction = ResolvePondEntranceDirection();
        Vector3 tangent = new(-direction.z, 0f, direction.x);
        Vector3 basePosition = PondCenter + direction * 4.65f;

        CreateBridgeLantern(parent, basePosition + tangent * 0.82f + Vector3.up * 0.48f);
        CreateBridgeLantern(parent, basePosition - tangent * 0.82f + Vector3.up * 0.48f);
    }

    private void CreatePondBankSegments(Transform parent, Vector3 center, float radius, PhysicsMaterial groundMaterial)
    {
        const int segmentCount = 28;
        float tangentLength = (Mathf.PI * 2f * radius / segmentCount) * 1.08f;
        Vector3 entranceDirection = ResolvePondEntranceDirection();

        for (int i = 0; i < segmentCount; i++)
        {
            float angle = i / (float)segmentCount * Mathf.PI * 2f;
            Vector3 radial = new(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            if (Vector3.Dot(radial, entranceDirection) > 0.88f)
            {
                continue;
            }

            Vector3 position = center + radial * radius;
            position.y = 0.94f + Mathf.Sin(i * 1.73f) * 0.018f;

            float radialWidth = 0.82f + Mathf.PerlinNoise(i * 0.27f, 0.45f) * 0.2f;
            float height = 1.22f + Mathf.PerlinNoise(i * 0.19f, 0.88f) * 0.18f;
            Color bankColor = Color.Lerp(new Color(0.38f, 0.54f, 0.31f), new Color(0.48f, 0.61f, 0.38f), Mathf.PerlinNoise(i * 0.31f, 0.18f));

            CreateBox(
                parent,
                $"PondBank_{i:00}",
                position,
                new Vector3(tangentLength, height, radialWidth),
                Quaternion.Euler(0f, -Mathf.Rad2Deg * angle, 0f),
                groundMaterial,
                bankColor);
        }
    }

    private void CreatePondEntrance(Transform parent, Vector3 center, float radius, PhysicsMaterial groundMaterial)
    {
        Vector3 direction = ResolvePondEntranceDirection();
        float angle = Mathf.Atan2(direction.z, direction.x);
        Vector3 position = center + direction * (radius + 0.08f);
        position.y = 0.31f;

        CreateBox(
            parent,
            "PondEntranceShelf",
            position,
            new Vector3(2.4f, 0.18f, 1.35f),
            Quaternion.Euler(0f, -Mathf.Rad2Deg * angle, 0f),
            groundMaterial,
            new Color(0.52f, 0.66f, 0.42f));
    }

    private static Vector3 ResolvePondEntranceDirection()
    {
        Vector3 direction = PondCenter - new Vector3(PondFragmentPosition.x, 0f, PondFragmentPosition.z);
        direction.y = 0f;
        return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
    }

    private void CreatePalmTree(Transform parent, Vector3 position, float height)
    {
        GameObject trunk = CreateCylinder(parent, $"PalmTrunk_{position.x:F1}_{position.z:F1}", position + Vector3.up * (height * 0.5f), new Vector3(0.22f, height * 0.5f, 0.22f), CreateGroundMaterial(), new Color(0.42f, 0.3f, 0.18f));

        for (int i = 0; i < 6; i++)
        {
            float angle = i / 6f * 360f;
            Vector3 frondOffset = Quaternion.Euler(0f, angle, 0f) * new Vector3(0f, 0f, 1.15f);
            GameObject frond = CreateSphere(trunk.transform, $"Frond_{i}", new Vector3(frondOffset.x, height * 0.8f, frondOffset.z), new Vector3(0.55f, 0.14f, 1.55f), CreateGroundMaterial(), new Color(0.26f, 0.58f, 0.28f));
            MakeVisualOnly(frond);
            frond.transform.localRotation = Quaternion.Euler(18f, angle, 14f);
        }
    }

    private void CreateScatteredPebbles(Transform parent, Vector3 center, int count, float radius)
    {
        for (int i = 0; i < count; i++)
        {
            float angle = i / (float)count * Mathf.PI * 2f;
            float distance = radius * (0.45f + (i % 3) * 0.2f);
            Vector3 position = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * distance;
            GameObject pebble = CreateSphere(parent, $"Pebble_{i}_{center.x:F1}", position, new Vector3(0.55f + (i % 2) * 0.18f, 0.18f, 0.42f + (i % 3) * 0.08f), CreateGroundMaterial(), new Color(0.58f, 0.6f, 0.62f));
            pebble.transform.localRotation = Quaternion.Euler(0f, angle * Mathf.Rad2Deg, 0f);
        }
    }

    private static float ScaleWorld(float value)
    {
        return value * Config.WorldScale;
    }

    private static Vector3 ScaleWorld(Vector3 value)
    {
        return value * Config.WorldScale;
    }

    private void MakeVisualOnly(GameObject target)
    {
        foreach (Collider collider in target.GetComponents<Collider>())
        {
            collider.enabled = false;
        }
    }

    private void ConfigureBoxCollider(GameObject target, PhysicsMaterial physicsMaterial)
    {
        BoxCollider collider = target.GetComponent<BoxCollider>();
        if (collider == null)
        {
            collider = target.AddComponent<BoxCollider>();
        }

        collider.enabled = true;
        collider.center = Vector3.zero;
        collider.size = Vector3.one;
        collider.material = physicsMaterial;

        foreach (Collider extraCollider in target.GetComponents<Collider>())
        {
            if (extraCollider != collider)
            {
                extraCollider.enabled = false;
            }
        }
    }

    private void ConfigureStaticMeshCollider(GameObject target, PhysicsMaterial physicsMaterial)
    {
        MeshFilter meshFilter = target.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            return;
        }

        MeshCollider meshCollider = target.GetComponent<MeshCollider>();
        if (meshCollider == null)
        {
            meshCollider = target.AddComponent<MeshCollider>();
        }

        meshCollider.enabled = true;
        meshCollider.sharedMesh = meshFilter.sharedMesh;
        meshCollider.convex = false;
        meshCollider.material = physicsMaterial;

        foreach (Collider extraCollider in target.GetComponents<Collider>())
        {
            if (extraCollider != meshCollider)
            {
                extraCollider.enabled = false;
            }
        }
    }

    private GameObject CreateWaterHazardDisk(Transform parent, string name, Vector3 center, float radius, float surfaceY, string reason)
    {
        GameObject hazard = new(name);
        hazard.transform.SetParent(parent, false);
        hazard.transform.localPosition = ScaleWorld(new Vector3(center.x, surfaceY, center.z));
        hazard.transform.localRotation = Quaternion.identity;

        float worldRadius = ScaleWorld(radius);
        float worldDiameter = worldRadius * 2f;
        float triggerPadding = Config.SoccerBallRadius * 2f;

        BoxCollider trigger = hazard.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.center = Vector3.zero;
        trigger.size = new Vector3(worldDiameter + triggerPadding, Config.SoccerBallDiameter + 0.28f, worldDiameter + triggerPadding);

        HazardVolume hazardVolume = hazard.AddComponent<HazardVolume>();
        hazardVolume.ConfigureSurfaceDisk(reason, hazard.transform.position, worldRadius, ScaleWorld(surfaceY));
        return hazard;
    }

    private GameObject CreateHazardCylinder(Transform parent, string name, Vector3 position, Vector3 scale, string reason)
    {
        GameObject hazard = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        hazard.name = name;
        hazard.transform.SetParent(parent, false);
        hazard.transform.localPosition = ScaleWorld(position);
        hazard.transform.localRotation = Quaternion.identity;
        hazard.transform.localScale = ScaleWorld(scale);

        MeshRenderer renderer = hazard.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.enabled = false;
        }

        ConfigureTriggerMeshCollider(hazard);

        HazardVolume hazardVolume = hazard.AddComponent<HazardVolume>();
        hazardVolume.Configure(reason);
        return hazard;
    }

    private void ConfigureTriggerMeshCollider(GameObject target)
    {
        MeshFilter meshFilter = target.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            return;
        }

        MeshCollider meshCollider = target.GetComponent<MeshCollider>();
        if (meshCollider == null)
        {
            meshCollider = target.AddComponent<MeshCollider>();
        }

        meshCollider.enabled = true;
        meshCollider.sharedMesh = meshFilter.sharedMesh;
        meshCollider.convex = true;
        meshCollider.isTrigger = true;

        foreach (Collider extraCollider in target.GetComponents<Collider>())
        {
            if (extraCollider != meshCollider)
            {
                extraCollider.enabled = false;
            }
        }
    }

    private void CreateMazeWall(Transform parent, string name, Vector3 position, Vector3 scale, float yRotation, PhysicsMaterial material)
    {
        CreatePlatform(parent, name, position, scale, Quaternion.Euler(0f, yRotation, 0f), material, new Color(0.28f, 0.49f, 0.22f));
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
            dynamicFriction = 0.06f,
            staticFriction = 0.05f,
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
            dynamicFriction = 0.78f,
            staticFriction = 0.82f,
            bounciness = 0f,
            frictionCombine = PhysicsMaterialCombine.Average,
            bounceCombine = PhysicsMaterialCombine.Minimum,
        };

        return material;
    }

    private PhysicsMaterial CreateSlickGroundMaterial()
    {
        PhysicsMaterial material = new("LoopfallSlickGroundPhysics")
        {
            dynamicFriction = 0.18f,
            staticFriction = 0.16f,
            bounciness = 0f,
            frictionCombine = PhysicsMaterialCombine.Minimum,
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

    private void TrySetEmission(Material material, Color emission)
    {
        if (material == null)
        {
            return;
        }

        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission);
        }
    }

    private void AttachLoopEmitter(GameObject target, LoopfallCue cue, float volume, float pitch, float spatialBlend, float minDistance, float maxDistance)
    {
        LoopfallAudioEmitter emitter = target.GetComponent<LoopfallAudioEmitter>();
        if (emitter == null)
        {
            emitter = target.AddComponent<LoopfallAudioEmitter>();
        }

        emitter.Configure(cue, volume, pitch, spatialBlend, ScaleWorld(minDistance), ScaleWorld(maxDistance));
    }

}
