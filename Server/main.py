from contextlib import asynccontextmanager
import uvicorn
from typing import Union
import random
from PIL import Image
import numpy as np

from fastapi import FastAPI
import torch

from image_gen import get_value_at_index, import_custom_nodes, NODE_CLASS_MAPPINGS


import os
import random
import sys
from typing import Sequence, Mapping, Any, Union
import torch


@asynccontextmanager
async def lifespan(app: FastAPI):
    import_custom_nodes()
    with torch.inference_mode():
        checkpointloadersimple = NODE_CLASS_MAPPINGS["CheckpointLoaderSimple"]()
        checkpointloadersimple_1 = checkpointloadersimple.load_checkpoint(
            ckpt_name="pixelXL_xl.safetensors"
        )

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

        app.package = {
            "checkpoint": checkpointloadersimple_1,
            "clip_encode": cliptextencode,
            "empty_latent_image": emptylatentimage_2,
            "lora_loader": loraloader_6,
            "ksampler": ksampler_efficient,
        }
        print(app.package)

    # Startup logic
    print("Application startup")
    yield
    # Shutdown logic
    print("Application shutdown haha!")


app = FastAPI(lifespan=lifespan)


@app.get("/")
def read_root():
    return {"Hello": "World"}


@app.get("/gen")
def gen(pos_prompt: str = "", neg_prompt: str = ""):
    with torch.inference_mode():
        checkpoint = app.package["checkpoint"]
        loraloader = app.package["lora_loader"]
        ksampler = app.package["ksampler"]
        image = app.package["empty_latent_image"]
        clip_encode = app.package["clip_encode"]

        negative_encode = clip_encode.encode(
            text="3D, walls, unrealistic, closed area, towered, limited, side view, watermark, signature, artist, inappropriate content, objects, game ui, ui, buttons, walled, grid, character, white edges, single portrait, edged, island, bottom ui, bottom blocks, player, creatures, life, uneven roads, unrealistic, human, living",
            clip=get_value_at_index(loraloader, 1),
        )
        positive_encode = clip_encode.encode(
            text="A 2D game sprite, Pixel art, 64 bit, top-view, 2d game map, urban, dessert, town, open world",
            clip=get_value_at_index(loraloader, 1),
        )

        ksampler_efficient_5 = ksampler.sample(
            seed=random.randint(1, 2**64),
            steps=20,
            cfg=4,
            sampler_name="ddim",
            scheduler="karras",
            denoise=1,
            preview_method="auto",
            vae_decode="true",
            model=get_value_at_index(loraloader, 0),
            positive=get_value_at_index(positive_encode, 0),
            negative=get_value_at_index(negative_encode, 0),
            latent_image=get_value_at_index(image, 0),
            optional_vae=get_value_at_index(checkpoint, 2),
        )

        final_image = get_value_at_index(ksampler_efficient_5, 5)[0]
        final_image = 255.0 * final_image.cpu().numpy()
        final_image = Image.fromarray(np.clip(final_image, 0, 255).astype(np.uint8))
        print(final_image)

        final_image.save("output1.png")
    print("hi")


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
