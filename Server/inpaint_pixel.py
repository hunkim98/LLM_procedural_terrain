import os
import random
import sys
from typing import Sequence, Mapping, Any, Union
import torch


def get_value_at_index(obj: Union[Sequence, Mapping], index: int) -> Any:
    """Returns the value at the given index of a sequence or mapping.

    If the object is a sequence (like list or string), returns the value at the given index.
    If the object is a mapping (like a dictionary), returns the value at the index-th key.

    Some return a dictionary, in these cases, we look for the "results" key

    Args:
        obj (Union[Sequence, Mapping]): The object to retrieve the value from.
        index (int): The index of the value to retrieve.

    Returns:
        Any: The value at the given index.

    Raises:
        IndexError: If the index is out of bounds for the object and the object is not a mapping.
    """
    try:
        return obj[index]
    except KeyError:
        return obj["result"][index]


def find_path(name: str, path: str = None) -> str:
    """
    Recursively looks at parent folders starting from the given path until it finds the given name.
    Returns the path as a Path object if found, or None otherwise.
    """
    # If no path is given, use the current working directory
    if path is None:
        path = os.getcwd()

    # Check if the current directory contains the name
    if name in os.listdir(path):
        path_name = os.path.join(path, name)
        print(f"{name} found: {path_name}")
        return path_name

    # Get the parent directory
    parent_directory = os.path.dirname(path)

    # If the parent directory is the same as the current directory, we've reached the root and stop the search
    if parent_directory == path:
        return None

    # Recursively call the function with the parent directory
    return find_path(name, parent_directory)


def add_comfyui_directory_to_sys_path() -> None:
    """
    Add 'ComfyUI' to the sys.path
    """
    comfyui_path = find_path("ComfyUI")
    if comfyui_path is not None and os.path.isdir(comfyui_path):
        sys.path.append(comfyui_path)
        print(f"'{comfyui_path}' added to sys.path")


def add_extra_model_paths() -> None:
    """
    Parse the optional extra_model_paths.yaml file and add the parsed paths to the sys.path.
    """
    try:
        from main import load_extra_path_config
    except ImportError:
        print(
            "Could not import load_extra_path_config from main.py. Looking in utils.extra_config instead."
        )
        from ComfyUI.utils.extra_config import load_extra_path_config

    extra_model_paths = find_path("extra_model_paths.yaml")

    if extra_model_paths is not None:
        load_extra_path_config(extra_model_paths)
    else:
        print("Could not find the extra_model_paths config file.")


add_comfyui_directory_to_sys_path()
add_extra_model_paths()


def import_custom_nodes() -> None:
    """Find all custom nodes in the custom_nodes folder and add those node objects to NODE_CLASS_MAPPINGS

    This function sets up a new asyncio event loop, initializes the PromptServer,
    creates a PromptQueue, and initializes the custom nodes.
    """
    import asyncio
    import ComfyUI.execution
    from ComfyUI.nodes import init_extra_nodes
    import ComfyUI.server

    # Creating a new event loop and setting it as the default loop
    loop = asyncio.new_event_loop()
    asyncio.set_event_loop(loop)

    # Creating an instance of PromptServer with the loop
    server_instance = ComfyUI.server.PromptServer(loop)
    ComfyUI.execution.PromptQueue(server_instance)

    # Initializing custom nodes
    init_extra_nodes()


from ComfyUI.nodes import NODE_CLASS_MAPPINGS


def main():
    import_custom_nodes()
    with torch.inference_mode():
        checkpointloadersimple = NODE_CLASS_MAPPINGS["CheckpointLoaderSimple"]()
        checkpointloadersimple_4 = checkpointloadersimple.load_checkpoint(
            ckpt_name="pixelXL_xl.safetensors"
        )

        loraloader = NODE_CLASS_MAPPINGS["LoraLoader"]()
        loraloader_206 = loraloader.load_lora(
            lora_name="pixel-art-xl-v1.1.safetensors",
            strength_model=1,
            strength_clip=1,
            model=get_value_at_index(checkpointloadersimple_4, 0),
            clip=get_value_at_index(checkpointloadersimple_4, 1),
        )

        cliptextencode = NODE_CLASS_MAPPINGS["CLIPTextEncode"]()
        cliptextencode_165 = cliptextencode.encode(
            text="A 2D game sprite, Pixel art, 64 bit, top-view, 2d game map, urban, dessert, town, open world",
            clip=get_value_at_index(loraloader_206, 1),
        )

        cliptextencode_166 = cliptextencode.encode(
            text="3D, walls, unrealistic, closed area, towered, limited, side view, watermark, signature, artist, inappropriate content, objects, game ui, ui, buttons, walled, grid, character, white edges, single portrait, edged, island, bottom ui, bottom blocks, player, creatures, life, uneven roads, unrealistic, human, living",
            clip=get_value_at_index(loraloader_206, 1),
        )

        loadimage = NODE_CLASS_MAPPINGS["LoadImage"]()
        loadimage_193 = loadimage.load_image(image="map_mask_half.png")

        vaeencodeforinpaint = NODE_CLASS_MAPPINGS["VAEEncodeForInpaint"]()
        vaeencodeforinpaint_213 = vaeencodeforinpaint.encode(
            grow_mask_by=3,
            pixels=get_value_at_index(loadimage_193, 0),
            vae=get_value_at_index(checkpointloadersimple_4, 2),
            mask=get_value_at_index(loadimage_193, 1),
        )

        ksampler = NODE_CLASS_MAPPINGS["KSampler"]()
        vaedecode = NODE_CLASS_MAPPINGS["VAEDecode"]()
        saveimage = NODE_CLASS_MAPPINGS["SaveImage"]()

        for q in range(1):
            ksampler_210 = ksampler.sample(
                seed=random.randint(1, 2**64),
                steps=20,
                cfg=3,
                sampler_name="ddim",
                scheduler="karras",
                denoise=1,
                model=get_value_at_index(loraloader_206, 0),
                positive=get_value_at_index(cliptextencode_165, 0),
                negative=get_value_at_index(cliptextencode_166, 0),
                latent_image=get_value_at_index(vaeencodeforinpaint_213, 0),
            )

            vaedecode_211 = vaedecode.decode(
                samples=get_value_at_index(ksampler_210, 0),
                vae=get_value_at_index(checkpointloadersimple_4, 2),
            )

            saveimage_212 = saveimage.save_images(
                filename_prefix="ComfyUI", images=get_value_at_index(vaedecode_211, 0)
            )


if __name__ == "__main__":
    main()
