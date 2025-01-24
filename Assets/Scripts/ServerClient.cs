using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections;
using Cysharp.Threading.Tasks;
using System.Threading;


public class ServerClient : MonoBehaviour
{
    private string baseUrl = "http://127.0.0.1:8765";  // Change this if your server IP differs
    public GameObject targetGameObject;  // Assign the GameObject with the sprite in Unity

    private CancellationTokenSource cts;

    void Awake()
    {
        cts = new CancellationTokenSource();
    }



    public async UniTask<Texture2D> GenerateImageAsync(string prompt)
    {
        string endpoint = "/gen";
        string url = $"{baseUrl}{endpoint}?pos_prompt={UnityWebRequest.EscapeURL(prompt)}";

        // Create the request
        var request = UnityWebRequestTexture.GetTexture(url);
        var operation = request.SendWebRequest();


        try
        {
            // Wait until done or canceled
            await UniTask.WaitUntil(() => operation.isDone, cancellationToken: cts.Token);

            if (request.result == UnityWebRequest.Result.Success)
            {
                return DownloadHandlerTexture.GetContent(request);
            }
            else
            {
                throw new Exception($"Request failed: {request.error}");
            }
        }
        catch (OperationCanceledException)
        {
            Debug.LogWarning("GenerateImageAsync was canceled.");
            return null;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Exception in GenerateImageAsync: {ex.Message}");
            return null;
        }
        finally
        {
            request.Dispose();
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
        int source_x,
        int source_y,
        int target_x,
        int target_y,
        string direction,
        string posPrompt = "A 2D game sprite, Pixel art, 64 bit, top-view, 2d game map, urban, dessert, town, open world",
        string negPrompt = "3D, walls, unnatural, rough, unrealistic, closed area, towered, limited, side view, watermark, signature, artist, inappropriate content, objects, game ui, ui, buttons, walled, grid, character, white edges, single portrait, edged, island, bottom ui, bottom blocks, player, creatures, life, uneven roads, human, living"
    )
    {

        if (!File.Exists(imagePath))
        {
            throw new Exception("Image file not found: " + imagePath);
        }

        byte[] imageBytes = File.ReadAllBytes(imagePath);

        WWWForm form = new WWWForm();
        form.AddBinaryData("image_file", imageBytes, Path.GetFileName(imagePath), "image/png");
        form.AddField("pos_prompt", posPrompt);
        form.AddField("neg_prompt", negPrompt);
        form.AddField("source_x", source_x.ToString());
        form.AddField("source_y", source_y.ToString());
        form.AddField("target_x", target_x.ToString());
        form.AddField("target_y", target_y.ToString());
        form.AddField("extend_direction", direction);

        var request = UnityWebRequest.Post(baseUrl + "/inpaint", form);
        request.downloadHandler = new DownloadHandlerTexture(true);

        var operation = request.SendWebRequest();

        try
        {
            // Wait until done or canceled
            await UniTask.WaitUntil(() => operation.isDone, cancellationToken: cts.Token);

            if (request.result == UnityWebRequest.Result.Success)
            {
                Texture2D resultTexture = DownloadHandlerTexture.GetContent(request);
                Debug.Log("Inpaint operation succeeded.");
                return resultTexture;
            }
            else
            {
                throw new Exception($"Inpaint request error: {request.error}");
            }
        }
        catch (OperationCanceledException)
        {
            Debug.LogWarning("InpaintImageAsync was canceled.");
            return null;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Exception in InpaintImageAsync: {ex.Message}");
            return null;
        }
        finally
        {
            request.Dispose();
        }
    }

    void OnDestroy()
    {
        CancelAllRequests();
    }

    void OnApplicationQuit()
    {
        CancelAllRequests();
    }

    private void CancelAllRequests()
    {
        if (cts != null && !cts.IsCancellationRequested)
        {
            cts.Cancel();
            cts.Dispose();
            Debug.Log("All pending web requests canceled.");
        }
    }
}
