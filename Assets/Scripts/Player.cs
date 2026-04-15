using UnityEngine;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    public float speed = 10f;
    public int numberOfObjects = 12;

    public Text countText;
    public Text winText;

    private Rigidbody rb;
    private int count;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody>();

        count = 0;
        SetCountText();
        winText.text = "";
    }

    private void FixedUpdate()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");

        Vector3 movement = new Vector3(moveX, 0f, moveZ);
        rb.AddForce(movement * speed);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(Game.Tag.PickUp))
        {
            other.gameObject.SetActive(false);
            count++;
            SetCountText();
        }
    }

    private void SetCountText()
    {
        countText.text = "Count: " + count;

        if (count >= numberOfObjects)
        {
            winText.text = "You Win!";
        }
    }
}
