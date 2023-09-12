using LLamaSharp.SemanticKernel.ChatCompletion;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using SemanticKernelExamples;
using SemanticKernelExamples.Examples;

internal class Program
{
    private static async Task Main(string[] args)
    {
        Console.WriteLine("Please input a number to choose an example to run:");
        Console.WriteLine("0: Internet");
        Console.WriteLine("1: Local");

        while(true)
        {
            Console.Write("\nYour choice: ");
            int choice = int.Parse(Console.ReadLine());

            if (choice == 0)
            {
                await Runner(true);
            }
            else if (choice == 1)
            {
                await Runner(false);
            }
            else
            {
                Console.WriteLine("Cannot parse your choice. Please select again.");
                continue;
            }
            break;
        }
    }

    private static async Task Runner(bool internet)
    {
        Console.WriteLine("Please input a number to choose an example to run:");
        Console.WriteLine("0: Example04_CombineLLMPromptsAndNativeCode");
        Console.WriteLine("1: Example13_ConversationSummarySkill");
        Console.WriteLine("2: Example14_SemanticMemory");
        Console.WriteLine("3: Example17_ChatGPT");
        Console.WriteLine("4: Example18_DallE");
        Console.WriteLine("5: Example28_ActionPlanner");
        Console.WriteLine("6: Example32_StreamingCompletion");
        Console.WriteLine("7: Example48_GroundednessChecks");
        Console.WriteLine("8: Example49_LogitBias");
        Console.WriteLine("9: Example51_StepwisePlanner");
        Console.WriteLine("10: StableDiffusion_Example");
        Console.WriteLine("11: Example07_BingAndGoogleSkills");
        Console.WriteLine("12: Example15_TextMemorySkill");
        Console.WriteLine("13: ConsoleGPTService");
        while (true)
        {
            Console.Write("\nYour choice: ");
            int choice = int.Parse(Console.ReadLine());

            if (choice == 0)
            {
                await Example04_CombineLLMPromptsAndNativeCode.Run(internet);
            }
            else if (choice == 1)
            {
                await Example13_ConversationSummarySkill.Run(internet);
            }
            else if (choice == 2)
            {
                await Example14_SemanticMemory.Run(internet);
            }
            else if (choice == 3)
            {
                await Example17_ChatGPT.Run(internet);
            }
            else if (choice == 4)
            {
                await Example18_DallE.Run(internet);
            }
            else if (choice == 5)
            {
                await Example28_ActionPlanner.Run(internet);
            }
            else if (choice == 6)
            {
                await Example32_StreamingCompletion.Run(internet);
            }
            else if (choice == 7)
            {
                await Example48_GroundednessChecks.Run(internet);
            }
            else if (choice == 8)
            {
                await Example49_LogitBias.Run(internet);
            }
            else if (choice == 9)
            {
                await Example51_StepwisePlanner.Run(internet);
            }
            else if (choice == 10)
            {
                await StableDiffusion_Example.Run(internet);
            }
            else if (choice == 11)
            {
                await Example07_BingAndGoogleSkills.Run(internet);
            }
            else if (choice == 12)
            {
                await Example15_TextMemorySkill.Run(internet);
            }
            else if (choice == 13)
            {
                await RunConsoleGPT(internet);
            }
            else
            {
                Console.WriteLine("Cannot parse your choice. Please select again.");
                continue;
            }
            break;
        }
    }

    private static async Task RunConsoleGPT(bool internet)
    {
        var modelPath = "";
        if (!internet)
        {
            Console.WriteLine("Local");
            Console.Write("Please input your model path: ");
            modelPath = Console.ReadLine();
        }
        // Create the host builder with logging configured from the kernel settings.
        var builder = Host.CreateDefaultBuilder()
            .ConfigureLogging(logging =>
            {
            });

        // Configure the services for the host
        builder.ConfigureServices((context, services) =>
        {
            IKernel kernel;

            if (internet)
            {
                var builder = new KernelBuilder();
                builder.WithOpenAIChatCompletionService("gpt-3.5-turbo", Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new NotImplementedException("OPENAI_API_KEY"));
                kernel = builder.Build();
            }
            else
            {
                // Load weights into memory
                var parameters = new ModelParams(modelPath)
                {
                    Seed = 1337,
                    ContextSize = 1024,
                    GpuLayerCount = 50
                };
                var model = LLamaWeights.LoadFromFile(parameters);
                var modelContext = model.CreateContext(parameters);
                var ex = new InteractiveExecutor(modelContext);

                var builder = new KernelBuilder();
                builder.WithAIService<IChatCompletion>("local-llama-chat", new LLamaSharpChatCompletion(ex), true);
                kernel = builder.Build();
            }

            // Add Semantic Kernel to the host builder
            services.AddSingleton<IKernel>(kernel);

            // Add Native Skills to the host builder
            services.AddSingleton<ConsoleSkill>();
            services.AddSingleton<ChatSkill>();

            // Add the primary hosted service to the host builder to start the loop.
            services.AddHostedService<ConsoleGPTService>();
        });

        // Build and run the host. This keeps the app running using the HostedService.
        var host = builder.Build();
        await host.RunAsync();
    }
}