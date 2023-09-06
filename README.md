# Experimental LLaMA

## Setup

This repo is set up to use LLamaSharp and its underlying `llama.cpp` bindings. The SemanticKernelExamples console app is wired to automatically use the runtime files inside the LLamaSharp source directory, so you should not need to include your own if you wish to try it out. If you want to include your own, turn off `IncludeBuiltInRuntimes` in the `csproj`.

This targeted version supports LLama2 GGUF files. You can find existing models available on Hugging Face.

Ex:

[TheBloke/Llama-2-7B-Chat-GGUF](https://huggingface.co/TheBloke/Llama-2-7B-GGUF)
[TheBloke/Llama-2-7B-GGUF](https://huggingface.co/TheBloke/Llama-2-7B-GGUF)

The 'chat' models will do better with `LLamaSharpChatCompletion`, while the latter should do better with `LLamaSharpTextCompletion`. Both should support TextEmbedding.

To test the OpenAI versions, be sure to include `OPENAI_API_KEY` (OpenAI API key) and `BING` (Bing Search API token) in your environment variables. 