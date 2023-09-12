using LLamaSharp.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SemanticKernelExamples.Examples
{
    internal class GitHub_Example
    {
        public static async Task Run(bool internet = false)
        {
            IKernel kernel;
            var loggerFactory = ConsoleLogger.LoggerFactory;
            if (internet)
            {
                var builder = new KernelBuilder();
                builder.WithOpenAIChatCompletionService("gpt-3.5-turbo", Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new NotImplementedException("OPENAI_API_KEY"));
                builder.WithLoggerFactory(loggerFactory);
                kernel = builder.Build();
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
                    ContextSize = 1024,
                    GpuLayerCount = 50
                };
                var model = LLamaWeights.LoadFromFile(parameters);
                var modelContext = model.CreateContext(parameters);
                var ex = new InteractiveExecutor(modelContext);

                var builder = new KernelBuilder();
                builder.WithAIService<IChatCompletion>("local-llama-chat", new LLamaSharpChatCompletion(ex), true);
                builder.WithLoggerFactory(loggerFactory);
                kernel = builder.Build();
            }

            GitHubPlugin githubPlugin = new(kernel);
            _ = kernel.ImportSkill(githubPlugin, nameof(GitHubPlugin));

            var repoUrl = "https://github.com/sciSharp/LLamaSharp/";
            var repoBranch = "master";

            var result = await githubPlugin.SummarizeRepositoryAsync(repoUrl, repoBranch, "*.md");
            Console.WriteLine(result);
        }
    }
}
