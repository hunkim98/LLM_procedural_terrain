import openai
from typing import List, Dict
from dotenv import load_dotenv
import os

# Load environment variables from .env file
load_dotenv()


class GPTAPIHelper:
    def __init__(
        self,
        api_key: str = None,
        model: str = "gpt-4",
        system_instruction: str = (
            "You are a helpful landscape architecture assistant that designs a game. "
            "Your goal is to give a prompt for generating a 2D pixel game stage design. "
            "The prompt will be used for generating art with a text-to-image model. "
            "Design the prompt so that the text-to-image model generates a top-down view, satellite view image for the pixel game. "
            "You will sometimes be given the direction of the user heading to a certain direction. "
            "Do not include any information about what the user is doing in the scene. "
            "Your job is to ONLY give a prompt sentence for generating an image in the text-to-image model. "
            "Like any good engineered prompt, the sentence prompt should be clear and concise comprised of descriptive words with commas. "
            "Only give me prompt sentence for generating the scene. Add no other instructions or title."
            "Do not include any information on whether the scene is extending. "
            "Just give me the scene that should be seen there. "
        ),
    ):
        """
        :param api_key: OpenAI API key for authentication.
        :param model: OpenAI model to use (e.g., "gpt-4", "gpt-3.5-turbo").
        :param system_instruction: Instruction text prepended to every interaction.
        """
        openai.api_key = api_key or os.getenv("OPENAI_KEY")
        self.model = model
        self.system_instruction = system_instruction
        self.chat_history: List[Dict[str, str]] = []

    def chat(
        self,
        user_message: str,
        max_tokens: int = 150,
        temperature: float = 0.8,
        top_p: float = 0.9,
    ) -> str:
        """
        Send a message to the GPT model and receive a response.
        """
        self.chat_history.append({"role": "user", "content": user_message})
        messages = [{"role": "system", "content": self.system_instruction}]
        messages.extend(self.chat_history)

        try:
            response = openai.ChatCompletion.create(
                model=self.model,
                messages=messages,
                max_tokens=max_tokens,
                temperature=temperature,
                top_p=top_p,
            )
            # for memory efficiency pop the recent user message
            # self.chat_history.pop()
            assistant_message = response["choices"][0]["message"]["content"].strip()
            self.chat_history.append(
                {"role": "assistant", "content": assistant_message}
            )
            print("\n--- Assistant response ---")
            print(assistant_message)
            return assistant_message

        except openai.error.OpenAIError as e:
            print(f"Error: {e}")
            raise e
            # return "An error occurred while processing your request."


# # Example usage
# if __name__ == "__main__":
#     # Either pass the API key here...
#     helper = GPTAPIHelper()

#     # ...or rely on the .env variable by not passing it:
#     # helper = GPTAPIHelper()

#     response = helper.chat("Help me create a prompt based on this: forest")
#     print("\n--- Final assistant response ---")
#     print(response)
