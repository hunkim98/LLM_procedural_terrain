using UnityEngine;

public class BaseBullet : MonoBehaviour
{
    public float speed = 15.0f;

    void Start()
    {
        // change order in layer of sprite renderer
        // set z to 1
        GetComponent<SpriteRenderer>().sortingOrder = 1;
        transform.position = new Vector3(transform.position.x, transform.position.y, 1);

        // ignore collision with player

    }

    void Update()
    {
        transform.position += transform.up * speed * Time.deltaTime;

        Camera camera = Camera.main;

        // remove the bullet if it is outside of the camera view
        if (!camera.pixelRect.Contains(camera.WorldToScreenPoint(transform.position)))
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {

        if (other.gameObject.GetComponent<BaseEnemy>() != null)
        {
            Destroy(other.gameObject);
            Destroy(gameObject);
        }
    }

}