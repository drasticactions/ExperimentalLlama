﻿using LLamaSharp.SemanticKernel.TextEmbedding;
using Microsoft.SemanticKernel.AI.Embeddings;
using Microsoft.SemanticKernel.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SemanticKernelExamples.Examples
{
    public class Example14_SemanticMemory
    {
        private const string MemoryCollectionName = "SKGitHub";

        public static async Task Run(bool internet = false)
        {
            var loggerFactory = ConsoleLogger.LoggerFactory;
            IKernel? kernel = null;
            Console.WriteLine("====================================================");
            Console.WriteLine("======== Semantic Memory (volatile, in RAM) ========");
            Console.WriteLine("====================================================");
            if (internet)
            {
                Console.WriteLine("Internet");

                kernel = Kernel.Builder
            .WithLoggerFactory(ConsoleLogger.LoggerFactory)
            .WithOpenAITextEmbeddingGenerationService("text-embedding-ada-002", Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new NotImplementedException("OPENAI_API_KEY"))
           .WithMemoryStorage(new VolatileMemoryStore())
            .Build();
            }
            else
            {
                Console.WriteLine("Local");
                Console.Write("Please input your model path: ");
                var modelPath = Console.ReadLine();

                var seed = 1337;
                // Load weights into memory
                var parameters = new ModelParams(modelPath)
                {
                    Seed = seed,
                    EmbeddingMode = true
                };

                using var model = LLamaWeights.LoadFromFile(parameters);
                var embedding = new LLamaEmbedder(model, parameters);

                kernel = Kernel.Builder
                    .WithLoggerFactory(ConsoleLogger.LoggerFactory)
                    .WithAIService<ITextEmbeddingGeneration>("local-llama-embed", new LLamaSharpEmbeddingGeneration(embedding), true)
                    .WithMemoryStorage(new VolatileMemoryStore())
                    .Build();
            }

            await RunExampleAsync(kernel);
        }

        private static async Task RunExampleAsync(IKernel kernel)
        {
            await StoreMemoryAsync(kernel);

            await SearchMemoryAsync(kernel, "How do I get started?");

            /*
            Output:

            Query: How do I get started?

            Result 1:
              URL:     : https://github.com/microsoft/semantic-kernel/blob/main/README.md
              Title    : README: Installation, getting started, and how to contribute

            Result 2:
              URL:     : https://github.com/microsoft/semantic-kernel/blob/main/samples/dotnet-jupyter-notebooks/00-getting-started.ipynb
              Title    : Jupyter notebook describing how to get started with the Semantic Kernel

            */

            await SearchMemoryAsync(kernel, "Can I build a chat with SK?");

            /*
            Output:

            Query: Can I build a chat with SK?

            Result 1:
              URL:     : https://github.com/microsoft/semantic-kernel/tree/main/samples/skills/ChatSkill/ChatGPT
              Title    : Sample demonstrating how to create a chat skill interfacing with ChatGPT

            Result 2:
              URL:     : https://github.com/microsoft/semantic-kernel/blob/main/samples/apps/chat-summary-webapp-react/README.md
              Title    : README: README associated with a sample chat summary react-based webapp

            */

            await SearchMemoryAsync(kernel, "Jupyter notebook");

            await SearchMemoryAsync(kernel, "README: README associated with a sample chat summary react-based webapp");

            await SearchMemoryAsync(kernel, "Jupyter notebook describing how to pass prompts from a file to a semantic skill or function");
        }

        private static async Task SearchMemoryAsync(IKernel kernel, string query)
        {
            Console.WriteLine("\nQuery: " + query + "\n");

            var memories = kernel.Memory.SearchAsync(MemoryCollectionName, query, limit: 10, minRelevanceScore: 0.5);

            int i = 0;
            await foreach (MemoryQueryResult memory in memories)
            {
                Console.WriteLine($"Result {++i}:");
                Console.WriteLine("  URL:     : " + memory.Metadata.Id);
                Console.WriteLine("  Title    : " + memory.Metadata.Description);
                Console.WriteLine("  Relevance: " + memory.Relevance);
                Console.WriteLine();
            }

            Console.WriteLine("----------------------");
        }

        private static async Task StoreMemoryAsync(IKernel kernel)
        {
            /* Store some data in the semantic memory.
             *
             * When using Azure Cognitive Search the data is automatically indexed on write.
             *
             * When using the combination of VolatileStore and Embedding generation, SK takes
             * care of creating and storing the index
             */

            Console.WriteLine("\nAdding some GitHub file URLs and their descriptions to the semantic memory.");
            var githubFiles = SampleData();
            var i = 0;
            foreach (var entry in githubFiles)
            {
                var result = await kernel.Memory.SaveReferenceAsync(
                    collection: MemoryCollectionName,
                    externalSourceName: "GitHub",
                    externalId: entry.Key,
                    description: entry.Value,
                    text: entry.Value);

                Console.WriteLine($"#{++i} saved.");
                Console.WriteLine(result);
            }

            Console.WriteLine("\n----------------------");
        }

        private static Dictionary<string, string> SampleData()
        {
            return new Dictionary<string, string>
            {
                ["https://github.com/microsoft/semantic-kernel/blob/main/README.md"]
                    = "README: Installation, getting started, and how to contribute",
                ["https://github.com/microsoft/semantic-kernel/blob/main/dotnet/notebooks/02-running-prompts-from-file.ipynb"]
                    = "Jupyter notebook describing how to pass prompts from a file to a semantic skill or function",
                ["https://github.com/microsoft/semantic-kernel/blob/main/dotnet/notebooks//00-getting-started.ipynb"]
                    = "Jupyter notebook describing how to get started with the Semantic Kernel",
                ["https://github.com/microsoft/semantic-kernel/tree/main/samples/skills/ChatSkill/ChatGPT"]
                    = "Sample demonstrating how to create a chat skill interfacing with ChatGPT",
                ["https://github.com/microsoft/semantic-kernel/blob/main/dotnet/src/SemanticKernel/Memory/VolatileMemoryStore.cs"]
                    = "C# class that defines a volatile embedding store",
                ["https://github.com/microsoft/semantic-kernel/blob/main/samples/dotnet/KernelHttpServer/README.md"]
                    = "README: How to set up a Semantic Kernel Service API using Azure Function Runtime v4",
                ["https://github.com/microsoft/semantic-kernel/blob/main/samples/apps/chat-summary-webapp-react/README.md"]
                    = "README: README associated with a sample chat summary react-based webapp",
            };
        }
    }
}
