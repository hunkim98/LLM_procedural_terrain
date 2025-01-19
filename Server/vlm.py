from transformers import LlavaNextProcessor, LlavaNextForConditionalGeneration
import torch

device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
processor = LlavaNextProcessor.from_pretrained("llava-hf/llava-v1.6-mistral-7b-hf")
model = LlavaNextForConditionalGeneration.from_pretrained(
    "llava-hf/llava-v1.6-mistral-7b-hf",
    torch_dtype=torch.float16,
    low_cpu_mem_usage=True,
)
model.to(device)
