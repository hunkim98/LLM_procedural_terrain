from contextlib import asynccontextmanager
import time
from fastapi.responses import JSONResponse, StreamingResponse
import uvicorn
import asyncio
from typing import Union
import random
from PIL import Image
import numpy as np
from io import BytesIO
from fastapi.responses import Response
from threading import Semaphore
from transformers import AutoTokenizer, AutoModelForCausalLM
import threading
import LLMHelper

from fastapi import FastAPI, File, Form, UploadFile
import torch

from image_gen import get_value_at_index, import_custom_nodes, NODE_CLASS_MAPPINGS

import json
import os
import random
import sys
from typing import Sequence, Mapping, Any, Union
import torch

DEBUG = False

STEPS = 10

USE_LLM = False

NECESSARY_PROMPTS = (
    "A 2D game sprite, Pixel art, 64 bit, top-view, 2d tilemap, game, flat design"
)


gpu_lock = threading.Lock()


# Create a custom LockableSemaphore class
class LockableSemaphore(Semaphore):
    def __enter__(self):
        self.acquire()

    def __exit__(self, exc_type, exc_val, exc_tb):
        self.release()
        return False


gpu_semaphore = LockableSemaphore(value=2)


def generate_tile_id(x: int, y: int):
    return f"{x}_{y}"


@asynccontextmanager
async def lifespan(app: FastAPI):
    import_custom_nodes()

    with torch.inference_mode():
        checkpointloadersimple = NODE_CLASS_MAPPINGS["CheckpointLoaderSimple"]()
        checkpointloadersimple_1 = checkpointloadersimple.load_checkpoint(
            ckpt_name="pixelXL_xl.safetensors"
        )
        loadimage = NODE_CLASS_MAPPINGS["LoadImage"]()

        emptylatentimage = NODE_CLASS_MAPPINGS["EmptyLatentImage"]()
        emptylatentimage_2 = emptylatentimage.generate(
            width=768, height=768, batch_size=1
        )

        loraloader = NODE_CLASS_MAPPINGS["LoraLoader"]()
        loraloader_6 = loraloader.load_lora(
            lora_name="pixel-art-xl-v1.1.safetensors",
            strength_model=1,
            strength_clip=1,
            model=get_value_at_index(checkpointloadersimple_1, 0),
            clip=get_value_at_index(checkpointloadersimple_1, 1),
        )

        cliptextencode = NODE_CLASS_MAPPINGS["CLIPTextEncode"]()

        ksampler_efficient = NODE_CLASS_MAPPINGS["KSampler (Efficient)"]()

        vaeencodeforinpaint = NODE_CLASS_MAPPINGS["VAEEncodeForInpaint"]()

        llm_helper = LLMHelper.LLMHelper()

        tile_prompts = {}

        app.package = {
            "checkpoint": checkpointloadersimple_1,
            "clip_encode": cliptextencode,
            "empty_latent_image": emptylatentimage_2,
            "lora_loader": loraloader_6,
            "ksampler": ksampler_efficient,
            "load_image": loadimage,
            "vae_encode_for_inpaint": vaeencodeforinpaint,
            "llm_helper": llm_helper,
            "tile_prompts": tile_prompts,
        }
        # print(app.package)

    # Startup logic
    print("Application startup")
    yield
    # Shutdown logic
    # save the dictionary to a file
    with open("tile_prompts.json", "w") as f:
        json.dump(tile_prompts, f)
    print("Application shutdown haha!")


app = FastAPI(lifespan=lifespan)


@app.get("/")
def read_root():
    return {"Hello": "World"}


