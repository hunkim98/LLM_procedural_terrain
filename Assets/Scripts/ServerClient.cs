using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using System;
using System.Threading.Tasks;

public class ServerClient : MonoBehaviour
{
    private string baseUrl = "http://127.0.0.1:8765";  // Change this if your server IP differs
    public GameObject targetGameObject;  // Assign the GameObject with the sprite in Unity

    // Events for generating an image
    public event Action<Texture2D> OnGenerateImageSuccess;
    public event Action<string> OnGenerateImageError;

    // Events for inpainting an image
    public event Action<Texture2D> OnInpaintImageSuccess;
    public event Action<string> OnInpaintImageError;

    // Call the /gen endpoint (GET request)
    public async void GenerateImageAsync(string prompt)
    {
        string endpoint = "/gen";
        string url = baseUrl + endpoint + "?pos_prompt=" + prompt;

        try
        {
            Texture2D texture = await GetImageAsync(url);
            OnGenerateImageSuccess?.Invoke(texture);
            ApplyTextureToGameObject(texture);
        }
        catch (Exception ex)
        {
            OnGenerateImageError?.Invoke($"Error fetching image: {ex.Message}");
        }
    }

    // Call the /inpaint endpoint (POST request with an image)
    public async void InpaintImageAsync(GameObject targetGameObject, Vector2 location)
    {
        string endpoint = "/inpaint";
        string url = baseUrl + endpoint;
        string savedImagePath = ProcessImage(targetGameObject);
        
        if (savedImagePath != null)
        {
            try
            {
                Texture2D texture = await PostImageAsync(url, savedImagePath, location);
                OnInpaintImageSuccess?.Invoke(texture);
                ApplyTextureToGameObject(texture);
            }
            catch (Exception ex)
            {
                OnInpaintImageError?.Invoke($"Error inpainting image: {ex.Message}");
            }
        }
        else
        {
            OnInpaintImageError?.Invoke("Image processing failed.");
        }
    }

    // Asynchronous function to GET an image
    private async Task<Texture2D> GetImageAsync(string url)
    {
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            var asyncOp = request.SendWebRequest();

            while (!asyncOp.isDone)
            {
                await Task.Yield();  // Non-blocking wait
            }

            if (request.result == UnityWebRequest.Result.Success)
            {
                return DownloadHandlerTexture.GetContent(request);
            }
            else
            {
                throw new Exception(request.error);
            }
        }
    }

    // Asynchronous function to POST an image
    private async Task<Texture2D> PostImageAsync(string url, string imagePath, Vector2 location)
    {
        if (!File.Exists(imagePath))
        {
            throw new Exception("Image file not found.");
        }

        byte[] imageBytes = File.ReadAllBytes(imagePath);
        WWWForm form = new WWWForm();
        form.AddBinaryData("image_file", imageBytes, Path.GetFileName(imagePath), "image/png");

        // add field makes additional data sent to the server
        form.AddField("x", location.x.ToString());
        form.AddField("y", location.y.ToString());

        using (UnityWebRequest request = UnityWebRequest.Post(url, form))
        {
            var asyncOp = request.SendWebRequest();

            while (!asyncOp.isDone)
            {
                await Task.Yield();
            }

            if (request.result == UnityWebRequest.Result.Success)
            {
                return DownloadHandlerTexture.GetContent(request);
            }
            else
            {
                throw new Exception(request.error);
            }
        }
    }

    // Image processing: modify sprite and save locally
    private string ProcessImage(GameObject targetGameObject)
    {
        Sprite sprite = targetGameObject.GetComponent<SpriteRenderer>().sprite;
        if (sprite == null)
        {
            Debug.LogError("No sprite found on target game object.");
            return null;
        }

        Texture2D processedTexture = CreateNewTexture(sprite.texture);
        byte[] imageBytes = processedTexture.EncodeToPNG();

        string filePath = Path.Combine(Application.persistentDataPath, "ProcessedImage.png");
        File.WriteAllBytes(filePath, imageBytes);

        Debug.Log("Processed image saved to: " + filePath);
        return filePath;
    }

    // Modify the texture: move right half to left and make the right half transparent
    private Texture2D CreateNewTexture(Texture2D originalTexture)
    {
        int width = originalTexture.width;
        int height = originalTexture.height;
        Texture2D newTexture = new Texture2D(width, height);

        Color32[] pixels = originalTexture.GetPixels32();
        Color32[] newPixels = new Color32[width * height];

        // Move the right half of the image to the left half
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width / 2; x++)
            {
                newPixels[y * width + x] = pixels[y * width + (x + width / 2)];
            }
        }

        // Set the right half to transparent (mask effect)
        for (int y = 0; y < height; y++)
        {
            for (int x = width / 2; x < width; x++)
            {
                newPixels[y * width + x] = new Color32(0, 0, 0, 0);  // Fully transparent pixel
            }
        }

        newTexture.SetPixels32(newPixels);
        newTexture.Apply();

        return newTexture;
    }

    // Apply received image to the target GameObject
    private void ApplyTextureToGameObject(Texture2D newTexture)
    {
        if (targetGameObject != null)
        {
            targetGameObject.GetComponent<SpriteRenderer>().sprite = Sprite.Create(
                newTexture,
                new Rect(0, 0, newTexture.width, newTexture.height),
                new Vector2(0.5f, 0.5f)
            );

            Debug.Log("New image applied to GameObject.");
        }
    }
}
