using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using TMPro;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

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

    public Dictionary<string, SpriteRenderStatus> spriteRenderStatusDict
    {
        get;
        private set;
    }

    private ServerClient serverClient;

    public TMP_InputField seedMapInput;

    public Vector2 playerPosTileIndex;

    public Canvas canvas;

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

        // var (texture, direction) = CreateInpaintSourceTexture(new Vector2(0, 0), new Vector2(1, -1), ExtendDirection.BottomRight);
        // // save texture
        // string filePath = Path.Combine(Application.persistentDataPath, "InpaintSource" + direction + ".png");
        // byte[] imageBytes = texture.EncodeToPNG();
        // File.WriteAllBytes(filePath, imageBytes);
        // Debug.Log("Image saved successfully at " + filePath);
    }


    // Update is called once per frame
    void Update()
    {
        // In this we need to see if a new tile should be placed
        // Get user position
        var seedId = TileTools.GenerateId(0, 0);
        if (!spriteRenderStatusDict.ContainsKey(seedId) || spriteRenderStatusDict[seedId] == SpriteRenderStatus.Pending)
        {
            return;
        }
        Vector3 userPosition = Camera.main.transform.position;
        CheckAdditionalMapTile(new Vector2(userPosition.x, userPosition.y));

    }

    public void OnStartGame()
    {
        // this will be called when user clicks the button
        string userSceneInput = seedMapInput.text;
        if (string.IsNullOrEmpty(userSceneInput))
        {
            return;
        }
        // turn off canvas 
        canvas.gameObject.SetActive(false);
        GenerateInitialMapTile(userSceneInput);
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
        float verHorzOffset = spriteSquareSize / 2;
        Vector2 currentTileIndex = new Vector2((int)Math.Round(userPosition.x / verHorzOffset), (int)Math.Round(userPosition.y / verHorzOffset));

        Vector2 userPos = new Vector2(userPosition.x, userPosition.y);


        Vector2 topPos = userPos + new Vector2(0, verHorzOffset);
        Vector2 bottomPos = userPos + new Vector2(0, -verHorzOffset);
        Vector2 leftPos = userPos + new Vector2(-verHorzOffset, 0);
        Vector2 rightPos = userPos + new Vector2(verHorzOffset, 0);

        float diagOffset = (float)(Math.Sqrt(2) * verHorzOffset) / 2;

        Vector2 topLeftPos = userPos + new Vector2(-diagOffset, diagOffset);
        Vector2 topRightPos = userPos + new Vector2(diagOffset, diagOffset);
        Vector2 bottomLeftPos = userPos + new Vector2(-diagOffset, -diagOffset);
        Vector2 bottomRightPos = userPos + new Vector2(diagOffset, -diagOffset);

        Vector2 topPosInt = new Vector2((int)Math.Round(topPos.x / verHorzOffset), (int)Math.Round(topPos.y / verHorzOffset));
        Vector2 bottomPosInt = new Vector2((int)Math.Round(bottomPos.x / verHorzOffset), (int)Math.Round(bottomPos.y / verHorzOffset));
        Vector2 leftPosInt = new Vector2((int)Math.Round(leftPos.x / verHorzOffset), (int)Math.Round(leftPos.y / verHorzOffset));
        Vector2 rightPosInt = new Vector2((int)Math.Round(rightPos.x / verHorzOffset), (int)Math.Round(rightPos.y / verHorzOffset));

        Vector2 topLeftPosInt = new Vector2((int)Math.Round(topLeftPos.x / verHorzOffset), (int)Math.Round(topLeftPos.y / verHorzOffset));
        Vector2 topRightPosInt = new Vector2((int)Math.Round(topRightPos.x / verHorzOffset), (int)Math.Round(topRightPos.y / verHorzOffset));
        Vector2 bottomLeftPosInt = new Vector2((int)Math.Round(bottomLeftPos.x / verHorzOffset), (int)Math.Round(bottomLeftPos.y / verHorzOffset));
        Vector2 bottomRightPosInt = new Vector2((int)Math.Round(bottomRightPos.x / verHorzOffset), (int)Math.Round(bottomRightPos.y / verHorzOffset));

        Dictionary<ExtendDirection, Vector2> directionTileIndex = new Dictionary<ExtendDirection, Vector2>
        {
            {ExtendDirection.Up, topPosInt},
            {ExtendDirection.Left, leftPosInt},
            {ExtendDirection.Down, bottomPosInt},
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
                GenerateAdditionalMapTile((int)tileIndex.x, (int)tileIndex.y, direction);
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

        // check if there are any existing children in the map container
        if (mapContainer.transform.childCount != 0)
        {
            // Get the children and add them to the dictionary
            for (int i = 0; i < mapContainer.transform.childCount; i++)
            {
                GameObject child = mapContainer.transform.GetChild(i).gameObject;
                string tileId = child.name;
                spriteRenderStatusDict.Add(tileId, SpriteRenderStatus.Rendered);
            }
        }
    }

    private async void GenerateInitialMapTile(string prompt)
    {
        try
        {
            string seedTileId = TileTools.GenerateId(0, 0);
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
        Debug.Log("Image inpainted successfully for " + tileId);
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


    private async void GenerateAdditionalMapTile(int indexX, int indexY, ExtendDirection direction)
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
            var (sourceTexture, extendDirection) = CreateInpaintSourceTexture(
                new Vector2((int)currentTileIndex.x, (int)currentTileIndex.y),
                new Vector2(indexX, indexY),
                direction
            );
            if (sourceTexture == null)
            {
                return;
            }
            byte[] imageBytes = sourceTexture.EncodeToPNG();
            string directionName = Enum.GetName(typeof(ExtendDirection), extendDirection);

            string filePath = Path.Combine(Application.persistentDataPath, "InpaintSource" + tileId + ".png");
            File.WriteAllBytes(filePath, imageBytes);

            Debug.Log("Inpainting image..." + directionName + new Vector2(indexX, indexY));
            int sourceX = (int)currentTileIndex.x;
            int sourceY = (int)currentTileIndex.y;
            if (spriteRenderStatusDict.ContainsKey(tileId))
            {
                spriteRenderStatusDict[tileId] = SpriteRenderStatus.Pending;
            }
            else
            {
                spriteRenderStatusDict.Add(tileId, SpriteRenderStatus.Pending);
            }
            Texture2D result = await this.serverClient.InpaintImageAsync(
                filePath,
                sourceX,
                sourceY,
                indexX,
                indexY,
                directionName,
                posPrompt: ""
            );
            // Save the image to the disk
            string outputFilePath = Path.Combine(Application.persistentDataPath, "InpaintOutput" + tileId + ".png");
            byte[] resultBytes = result.EncodeToPNG();
            File.WriteAllBytes(outputFilePath, resultBytes);
            Debug.Log("Image inpainted successfully for " + directionName);
            HandleInpaintImageSuccess(result, new Vector2(indexX, indexY));
        }
        catch (Exception e)
        {
            // remove the pending status
            string tileId = TileTools.GenerateId(indexX, indexY);
            spriteRenderStatusDict.Remove(tileId);
            Debug.LogError(e);
        }

        // GameObject spriteObject = CreateSpriteGameObject(imagePath, new Vector2(indexX, indexY));
        // return spriteObject;
    }

    private List<(GameObject, Vector2)> GetRenderedNeighborTileIndex(Vector2 tileIndex)
    {
        Dictionary<ExtendDirection, Vector2> directionTileIndex = new Dictionary<ExtendDirection, Vector2>
        {
            {ExtendDirection.Down, new Vector2(tileIndex.x, tileIndex.y + 1)},
            {ExtendDirection.Up, new Vector2(tileIndex.x, tileIndex.y - 1)},
            {ExtendDirection.Right, new Vector2(tileIndex.x - 1, tileIndex.y)},
            {ExtendDirection.Left, new Vector2(tileIndex.x + 1, tileIndex.y)},
            // {ExtendDirection.BottomRight, new Vector2(tileIndex.x - 1, tileIndex.y + 1)},
            // {ExtendDirection.BottomLeft, new Vector2(tileIndex.x + 1, tileIndex.y + 1)},
            // {ExtendDirection.TopRight, new Vector2(tileIndex.x - 1, tileIndex.y - 1)},
            // {ExtendDirection.TopLeft, new Vector2(tileIndex.x + 1, tileIndex.y - 1)}
        };

        List<(GameObject, Vector2)> existingNeighborTiles = new List<(GameObject, Vector2)>();
        bool isAnyPending = false;

        foreach (var direction in directionTileIndex.Keys)
        {
            string tileId = TileTools.GenerateId((int)directionTileIndex[direction].x, (int)directionTileIndex[direction].y);

            if (spriteRenderStatusDict.ContainsKey(tileId))
            {
                if (spriteRenderStatusDict[tileId] == SpriteRenderStatus.Rendered)
                {
                    existingNeighborTiles.Add((GetTileMapAt(directionTileIndex[direction]), directionTileIndex[direction]));
                }
                else if (spriteRenderStatusDict[tileId] == SpriteRenderStatus.Pending)
                {
                    isAnyPending = true;
                }
            }
        }
        if (isAnyPending)
        {
            return new List<(GameObject, Vector2)>();
        }
        return existingNeighborTiles;
    }

    private (Texture2D, ExtendDirection) CreateInpaintSourceTexture(Vector2 sourceIndex, Vector2 targetIndex, ExtendDirection direction)
    {
        // we just need to extend half of the image
        try
        {
            var neighboringTiles = GetRenderedNeighborTileIndex(targetIndex);
            // for (int i = 0; i < neighboringTiles.Count; i++)
            // {
            //     Debug.Log(neighboringTiles[i].Item2);
            // }

            var seedTile = GetTileMapAt(new Vector2(0, 0));
            SpriteRenderer sourceSpriteRenderer = seedTile.GetComponent<SpriteRenderer>();
            Sprite sourceSprite = sourceSpriteRenderer.sprite;
            Texture2D sourceTexture = sourceSprite.texture;
            int width = sourceTexture.width;
            int height = sourceTexture.height;
            Texture2D newTexture = new Texture2D(width, height);

            Color32[] newPixels = new Color32[width * height];
            ExtendDirection extendDirection = ExtendDirection.Up;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    newPixels[y * width + x] = new Color32(0, 0, 0, 0);
                }
            }
            if (neighboringTiles.Count == 0)
            {
                return (null, ExtendDirection.Up);
            }
            else if (neighboringTiles.Count == 1)
            {
                Vector2 offset = targetIndex - neighboringTiles[0].Item2;
                int offsetX = (int)offset.x;
                int offsetY = (int)offset.y;
                Color32[] pixels = neighboringTiles[0].Item1.GetComponent<SpriteRenderer>().sprite.texture.GetPixels32();

                // we will not do anything when the offset is diagonal


                fillInPixels(pixels, newPixels, offsetX, offsetY);

                if (offsetX == 0 && offsetY == 1)
                {
                    // top
                    extendDirection = ExtendDirection.Up;
                }
                else if (offsetX == 0 && offsetY == -1)
                {
                    // bottom
                    extendDirection = ExtendDirection.Down;
                }
                else if (offsetX == -1 && offsetY == 0)
                {
                    // left
                    extendDirection = ExtendDirection.Left;
                }
                else if (offsetX == 1 && offsetY == 0)
                {
                    // right
                    extendDirection = ExtendDirection.Right;
                }
                else
                {
                    return (null, ExtendDirection.Up);
                }
            }
            else if (neighboringTiles.Count == 2)
            {
                Vector2 offset1 = targetIndex - neighboringTiles[0].Item2;
                Vector2 offset2 = targetIndex - neighboringTiles[1].Item2;
                int offsetX1 = (int)offset1.x;
                int offsetY1 = (int)offset1.y;
                int offsetX2 = (int)offset2.x;
                int offsetY2 = (int)offset2.y;

                Color32[] pixels1 = neighboringTiles[0].Item1.GetComponent<SpriteRenderer>().sprite.texture.GetPixels32();
                Color32[] pixels2 = neighboringTiles[1].Item1.GetComponent<SpriteRenderer>().sprite.texture.GetPixels32();

                Debug.LogWarning("Inpainting fill in pixel for two neighbors" + offset1 + offset2);
                fillInPixels(pixels1, newPixels, offsetX1, offsetY1);
                fillInPixels(pixels2, newPixels, offsetX2, offsetY2);
                // we will only check diagonals
                if ((offset1 == Vector2.up && offset2 == Vector2.right)
                     || (offset1 == Vector2.right && offset2 == Vector2.up))
                {
                    extendDirection = ExtendDirection.TopRight;
                }
                else if ((offset1 == Vector2.up && offset2 == Vector2.left)
                     || (offset1 == Vector2.left && offset2 == Vector2.up))
                {
                    extendDirection = ExtendDirection.TopLeft;
                }
                else if ((offset1 == Vector2.down && offset2 == Vector2.right)
                     || (offset1 == Vector2.right && offset2 == Vector2.down))
                {
                    extendDirection = ExtendDirection.BottomRight;
                }
                else if ((offset1 == Vector2.down && offset2 == Vector2.left)
                     || (offset1 == Vector2.left && offset2 == Vector2.down))
                {
                    extendDirection = ExtendDirection.BottomLeft;
                }
                else
                {
                    return (null, ExtendDirection.Up);
                }

            }
            newTexture.SetPixels32(newPixels);
            newTexture.Apply();
            return (newTexture, extendDirection);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            return (null, ExtendDirection.Up);
        }
    }

    private void fillInPixels(Color32[] sourcePixels, Color32[] targetPixels, int offsetX, int offsetY)
    {
        int width = (int)Math.Sqrt(sourcePixels.Length);
        int height = width;
        if (offsetX == 0 && offsetY == 1)
        {
            // top
            for (int y = height / 2; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    targetPixels[(y - height / 2) * width + x] = sourcePixels[y * width + x];
                }
            }
        }
        else if (offsetX == 0 && offsetY == -1)
        {
            // bottom
            for (int y = 0; y < height / 2; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    targetPixels[(y + height / 2) * width + x] = sourcePixels[y * width + x];
                }
            }
        }
        else if (offsetX == -1 && offsetY == 0)
        {
            // left
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width / 2; x++)
                {
                    targetPixels[y * width + (x + width / 2)] = sourcePixels[y * width + x];
                }
            }
        }
        else if (offsetX == 1 && offsetY == 0)
        {
            // right
            for (int y = 0; y < height; y++)
            {
                for (int x = width / 2; x < width; x++)
                {
                    targetPixels[y * width + (x - width / 2)] = sourcePixels[y * width + x];
                }
            }
        }

    }
}