@app.get("/gen")
def gen(
    pos_prompt: str = "A 2D game sprite, Pixel art, 64 bit, top-view, 2d game map, urban, dessert, town, open world",
    neg_prompt: str = "3D, walls, unnatural, rough, unrealistic, closed area, towered, limited, side view, watermark, signature, artist, inappropriate content, objects, game ui, ui, buttons, walled, grid, character, white edges, single portrait, edged, island, bottom ui, bottom blocks, player, creatures, life, uneven roads, human, living, perspective, 3D, depth, shadows, vanishing point, isometric, gradient shading, foreshortening, parallax, skewed angles, distorted, photorealistic, realistic lighting, complex shading, dynamic lighting, occlusion",
):
    llm_helper = app.package["llm_helper"]
    tile_prompts = app.package["tile_prompts"]
    pos_prompt = "Help me create a top-view image prompt based on this: " + pos_prompt

    if USE_LLM:
        pos_prompt = NECESSARY_PROMPTS + llm_helper.chat(pos_prompt)
    # for efficiency purposes we will return existing image
    if DEBUG:
        with open("output.png", "rb") as f:
            return Response(content=f.read(), media_type="image/png")

    with torch.inference_mode():
        checkpoint = app.package["checkpoint"]
        loraloader = app.package["lora_loader"]
        # ksampler = app.package["ksampler"]
        ksampler = NODE_CLASS_MAPPINGS["KSampler"]()
        vaedecode = NODE_CLASS_MAPPINGS["VAEDecode"]()
        latent_image = app.package["empty_latent_image"]
        clip_encode = app.package["clip_encode"]

        positive_encode = clip_encode.encode(
            text=pos_prompt,
            clip=get_value_at_index(loraloader, 1),
        )

        negative_encode = clip_encode.encode(
            text=neg_prompt,
            clip=get_value_at_index(loraloader, 1),
        )

        ksampler_8 = ksampler.sample(
            seed=random.randint(1, 2**64),
            steps=STEPS,
            cfg=2.98,
            sampler_name="ddim",
            scheduler="karras",
            denoise=1,
            model=get_value_at_index(loraloader, 0),
            positive=get_value_at_index(positive_encode, 0),
            negative=get_value_at_index(negative_encode, 0),
            latent_image=get_value_at_index(latent_image, 0),
        )

        vaedecode_9 = vaedecode.decode(
            samples=get_value_at_index(ksampler_8, 0),
            vae=get_value_at_index(checkpoint, 2),
        )

        final_image = get_value_at_index(vaedecode_9, 0)[0]
        final_image = 255.0 * final_image.cpu().numpy()
        final_image = Image.fromarray(np.clip(final_image, 0, 255).astype(np.uint8))

        img_bytes = BytesIO()
        final_image.save(img_bytes, format="PNG")
        img_bytes.seek(0)
        print("Generated image for ", pos_prompt)
        tile_prompts[generate_tile_id(0, 0)] = pos_prompt

    return StreamingResponse(img_bytes, media_type="image/png")


async def wait_for_file(filepath: str, timeout: int = 10, interval: float = 0.5):
    """Waits for the file to be available with a timeout."""
    start_time = asyncio.get_event_loop().time()
    while not os.path.exists(filepath):
        await asyncio.sleep(interval)
        if asyncio.get_event_loop().time() - start_time > timeout:
            raise FileNotFoundError(
                f"File {filepath} not found within {timeout} seconds."
            )


