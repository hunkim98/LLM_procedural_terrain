using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using UnityEngine;

public enum ExtendDirection
{
    Up,
    Down,
    Left,
    Right
}

public enum SpriteRenderStatus
{
    Pending,
    Rendered
}


public class MapGenerator : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public GameObject mapContainer;
    public GameObject[] mapTiles;

    private float spriteSquareSize;

    private Dictionary<String, SpriteRenderStatus> spriteRenderStatusDict;

    private 
    void Start()
    {
        Initialize();
  
    }

    // Update is called once per frame
    void Update()
    {
        // In this we need to see if a new tile should be placed
        // Get user position
        Vector3 userPosition = Camera.main.transform.position;
        CheckAdditionalMapTile(new Vector2(userPosition.x, userPosition.y));
        
    }

    void CheckAdditionalMapTile(Vector2 userPosition)
    {
        // Check if we need to add a new tile
        // Get the user position in the map
        Vector2 topPos = new Vector2(userPosition.x, userPosition.y + spriteSquareSize / 2);
        Vector2 bottomPos = new Vector2(userPosition.x, userPosition.y - spriteSquareSize / 2);
        Vector2 leftPos = new Vector2(userPosition.x - spriteSquareSize / 2, userPosition.y);
        Vector2 rightPos = new Vector2(userPosition.x + spriteSquareSize / 2, userPosition.y);

        Vector2 topLeftPos = new Vector2(userPosition.x - spriteSquareSize / 2, userPosition.y + (float)(spriteSquareSize/Math.Sqrt(2)));
        Vector2 topRightPos = new Vector2(userPosition.x + spriteSquareSize / 2, userPosition.y + (float)(spriteSquareSize/Math.Sqrt(2)));
        Vector2 bottomLeftPos = new Vector2(userPosition.x - spriteSquareSize / 2, userPosition.y - (float)(spriteSquareSize/Math.Sqrt(2)));
        Vector2 bottomRightPos = new Vector2(userPosition.x + spriteSquareSize / 2, userPosition.y - (float)(spriteSquareSize/Math.Sqrt(2)));

        Vector2 topPosInt = new Vector2((int) Math.Round(topPos.x / (spriteSquareSize/2)), (int) Math.Round(topPos.y / (spriteSquareSize/2)));
        Vector2 bottomPosInt = new Vector2((int) Math.Round(bottomPos.x / (spriteSquareSize/2)), (int) Math.Round(bottomPos.y / (spriteSquareSize/2)));
        Vector2 leftPosInt = new Vector2((int) Math.Round(leftPos.x / (spriteSquareSize/2)), (int) Math.Round(leftPos.y / (spriteSquareSize/2)));
        Vector2 rightPosInt = new Vector2((int) Math.Round(rightPos.x / (spriteSquareSize/2)), (int) Math.Round(rightPos.y / (spriteSquareSize/2)));

        Vector2 topLeftPosInt = new Vector2((int) Math.Round(topLeftPos.x / (spriteSquareSize/2)), (int) Math.Round(topLeftPos.y / (spriteSquareSize/2)));
        Vector2 topRightPosInt = new Vector2((int) Math.Round(topRightPos.x / (spriteSquareSize/2)), (int) Math.Round(topRightPos.y / (spriteSquareSize/2)));
        Vector2 bottomLeftPosInt = new Vector2((int) Math.Round(bottomLeftPos.x / (spriteSquareSize/2)), (int) Math.Round(bottomLeftPos.y / (spriteSquareSize/2)));
        Vector2 bottomRightPosInt = new Vector2((int) Math.Round(bottomRightPos.x / (spriteSquareSize/2)), (int) Math.Round(bottomRightPos.y / (spriteSquareSize/2)));

        Debug.Log("Top: " + topPosInt.x + "-" + topPosInt.y);
        // Debug.Log("Right: " + rightPosInt.x + "-" + rightPosInt.y);
        // Debug.Log("Bottom: " + bottomPosInt.x + "-" + bottomPosInt.y);
        // Debug.Log("Left: " + leftPosInt.x + "-" + leftPosInt.y);

        // Debug.Log("TopLeft: " + topLeftPosInt.x + "-" + topLeftPosInt.y);
        // Debug.Log("TopRight: " + topRightPosInt.x + "-" + topRightPosInt.y); 
        // Debug.Log("BottomLeft: " + bottomLeftPosInt.x + "-" + bottomLeftPosInt.y);
        // Debug.Log("BottomRight: " + bottomRightPosInt.x + "-" + bottomRightPosInt.y);

        Vector2[] checkPositions = new Vector2[] {topPosInt, bottomPosInt, leftPosInt, rightPosInt, topLeftPosInt, topRightPosInt, bottomLeftPosInt, bottomRightPosInt};
        for (int i = 0; i < checkPositions.Length; i++)
        {
            // check if the key is contianed
            string checkPositionTileId = TileTools.GenerateId((int)checkPositions[i].x, (int)checkPositions[i].y);
            if (!spriteRenderStatusDict.ContainsKey(checkPositionTileId))
            {
                // Generate the tile
                GenerateAdditionalMapTile((int)checkPositions[i].x, (int)checkPositions[i].y);
            }
        }

    }

    void Initialize() 
    {
        // Get camera width and height
        var camera = Camera.main;
        float cameraWidth = camera.orthographicSize * 2 * camera.aspect;
        float cameraHeight = camera.orthographicSize * 2;
        this.spriteSquareSize = cameraWidth;


        Debug.Log("Camera width: " + cameraWidth);
        Debug.Log("Camera height: " + cameraHeight);

        // We need to initialize 
        ExtractMapTilesFromContainer();
        InitializeSpriteRenderStatusDict();
        GenerateInitialMapTile();
    }

    void ExtractMapTilesFromContainer()
    {
        // Extract map tiles from container
        mapTiles = new GameObject[mapContainer.transform.childCount];
        for (int i = 0; i < mapContainer.transform.childCount; i++)
        {
            mapTiles[i] = mapContainer.transform.GetChild(i).gameObject;
        }
    }

    void InitializeSpriteRenderStatusDict()
    {
        spriteRenderStatusDict = new Dictionary<string, SpriteRenderStatus>();
    }

    GameObject CreateSpriteGameObject(string imagePath, Vector2 tileIndex)
    {
        byte[] imageBytes = File.ReadAllBytes(imagePath);
        Texture2D texture = new Texture2D(2, 2);
        string tileId = TileTools.GenerateId((int)tileIndex.x, (int)tileIndex.y);
        spriteRenderStatusDict[tileId] = SpriteRenderStatus.Pending;
        if (texture.LoadImage(imageBytes))
        {
            GameObject spriteGameObject = new GameObject();
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            // scale the sprite to the size of spriteSquareSize
            // float scale = spriteSquareSize / sprite.texture.width;
            // float scale = 1;
            // add sprite to the game object
            spriteGameObject.AddComponent<SpriteRenderer>().sprite = sprite;
            // get size of the game object
            Vector2 spriteSize = spriteGameObject.GetComponent<SpriteRenderer>().bounds.size;
            float scale = spriteSquareSize / spriteSize.x;
            spriteGameObject.transform.localScale = new Vector3(scale, scale, 1);
            // position the game object
            spriteGameObject.transform.position = new Vector3(tileIndex.x * (spriteSquareSize/2), tileIndex.y * (spriteSquareSize/2), 0);
            // add the game object to the map container
            spriteGameObject.transform.parent = mapContainer.transform;
            Debug.Log("Image loaded successfully");
            // change the name of the game object
            spriteGameObject.name = tileId;
            spriteRenderStatusDict[tileId] = SpriteRenderStatus.Rendered;

            return spriteGameObject;
        }
        else
        {
            Debug.LogError("Image failed to load");
            throw new Exception("Image failed to load");
        }
    }

    void GenerateInitialMapTile()
    {
        string seedTileId = TileTools.GenerateId(0, 0);
        spriteRenderStatusDict.Add(seedTileId, SpriteRenderStatus.Pending);
        // In the real world this should generate the initial map tiles 
        string imagePath = "Assets/Images/example.jpeg";
        GameObject spriteObject = CreateSpriteGameObject(imagePath, new Vector2(0, 0));
    }

    GameObject GenerateAdditionalMapTile(int indexX, int indexY, string imagePath = "Assets/Images/example.jpeg")
    {
        string tileId = TileTools.GenerateId(indexX, indexY);
        spriteRenderStatusDict.Add(tileId, SpriteRenderStatus.Pending);
        GameObject spriteObject = CreateSpriteGameObject(imagePath, new Vector2(indexX, indexY));
        spriteRenderStatusDict[tileId] = SpriteRenderStatus.Rendered;
        return spriteObject;
    }
}
