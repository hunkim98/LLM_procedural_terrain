using System;
using UnityEngine;


public class Player : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private ServerClient serverClient;

    private MapGenerator mapGenerator;

    private ExtendDirection direction;

    public GameObject bulletPrefab;





    void Awake()
    {
        this.serverClient = transform.gameObject.GetComponent<ServerClient>();
        this.mapGenerator = transform.gameObject.GetComponent<MapGenerator>();
        this.direction = ExtendDirection.Up;
    }

    void isUserOutsideOfRendered()
    {

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
        float rotateDegree = 0;

        float speed = Time.deltaTime * 4f;
        bool isLeftPressed = Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A);
        bool isRightPressed = Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D);
        bool isUpPressed = Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W);
        bool isDownPressed = Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S);
        if (isLeftPressed)
        {
            transform.position += new Vector3(-speed, 0, 0);
            direction = ExtendDirection.Left;
        }
        else if (isRightPressed)
        {
            transform.position += new Vector3(speed, 0, 0);
            direction = ExtendDirection.Right;
        }

        if (isUpPressed)
        {
            transform.position += new Vector3(0, speed, 0);
            direction = ExtendDirection.Up;
        }
        else if (isDownPressed)
        {
            transform.position += new Vector3(0, -speed, 0);
            direction = ExtendDirection.Down;
        }

        // for both keys

        if (isUpPressed && isLeftPressed)
        {
            direction = ExtendDirection.TopLeft;
        }
        else if (isUpPressed && isRightPressed)
        {
            direction = ExtendDirection.TopRight;
        }
        else if (isDownPressed && isLeftPressed)
        {
            direction = ExtendDirection.BottomLeft;
        }
        else if (isDownPressed && isRightPressed)
        {
            direction = ExtendDirection.BottomRight;
        }

        switch (direction)
        {
            case ExtendDirection.Up:
                rotateDegree = 0;
                break;
            case ExtendDirection.Down:
                rotateDegree = 180;
                break;
            case ExtendDirection.Left:
                rotateDegree = 90;
                break;
            case ExtendDirection.Right:
                rotateDegree = 270;
                break;
            case ExtendDirection.TopLeft:
                rotateDegree = 45;
                break;
            case ExtendDirection.TopRight:
                rotateDegree = -45;
                break;
            case ExtendDirection.BottomLeft:
                rotateDegree = 135;
                break;
            case ExtendDirection.BottomRight:
                rotateDegree = -135;
                break;
            default:
                break;
        }

        // rotate the children sprite based on the player movement
        foreach (Transform child in transform)
        {
            child.rotation = Quaternion.Euler(0, 0, rotateDegree);
        }

        // shoot the bullet when the space key is pressed
        if (Input.GetKeyDown(KeyCode.Space))
        {
            GameObject bullet = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
            bullet.GetComponent<BaseBullet>().transform.rotation = Quaternion.Euler(0, 0, rotateDegree);
        }

    }
}