@app.post("/inpaint")
def inpaint(
    image_file: UploadFile = File(...),
    pos_prompt: str = Form(
        "A 2D game sprite, natural, Pixel art, 64 bit, top-view, 2d game map, urban, dessert, town, open world, connected, smooth transition, natural"
    ),
    neg_prompt: str = Form(
        "3D, walls, unnatural, rough, unrealistic, closed area, towered, limited, side view, watermark, signature, artist, inappropriate content, objects, game ui, ui, buttons, walled, grid, character, white edges, single portrait, edged, island, bottom ui, bottom blocks, player, creatures, life, uneven roads, human, living, perspective, 3D, depth, shadows, vanishing point, isometric, gradient shading, foreshortening, parallax, skewed angles, distorted, photorealistic, realistic lighting, complex shading, dynamic lighting, occlusion",
    ),
    source_x: int = Form(...),
    source_y: int = Form(...),
    target_x: int = Form(...),
    target_y: int = Form(...),
    extend_direction: str = Form(""),
):
    llm_helper = app.package["llm_helper"]
    tile_prompts = app.package["tile_prompts"]

    prev_prompt = app.package["tile_prompts"].get(
        generate_tile_id(source_x, source_y), ""
    )

    print("Got inpaint request for tile ", target_x, target_y)

    if USE_LLM:
        pos_prompt = (
            "The scene that the player currently is in is a scene generated with this prompt: "
            + prev_prompt
            + " I want to create a scene that is connected to this scene. But don't be too creative. The scene should be connected to the current scene. "
            + ". What would be the prompt for the scene when the player moves "
            + extend_direction
            + "?"
        )

        pos_prompt = NECESSARY_PROMPTS + llm_helper.chat(pos_prompt)

    # Read the image file
    contents = image_file.file.read()
    if not image_file.content_type.startswith("image/"):
        return JSONResponse(content={"error": "Invalid file type"}, status_code=400)

    # Save to temp file
    temp_filename = "temp" + str(target_x) + "_" + str(target_y) + ".png"
    ComfyUI_image_dir = "ComfyUI/input/"
    temp_filepath = ComfyUI_image_dir + temp_filename
    with open(temp_filepath, "wb") as f:
        f.write(contents)

    # Ensure file is available (if needed)
    if not os.path.exists(temp_filepath):
        return JSONResponse(
            content={"error": "File not found after write"}, status_code=500
        )

    # if DEBUG:
    #     # we will return existing image for efficiency purposes
    #     with open("output.png", "rb") as f:
    #         time.sleep(2)
    #         return Response(content=f.read(), media_type="image/png")
    with gpu_lock:
        with torch.inference_mode():
            checkpoint = app.package["checkpoint"]
            loraloader = app.package["lora_loader"]
            # ksampler = app.package["ksampler"]
            ksampler = NODE_CLASS_MAPPINGS["KSampler"]()
            vaedecode = NODE_CLASS_MAPPINGS["VAEDecode"]()
            clip_encode = app.package["clip_encode"]
            load_image = app.package["load_image"]
            vae_encode_for_inpaint = app.package["vae_encode_for_inpaint"]

            loadimage_193 = load_image.load_image(image=temp_filename)

            vaeencodeforinpaint_213 = vae_encode_for_inpaint.encode(
                grow_mask_by=3,
                pixels=get_value_at_index(loadimage_193, 0),
                vae=get_value_at_index(checkpoint, 2),
                mask=get_value_at_index(loadimage_193, 1),
            )

            positive_encode = clip_encode.encode(
                text=pos_prompt,
                clip=get_value_at_index(loraloader, 1),
            )

            negative_encode = clip_encode.encode(
                text=neg_prompt,
                clip=get_value_at_index(loraloader, 1),
            )

            ksampler_8 = ksampler.sample(
                seed=random.randint(1, 2**64),
                steps=STEPS,
                cfg=3,
                sampler_name="ddim",
                scheduler="karras",
                denoise=1,
                model=get_value_at_index(loraloader, 0),
                positive=get_value_at_index(positive_encode, 0),
                negative=get_value_at_index(negative_encode, 0),
                latent_image=get_value_at_index(vaeencodeforinpaint_213, 0),
            )

            vaedecode_9 = vaedecode.decode(
                samples=get_value_at_index(ksampler_8, 0),
                vae=get_value_at_index(checkpoint, 2),
            )

            final_image = get_value_at_index(vaedecode_9, 0)[0]
            final_image = 255.0 * final_image.cpu().numpy()
            final_image = Image.fromarray(np.clip(final_image, 0, 255).astype(np.uint8))
            img_bytes = BytesIO()
            final_image.save(img_bytes, format="PNG")
            img_bytes.seek(0)

            tile_prompts[generate_tile_id(target_x, target_y)] = pos_prompt

            print("Inpainting done for ", temp_filename)
            # save file
            return StreamingResponse(img_bytes, media_type="image/png")


def start_server():
    # print('Starting Server...')

    uvicorn.run(
        "main:app",
        host="0.0.0.0",
        port=8765,
        log_level="debug",
        reload=True,
    )
    # webbrowser.open("http://127.0.0.1:8765")


if __name__ == "__main__":
    start_server()
