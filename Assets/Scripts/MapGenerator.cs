using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Networking;

public enum ExtendDirection
{
    Up,
    Down,
    Left,
    Right,
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight
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

    private Dictionary<string, SpriteRenderStatus> spriteRenderStatusDict;

    private ServerClient serverClient;

    public Vector2 playerPosTileIndex;

    private string baseUrl = "http://127.0.0.1:8765";  // Change this if your server IP differs


    public bool isGameTileMapInitialzied
    {
        get
        {
            if (this.spriteRenderStatusDict == null)
            {
                return false;
            }
            if (!spriteRenderStatusDict.ContainsKey(TileTools.GenerateId(0, 0)))
            {
                return false;
            }
            if (spriteRenderStatusDict[TileTools.GenerateId(0, 0)] == SpriteRenderStatus.Pending)
            {
                return false;
            }
            return true;
        }
    }

    void Awake()
    {
        this.serverClient = transform.gameObject.GetComponent<ServerClient>();
    }

    void Start()
    {
        Initialize();
    }

    // Update is called once per frame
    void Update()
    {
        // In this we need to see if a new tile should be placed
        // Get user position
        var seedId = TileTools.GenerateId(0, 0);
        if (spriteRenderStatusDict[seedId] == SpriteRenderStatus.Pending)
        {
            return;
        }
        Vector3 userPosition = Camera.main.transform.position;
        CheckAdditionalMapTile(new Vector2(userPosition.x, userPosition.y));

    }

    GameObject GetTileMapAt(Vector2 tileIndex)
    {
        string tileId = TileTools.GenerateId((int)tileIndex.x, (int)tileIndex.y);
        return mapContainer.transform.Find(tileId).gameObject;
    }

    void CheckAdditionalMapTile(Vector2 userPosition)
    {
        // Check if we need to add a new tile
        // Get the user position in the map
        Vector2 topPos = new Vector2(userPosition.x, userPosition.y + spriteSquareSize / 2);
        Vector2 bottomPos = new Vector2(userPosition.x, userPosition.y - spriteSquareSize / 2);
        Vector2 leftPos = new Vector2(userPosition.x - spriteSquareSize / 2, userPosition.y);
        Vector2 rightPos = new Vector2(userPosition.x + spriteSquareSize / 2, userPosition.y);

        Vector2 topLeftPos = new Vector2(userPosition.x - spriteSquareSize / 2, userPosition.y + (float)(spriteSquareSize / Math.Sqrt(2)));
        Vector2 topRightPos = new Vector2(userPosition.x + spriteSquareSize / 2, userPosition.y + (float)(spriteSquareSize / Math.Sqrt(2)));
        Vector2 bottomLeftPos = new Vector2(userPosition.x - spriteSquareSize / 2, userPosition.y - (float)(spriteSquareSize / Math.Sqrt(2)));
        Vector2 bottomRightPos = new Vector2(userPosition.x + spriteSquareSize / 2, userPosition.y - (float)(spriteSquareSize / Math.Sqrt(2)));

        Vector2 topPosInt = new Vector2((int)Math.Round(topPos.x / (spriteSquareSize / 2)), (int)Math.Round(topPos.y / (spriteSquareSize / 2)));
        Vector2 bottomPosInt = new Vector2((int)Math.Round(bottomPos.x / (spriteSquareSize / 2)), (int)Math.Round(bottomPos.y / (spriteSquareSize / 2)));
        Vector2 leftPosInt = new Vector2((int)Math.Round(leftPos.x / (spriteSquareSize / 2)), (int)Math.Round(leftPos.y / (spriteSquareSize / 2)));
        Vector2 rightPosInt = new Vector2((int)Math.Round(rightPos.x / (spriteSquareSize / 2)), (int)Math.Round(rightPos.y / (spriteSquareSize / 2)));

        Vector2 topLeftPosInt = new Vector2((int)Math.Round(topLeftPos.x / (spriteSquareSize / 2)), (int)Math.Round(topLeftPos.y / (spriteSquareSize / 2)));
        Vector2 topRightPosInt = new Vector2((int)Math.Round(topRightPos.x / (spriteSquareSize / 2)), (int)Math.Round(topRightPos.y / (spriteSquareSize / 2)));
        Vector2 bottomLeftPosInt = new Vector2((int)Math.Round(bottomLeftPos.x / (spriteSquareSize / 2)), (int)Math.Round(bottomLeftPos.y / (spriteSquareSize / 2)));
        Vector2 bottomRightPosInt = new Vector2((int)Math.Round(bottomRightPos.x / (spriteSquareSize / 2)), (int)Math.Round(bottomRightPos.y / (spriteSquareSize / 2)));

        Dictionary<ExtendDirection, Vector2> directionTileIndex = new Dictionary<ExtendDirection, Vector2>
        {
            {ExtendDirection.Up, topPosInt},
            {ExtendDirection.Down, bottomPosInt},
            {ExtendDirection.Left, leftPosInt},
            {ExtendDirection.Right, rightPosInt},
            {ExtendDirection.TopLeft, topLeftPosInt},
            {ExtendDirection.TopRight, topRightPosInt},
            {ExtendDirection.BottomLeft, bottomLeftPosInt},
            {ExtendDirection.BottomRight, bottomRightPosInt}
        };

        for (int i = 0; i < directionTileIndex.Count; i++)
        {
            ExtendDirection direction = (ExtendDirection)i;
            Vector2 tileIndex = directionTileIndex[direction];
            string tileId = TileTools.GenerateId((int)tileIndex.x, (int)tileIndex.y);
            if (!spriteRenderStatusDict.ContainsKey(tileId))
            {
                // Generate the tile
                // Debug.Log((int)tileIndex.x + ", " + (int)tileIndex.y);
                // StartCoroutine(GenerateAdditionalMapTile((int)tileIndex.x, (int)tileIndex.y, direction));
                GenerateAdditionalMapTile((int)tileIndex.x, (int)tileIndex.y, direction);
                // StartCoroutine(GenerateAdditionalMapTile((int)tileIndex.x, (int)tileIndex.y, direction));
            }
        }

    }

