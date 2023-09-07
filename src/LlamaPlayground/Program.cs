using LLama;
using LLama.Common;
using LLamaSharp.SemanticKernel.ChatCompletion;

Console.WriteLine("Hello, LLama!");

Console.Write("Please input your model path: ");
var modelPath = Console.ReadLine();

// Load weights into memory
var parameters = new ModelParams(modelPath)
{
    Seed = 1337,
    GpuLayerCount = 60
};

var model = LLamaWeights.LoadFromFile(parameters);
var context = model.CreateContext(parameters);
var ex = new InteractiveExecutor(context);

var chat = new LLamaSharpChatCompletion(ex);
var chatHistory = chat.CreateNewChat("You are a helpful AI assistant.");

Console.Write("User: ");

while (true)
{
    var input = Console.ReadLine();
    if (string.IsNullOrEmpty(input))
    {
        break;
    }

    chatHistory.AddUserMessage(input);
    var reply = chat.GetStreamingChatCompletionsAsync(chatHistory, new Microsoft.SemanticKernel.AI.ChatCompletion.ChatRequestSettings() { MaxTokens = 1024 } );
    await foreach (var message in reply)
    {
        Console.Write($"Assistant: ");
        await foreach (var item in message.GetStreamingChatMessageAsync())
        {
            Console.Write(item.Content);
        }
    }

    Console.WriteLine(Environment.NewLine);
    Console.Write("User: ");
}