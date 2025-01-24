import torch
from transformers import AutoTokenizer, AutoModelForCausalLM
from typing import List, Dict


class LLMHelper:
    def __init__(
        self,
        model_name: str = "mistralai/Mistral-7B-Instruct-v0.2",
        device: str = None,
        system_instruction: str = (
            "You are a helpful landscape architecture assistant that designs a game. "
            "Your goal is to give a prompt for generating a 2D pixel game stage design. "
            "The prompt will be used for generating art with a text to image model. "
            "Remember that you should generate a top-view, satellite view image for the pixel game. "
            "Only give me prompt sentence for generating the scene. Add no other instructions or title."
        ),
    ):
        """
        :param model_name: The Hugging Face model identifier to load.
        :param device: "cpu", "cuda", or "mps". If None, auto-detect is attempted.
        :param system_instruction: Instruction text prepended to every prompt.
        """

        # 1) Auto-detect device if not provided
        if device is None:
            if torch.cuda.is_available():
                device = "cuda"
            elif (
                getattr(torch.backends, "mps", None)
                and torch.backends.mps.is_available()
            ):
                device = "mps"
            else:
                device = "cpu"

        self.device = device
        print(f"Loading model '{model_name}' on {device}...")

        # 2) Load tokenizer & model
        #    (Remove device_map="auto" if you're manually calling .to(self.device))
        self.tokenizer = AutoTokenizer.from_pretrained(model_name)
        self.model = AutoModelForCausalLM.from_pretrained(model_name)
        self.model.to(self.device)
        self.model.eval()

        # 3) Keep a chat history if needed
        self.chat_history: List[Dict[str, str]] = []
        self.system_instruction = system_instruction

    def chat(
        self,
        user_message: str,
        max_new_tokens: int = 100,
        do_sample: bool = True,
        top_p: float = 0.9,
        temperature: float = 0.8,
    ) -> str:
        """
        Generate a response given a user message, using a "chat" style format
        that includes a system instruction and chat history.

        :param user_message: The user's query or input.
        :param max_new_tokens: Max tokens to generate (beyond the prompt).
        :param do_sample: Whether to sample (True) or do greedy decode (False).
        :param top_p: Nucleus sampling cutoff.
        :param temperature: Sampling temperature.
        :return: The assistant's extracted response (string).
        """
        # 1) Add the user message to chat history
        self.chat_history.append({"role": "user", "content": user_message})

        # 2) Build the combined prompt
        prompt = f"System: {self.system_instruction}\n"
        for turn in self.chat_history:
            if turn["role"] == "user":
                prompt += f"User: {turn['content']}\n"
            else:
                prompt += f"Assistant: {turn['content']}\n"
        # Add a final "Assistant:" to indicate the model should produce the next turn
        prompt += "Assistant:"

        self.chat_history.pop()  # Remove the last "Assistant:" turn (for memory)

        # 3) Tokenize
        # prompt = "Create me a rich and descriptive but concise text prompt for generating a landscape image with AI based on my description: forest."
        inputs = self.tokenizer(prompt, return_tensors="pt").to(self.device)

        # 4) Generate text
        with torch.no_grad():
            outputs = self.model.generate(
                **inputs,
                max_new_tokens=max_new_tokens,
                do_sample=do_sample,
                top_p=top_p,
                temperature=temperature,
            )

        # 5) Decode
        generated_text = self.tokenizer.decode(outputs[0], skip_special_tokens=True)

        # 6) Extract the newly generated assistant portion
        #    We split on the *last* occurrence of "Assistant:" in case it's repeated
        if "Assistant:" in generated_text:
            assistant_response = generated_text.split("Assistant:")[-1].strip()
        else:
            # If we can't find that marker, just return the entire generation
            assistant_response = generated_text

        # remove the double quotes in the response
        assistant_response = assistant_response.replace('"', "")

        # 7) Append the assistant's response to chat history
        self.chat_history.append({"role": "assistant", "content": assistant_response})

        # Debug prints (optional)
        print("===== FULL GENERATED TEXT =====")
        print(generated_text)
        print("===== ASSISTANT (extracted) ====")
        print(assistant_response)

        return assistant_response


# if __name__ == "__main__":
#     # Quick test
#     helper = LLMHelper()
#     # response = helper.chat("Help me create a prompt based on this: forest")
#     print("\n--- Final assistant response ---")
#     print(response)
