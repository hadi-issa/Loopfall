using UnityEngine;

public class GameManager : MonoBehaviour
{
    // UI
    public GameObject startMenu;

    // Gameplay
    public GameObject player;
    public GameObject pickUps;

    // Internal state
    private Vector3 playerStartPos;
    private Quaternion playerStartRot;

    private void Start()
    {
        // Cache starting position for relaunch
        playerStartPos = player.transform.position;
        playerStartRot = player.transform.rotation;

        // Freeze gameplay until Start is pressed
        player.SetActive(false);
        pickUps.SetActive(false);

        startMenu.SetActive(true);
    }

    // -------------------------
    // MAIN MENU BUTTONS
    // -------------------------

    public void StartGame()
    {
        startMenu.SetActive(false);

        player.SetActive(true);
        pickUps.SetActive(true);
    }

    public void RelaunchGame()
    {
        // Reset player
        player.transform.position = playerStartPos;
        player.transform.rotation = playerStartRot;

        // Reset pickups (simple version: re-enable all)
        foreach (Transform t in pickUps.transform)
            t.gameObject.SetActive(true);

        // Reset any future systems here (loops, fragments, etc.)

        startMenu.SetActive(false);
        player.SetActive(true);
        pickUps.SetActive(true);
    }

    public void ExitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
