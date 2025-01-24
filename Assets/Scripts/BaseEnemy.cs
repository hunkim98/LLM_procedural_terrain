using System;
using UnityEngine;

public class BaseEnemy : MonoBehaviour
{
    public float speed = 1.0f;

    private bool isPlayerInSight = false;
    void Awake()
    {

        Camera camera = Camera.main;
        foreach (Transform child in camera.transform)
        {
            Physics2D.IgnoreCollision(GetComponent<Collider2D>(), child.GetComponent<Collider2D>());
        }
    }

    void Update()
    {
        Vector3 userPosition = Camera.main.transform.position;

        Vector3 enemyPosition = transform.position;

        // we should get the direction of the player
        Vector3 direction = userPosition - enemyPosition;

        // we should get the angle
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // if in user camera view, we should rotate the enemy to the player
        Camera camera = Camera.main;

        bool isInCameraView = camera.pixelRect.Contains(camera.WorldToScreenPoint(transform.position));

        if (!isInCameraView)
        {
            return;
        }

        // we should rotate the enemy to the player, but we should rotate it by 8 directions
        // top, top right, right, bottom right, bottom, bottom left, left, top left
        if (angle > -22.5 && angle <= 22.5)
        {
            // right
            transform.rotation = Quaternion.Euler(0, 0, 270);
        }
        else if (angle > 22.5 && angle <= 67.5)
        {
            // top right
            transform.rotation = Quaternion.Euler(0, 0, -45);
        }
        else if (angle > 67.5 && angle <= 112.5)
        {
            // top
            transform.rotation = Quaternion.Euler(0, 0, 0);
        }
        else if (angle > 112.5 && angle <= 157.5)
        {
            // top left
            transform.rotation = Quaternion.Euler(0, 0, 45);
        }
        else if (angle > 157.5 || angle <= -157.5)
        {
            // left
            transform.rotation = Quaternion.Euler(0, 0, 90);
        }
        else if (angle > -157.5 && angle <= -112.5)
        {
            // bottom left
            transform.rotation = Quaternion.Euler(0, 0, 135);
        }
        else if (angle > -112.5 && angle <= -67.5)
        {
            // bottom
            transform.rotation = Quaternion.Euler(0, 0, 180);
        }
        else if (angle > -67.5 && angle <= -22.5)
        {
            // bottom right
            transform.rotation = Quaternion.Euler(0, 0, -135);
        }
        // we should also make the enemy approach the player

        // since we already rotated the enemy to the player, we can just move the enemy forward
        transform.position += transform.up * speed * Time.deltaTime;
    }
}