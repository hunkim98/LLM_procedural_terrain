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
            spriteGameObject.transform.position = new Vector3(tileIndex.x * spriteSquareSize, tileIndex.y * spriteSquareSize, 0);
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

    void GenerateAdditionalMapTile(int positionX, int positionY)
    {

    }
}
