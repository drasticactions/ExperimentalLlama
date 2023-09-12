# Experimental LLaMA

## Setup

This repo is set up to use LLamaSharp and its underlying `llama.cpp` bindings. The SemanticKernelExamples console app is wired to automatically use the runtime files inside the LLamaSharp source directory, so you should not need to include your own if you wish to try it out. If you want to include your own, turn off `IncludeBuiltInRuntimes` in the `csproj`.

This targeted version supports LLama2 GGUF files. You can find existing models available on Hugging Face.

Ex:

[TheBloke/Llama-2-7B-Chat-GGUF](https://huggingface.co/TheBloke/Llama-2-7B-GGUF)
[TheBloke/Llama-2-7B-GGUF](https://huggingface.co/TheBloke/Llama-2-7B-GGUF)

The 'chat' models will do better with `LLamaSharpChatCompletion`, while the latter should do better with `LLamaSharpTextCompletion`. Both should support TextEmbedding.

To test the OpenAI versions, be sure to include `OPENAI_API_KEY` (OpenAI API key) and `BING` (Bing Search API token) in your environment variables.

## Stable Diffusion

To test the Stable Diffusion wrapper:

- Download the newest artifacts for the stable-diffusion.cpp wrapper:
[stable-diffusion.cpp-wrapper](https://github.com/drasticactions/stable-diffusion.cpp-wrapper/actions/workflows/main.yml)
- Place the artifacts for the given OS you're running in the output directory of SemanticKernelExamples or LlamaPlayground

You can download ggml stable diffusion models on [huggingface](https://huggingface.co/nmkd/stable-diffusion-1.5-ggml/tree/main). Note that these models are intended for images 512x or higher. They may perform poorly on lower resolution images. You can also follow these [docs](https://github.com/leejet/stable-diffusion.cpp#convert-weights) to convert SD weights into ggml models.

## Findings

- For Text Embedding functions, smaller models with less parameters seem to function as well as larger ones. Having a smaller model used only for embedding with a larger one for other functions may be practical.
- With this in mind, the "orca-mini" models, with their smaller size and parameters, work well on iOS and Android devices. While terrible for chat, they can seem to handle other transformer tasks well enough for practical use in some cases. Granted, 1 to 2 GB filesizes are still too large for app deployment. More work needs to be done on smaller models.
- The default context size of 512MB is much too small for SK kernels with multiple functions, at least 1024+ is required or else llama.cpp will throw exceptions as its memory context is filled. There could be more within llama.cpp for saying where the state of the context memory usage is before getting to that point. Newer versions do show ggml asserts that it's trying to access out of bounds memory, but that's not user friendly.
- Shockingly, different models produce different results with the same settings applied. While above I stated that "LLamaSharpTextCompletion" should work well with non-chat models, that depends as the SK function itself may try to set up a chat style session within the context, and that _won't_ do well and will produce weird output. In the end, you need to understand the functions and plugs you use and what the underlying prompts are. OpenAI/Azure models seem more flexable here, or at least they're doing something on the server side to handle it.