    void Initialize()
    {
        // Get camera width and height
        var camera = Camera.main;
        float cameraWidth = camera.orthographicSize * 2 * camera.aspect;
        float cameraHeight = camera.orthographicSize * 2;
        this.spriteSquareSize = cameraWidth * 1.2f;


        // We need to initialize 
        this.spriteRenderStatusDict = new Dictionary<string, SpriteRenderStatus>();
        GenerateInitialMapTile();
    }

    private async void GenerateInitialMapTile()
    {
        try
        {
            string seedTileId = TileTools.GenerateId(0, 0);
            string prompt = "A 2D game sprite, Pixel art, 64 bit, top-view, 2d game map, urban, dessert, town, open world";
            spriteRenderStatusDict.Add(seedTileId, SpriteRenderStatus.Pending);
            Debug.Log("Generating image...");
            Texture2D result = await this.serverClient.GenerateImageAsync(
                prompt
            );
            Debug.Log("Image generated successfully");
            HandleGenerateImageSuccess(result);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }

    void HandleGenerateImageSuccess(Texture2D texture)
    {
        // this will only be for seed image 
        string seedTileId = TileTools.GenerateId(0, 0);
        // create new game object
        GameObject spriteGameObject = CreateSpriteGameObject(texture, new Vector2(0, 0));
        spriteRenderStatusDict[seedTileId] = SpriteRenderStatus.Rendered;
    }

    void HandleGenerateImageError(string error)
    {
        Debug.LogError(error);
    }

    void HandleInpaintImageSuccess(Texture2D texture, Vector2 tileIndex)
    {
        // we will need to know the tile index also
        // save the image to the disk
        string tileId = TileTools.GenerateId((int)tileIndex.x, (int)tileIndex.y);
        // string filePath = Path.Combine(Application.persistentDataPath, "Inpaint" + tileId + ".png");
        // byte[] imageBytes = texture.EncodeToPNG();
        // File.WriteAllBytes(filePath, imageBytes);

        GameObject spriteGameObject = CreateSpriteGameObject(texture, tileIndex);
        spriteRenderStatusDict[tileId] = SpriteRenderStatus.Rendered;
        Debug.Log("Image inpainted successfully");
    }

    void HandleInpaintImageError(string error)
    {
        Debug.LogError(error);
    }

    GameObject CreateSpriteGameObject(Texture2D texture, Vector2 tileIndex)
    {
        string tileId = TileTools.GenerateId((int)tileIndex.x, (int)tileIndex.y);
        spriteRenderStatusDict[tileId] = SpriteRenderStatus.Pending;

        if (texture != null)
        {
            GameObject spriteGameObject = new GameObject();

            // Create a sprite from the given texture
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));

            // Add sprite to the game object
            spriteGameObject.AddComponent<SpriteRenderer>().sprite = sprite;

            // Get the size of the game object and scale accordingly
            Vector2 spriteSize = spriteGameObject.GetComponent<SpriteRenderer>().bounds.size;
            float scale = spriteSquareSize / spriteSize.x;
            spriteGameObject.transform.localScale = new Vector3(scale, scale, 1);

            // Position the game object
            spriteGameObject.transform.position = new Vector3(tileIndex.x * (spriteSquareSize / 2), tileIndex.y * (spriteSquareSize / 2), 0);

            // Set the parent to the map container
            spriteGameObject.transform.parent = mapContainer.transform;

            Debug.Log("Texture applied successfully");

            // Change the name of the game object
            spriteGameObject.name = tileId;
            spriteRenderStatusDict[tileId] = SpriteRenderStatus.Rendered;

            return spriteGameObject;
        }
        else
        {
            Debug.LogError("Invalid texture provided");
            throw new Exception("Invalid texture provided");
        }
    }

    // private IEnumerator GenerateAdditionalMapTile(int indexX, int indexY, ExtendDirection direction)
    // {
    //     // Convert index to ID
    //     string tileId = TileTools.GenerateId(indexX, indexY);

    //     // If we already have a status for this tile, skip
    //     if (spriteRenderStatusDict.ContainsKey(tileId))
    //     {
    //         yield break;
    //     }

    //     // Mark this tile as "pending" to avoid duplicates
    //     spriteRenderStatusDict[tileId] = SpriteRenderStatus.Pending;

    //     // 1) Prepare your local file -> in this example we assume you have a method
    //     //    CreateInpaintSourceTexture that returns a Texture2D or null
    //     Vector3 userPosition = Camera.main.transform.position;
    //     Vector2 currentTileIndex = new Vector2((int)Math.Round(userPosition.x / (spriteSquareSize / 2)), (int)Math.Round(userPosition.y / (spriteSquareSize / 2)));
    //     Texture2D sourceTexture = CreateInpaintSourceTexture(new Vector2((int)currentTileIndex.x, (int)currentTileIndex.y), direction);
    //     if (sourceTexture == null)
    //     {
    //         // If there's no source, just stop and remove the tile from the dictionary if needed
    //         spriteRenderStatusDict.Remove(tileId);
    //         yield break;
    //     }

    //     // Convert to bytes, write to disk, etc.
    //     byte[] imageBytes = sourceTexture.EncodeToPNG();
    //     string directionName = Enum.GetName(typeof(ExtendDirection), direction);
    //     string filePath = Path.Combine(Application.persistentDataPath, $"InpaintSource_{directionName}.png");
    //     File.WriteAllBytes(filePath, imageBytes);

    //     Debug.Log("Inpaint source image saved to: " + filePath);

    //     // 2) Create a UnityWebRequest POST form
    //     WWWForm form = new WWWForm();
    //     form.AddBinaryData("image_file", imageBytes, Path.GetFileName(filePath), "image/png");
    //     form.AddField("pos_prompt", "A 2D game sprite, Pixel art...");
    //     form.AddField("neg_prompt", "3D, walls, unnatural...");
    //     form.AddField("extend_direction", directionName);

    //     // 3) Send the request
    //     using (UnityWebRequest request = UnityWebRequest.Post(baseUrl + "/inpaint", form))
    //     {
    //         // We want to download a texture from this request
    //         request.downloadHandler = new DownloadHandlerTexture(true);

    //         // yield return the send operation so the coroutine waits until the request finishes
    //         yield return request.SendWebRequest();

    //         // 4) Check the result
    //         if (request.result == UnityWebRequest.Result.Success)
    //         {
    //             Texture2D resultTexture = DownloadHandlerTexture.GetContent(request);
    //             Debug.Log("Inpaint operation succeeded for tile: " + tileId);

    //             // 5) Handle success (create sprite, mark dictionary as rendered, etc.)
    //             HandleInpaintImageSuccess(resultTexture, new Vector2(indexX, indexY));
    //         }
    //         else
    //         {
    //             Debug.LogError($"Inpaint request error for tile {tileId}: {request.error}");
    //             // Optionally revert the dictionary state
    //             spriteRenderStatusDict.Remove(tileId);
    //         }
    //     }
    // }



    private async void GenerateAdditionalMapTile(int indexX, int indexY, ExtendDirection direction, string imagePath = "Assets/Images/example.jpeg")
    {
        try
        {
            string tileId = TileTools.GenerateId(indexX, indexY);
            if (spriteRenderStatusDict.ContainsKey(tileId))
            {
                return;
            }
            Vector3 userPosition = Camera.main.transform.position;
            Vector2 currentTileIndex = new Vector2((int)Math.Round(userPosition.x / (spriteSquareSize / 2)), (int)Math.Round(userPosition.y / (spriteSquareSize / 2)));
            Texture2D sourceTexture = CreateInpaintSourceTexture(new Vector2((int)currentTileIndex.x, (int)currentTileIndex.y), direction);
            if (sourceTexture == null)
            {
                return;
            }
            byte[] imageBytes = sourceTexture.EncodeToPNG();
            string directionName = Enum.GetName(typeof(ExtendDirection), direction);

            string filePath = Path.Combine(Application.persistentDataPath, "InpaintSource" + directionName + ".png");
            File.WriteAllBytes(filePath, imageBytes);

            Debug.Log("Inpainting image..." + directionName);
            spriteRenderStatusDict.Add(tileId, SpriteRenderStatus.Pending);
            Texture2D result = await this.serverClient.InpaintImageAsync(
                filePath,
                indexX,
                indexY,
                directionName,
                posPrompt: ""
            );
            // Save the image to the disk
            string outputFilePath = Path.Combine(Application.persistentDataPath, "InpaintOutput" + directionName + ".png");
            byte[] resultBytes = result.EncodeToPNG();
            File.WriteAllBytes(outputFilePath, resultBytes);
            Debug.Log("Image inpainted successfully for " + directionName);
            HandleInpaintImageSuccess(result, new Vector2(indexX, indexY));
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }

        // GameObject spriteObject = CreateSpriteGameObject(imagePath, new Vector2(indexX, indexY));
        // spriteRenderStatusDict[tileId] = SpriteRenderStatus.Rendered;
        // return spriteObject;
    }

    private Texture2D CreateInpaintSourceTexture(Vector2 sourceIndex, ExtendDirection direction)
    {
        bool isDiagonalDirection = direction == ExtendDirection.TopLeft || direction == ExtendDirection.TopRight || direction == ExtendDirection.BottomLeft || direction == ExtendDirection.BottomRight;

        if (!isDiagonalDirection)
        {
            // we just need to extend half of the image
            GameObject sourceTile = GetTileMapAt(sourceIndex);
            SpriteRenderer sourceSpriteRenderer = sourceTile.GetComponent<SpriteRenderer>();
            Sprite sourceSprite = sourceSpriteRenderer.sprite;
            Texture2D sourceTexture = sourceSprite.texture;
            int width = sourceTexture.width;
            int height = sourceTexture.height;
            Texture2D newTexture = new Texture2D(width, height);

            Color32[] pixels = sourceTexture.GetPixels32();
            Color32[] newPixels = new Color32[width * height];

            switch (direction)
            {
                case ExtendDirection.Up:
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            newPixels[y * width + x] = new Color32(0, 0, 0, 0);
                        }
                    }
                    for (int y = height / 2; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            newPixels[(y - height / 2) * width + x] = pixels[y * width + x];
                        }
                    }
                    break;
                case ExtendDirection.Down:
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            newPixels[y * width + x] = new Color32(0, 0, 0, 0);
                        }
                    }
                    for (int y = 0; y < height / 2; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            newPixels[(y + height / 2) * width + x] = pixels[y * width + x];
                        }
                    }
                    break;
                case ExtendDirection.Right:
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            newPixels[y * width + x] = new Color32(0, 0, 0, 0);
                        }
                    }
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = width / 2; x < width; x++)
                        {
                            newPixels[y * width + (x - width / 2)] = pixels[y * width + x];
                        }
                    }
                    break;
                case ExtendDirection.Left:
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            newPixels[y * width + x] = new Color32(0, 0, 0, 0);
                        }
                    }
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width / 2; x++)
                        {
                            newPixels[y * width + (x + width / 2)] = pixels[y * width + x];
                        }
                    }
                    break;

            }
            newTexture.SetPixels32(newPixels);
            newTexture.Apply();
            return newTexture;
        }
        return null;

    }
}
