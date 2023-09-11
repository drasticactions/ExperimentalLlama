using LLamaSharp.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.AI.ImageGeneration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SemanticKernelExamples.Examples;

internal class Example18_DallE
{
    public static async Task Run(bool internet = false)
    {
        IKernel kernel;

        if (internet)
        {
            kernel = new KernelBuilder()
            .WithLoggerFactory(ConsoleLogger.LoggerFactory)
            // Add your image generation service
            .WithOpenAIImageGenerationService(Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new NotImplementedException("OPENAI_API_KEY"))
            // Add your chat completion service 
            .WithOpenAIChatCompletionService("gpt-3.5-turbo", Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new NotImplementedException("OPENAI_API_KEY"))
            .Build();
        }
        else
        {
            Console.WriteLine("Local");
            Console.Write("Please input your model path: ");
            var modelPath = Console.ReadLine();
            // Load weights into memory
            var parameters = new ModelParams(modelPath)
            {
                Seed = 1337,
            };
            var model = LLamaWeights.LoadFromFile(parameters);
            var context = model.CreateContext(parameters);
            var ex = new InteractiveExecutor(context);
            var chat = new LLamaSharpChatCompletion(ex);

            Console.Write("Please input your stable diffusion model path: ");
            var sdPath = Console.ReadLine();
            var imageGeneration = new StableDiffusionCppImageGeneration(sdPath, 20);

            kernel = new KernelBuilder()
                .WithAIService<IChatCompletion>("local-llama", chat)
                .WithAIService<IImageGeneration>("local-sd", imageGeneration)
                .Build();
        }

        var size = 512;

        IImageGeneration dallE = kernel.GetService<IImageGeneration>();

        var imageDescription = "A cute baby sea otter";
        var image = await dallE.GenerateImageAsync(imageDescription, size, size);

        Console.WriteLine(imageDescription);
        Console.WriteLine("Image URL: " + image);

        /* Output:

        A cute baby sea otter
        Image URL: https://oaidalleapiprodscus.blob.core.windows.net/private/....

        */

        Console.WriteLine("======== Chat with images ========");

        IChatCompletion chatGPT = kernel.GetService<IChatCompletion>();
        var chatHistory = chatGPT.CreateNewChat(
            "You're chatting with a user. Instead of replying directly to the user" +
            " provide the description of an image that expresses what you want to say." +
            " The user won't see your message, they will see only the image. The system " +
            " generates an image using your description, so it's important you describe the image with details.");

        var msg = "Hi, I'm from Tokyo, where are you from?";
        chatHistory.AddUserMessage(msg);
        Console.WriteLine("User: " + msg);

        string reply = await chatGPT.GenerateMessageAsync(chatHistory);
        chatHistory.AddAssistantMessage(reply);
        image = await dallE.GenerateImageAsync(reply, size, size);
        Console.WriteLine("Bot: " + image);
        Console.WriteLine("Img description: " + reply);

        msg = "Oh, wow. Not sure where that is, could you provide more details?";
        chatHistory.AddUserMessage(msg);
        Console.WriteLine("User: " + msg);

        reply = await chatGPT.GenerateMessageAsync(chatHistory);
        chatHistory.AddAssistantMessage(reply);
        image = await dallE.GenerateImageAsync(reply, size, size);
        Console.WriteLine("Bot: " + image);
        Console.WriteLine("Img description: " + reply);

        /* Output:

        User: Hi, I'm from Tokyo, where are you from?
        Bot: https://oaidalleapiprodscus.blob.core.windows.net/private/...
        Img description: [An image of a globe with a pin dropped on a location in the middle of the ocean]

        User: Oh, wow. Not sure where that is, could you provide more details?
        Bot: https://oaidalleapiprodscus.blob.core.windows.net/private/...
        Img description: [An image of a map zooming in on the pin location, revealing a small island with a palm tree on it]

        */
    }
}
