using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections;
using Cysharp.Threading.Tasks;


public class ServerClient : MonoBehaviour
{
    private string baseUrl = "http://127.0.0.1:8765";  // Change this if your server IP differs
    public GameObject targetGameObject;  // Assign the GameObject with the sprite in Unity


    public async UniTask<Texture2D> GenerateImageAsync(string prompt)
    {
        string endpoint = "/gen";
        string url = $"{baseUrl}{endpoint}?pos_prompt={UnityWebRequest.EscapeURL(prompt)}";

        using (var request = UnityWebRequestTexture.GetTexture(url))
        {
            await request.SendWebRequest().ToUniTask(); // Provided by UniTask

            if (request.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(request);
                return texture;
            }
            else
            {
                throw new Exception($"Error: {request.error}");
            }
        }
    }

    // Call the /inpaint endpoint (POST request with an image)
    // ---------------------------------------------
    // 2) INPAINT IMAGE (POST) via Coroutine
    // ---------------------------------------------
    // public void InpaintImageCoroutine(
    //     string maskImagePath,
    //     Vector2 location,
    //     string direction,
    //     string prompt = "A 2D game sprite, natural, Pixel art, 64 bit, top-view..."
    // )
    // {
    //     StartCoroutine(InpaintImageRoutine(maskImagePath, location, direction, prompt));
    // }


    public async UniTask<Texture2D> InpaintImageAsync(
        string imagePath,
        int x,
        int y,
        string direction,
        string posPrompt = "A 2D game sprite, Pixel art, 64 bit, top-view, 2d game map, urban, dessert, town, open world",
        string negPrompt = "3D, walls, unnatural, rough, unrealistic, closed area, towered, limited, side view, watermark, signature, artist, inappropriate content, objects, game ui, ui, buttons, walled, grid, character, white edges, single portrait, edged, island, bottom ui, bottom blocks, player, creatures, life, uneven roads, human, living"
    )
    {
        if (!File.Exists(imagePath))
        {
            throw new Exception("Image file not found: " + imagePath);
        }

        // Read the local image bytes
        byte[] imageBytes = File.ReadAllBytes(imagePath);

        // Prepare the form data
        WWWForm form = new WWWForm();
        form.AddBinaryData("image_file", imageBytes, Path.GetFileName(imagePath), "image/png");
        form.AddField("pos_prompt", posPrompt);
        form.AddField("neg_prompt", negPrompt);
        form.AddField("x", x.ToString());
        form.AddField("y", y.ToString());
        form.AddField("extend_direction", direction);

        using (UnityWebRequest request = UnityWebRequest.Post(baseUrl + "/inpaint", form))
        {
            // We want to download a texture from this request
            request.downloadHandler = new DownloadHandlerTexture(true);

            // Send request & await completion via UniTask
            await request.SendWebRequest().ToUniTask();

            // Check for errors
            if (request.result == UnityWebRequest.Result.Success)
            {
                // Convert response to a Texture2D
                Texture2D resultTexture = DownloadHandlerTexture.GetContent(request);
                Debug.Log("Inpaint operation succeeded.");
                return resultTexture;
            }
            else
            {
                throw new Exception($"Inpaint request error: {request.error}");
            }
        }
    }
}
