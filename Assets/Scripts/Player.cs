using UnityEngine;

public class Player : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private ServerClient serverClient;

    private MapGenerator mapGenerator;


    void Awake()
    {
        this.serverClient = transform.gameObject.GetComponent<ServerClient>();
        this.mapGenerator = transform.gameObject.GetComponent<MapGenerator>();
    }

    // Update is called once per frame
    void Update()
    {

        if (!mapGenerator.isGameTileMapInitialzied)
        {
            // if not initialized, we will not move the player
            return;
        }
        // get keyboard event left and right top move the player
        float speed = Time.deltaTime * 4f;
        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
        {
            transform.position += new Vector3(-speed, 0, 0);
        }
        else if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
        {
            transform.position += new Vector3(speed, 0, 0);
        }

        if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
        {
            transform.position += new Vector3(0, speed, 0);
        }
        else if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
        {
            transform.position += new Vector3(0, -speed, 0);
        }

    }
}
