using LLamaSharp.SemanticKernel.TextEmbedding;
using Microsoft.SemanticKernel.AI.Embeddings;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.TextEmbedding;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Skills.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SemanticKernelExamples.Examples
{
    internal class Example15_TextMemorySkill
    {
        private const string MemoryCollectionName = "aboutMe";

        public static async Task Run(bool internet = false)
        {
            IMemoryStore memoryStore;
            CancellationToken cancellationToken = CancellationToken.None;
            // Volatile Memory Store - an in-memory store that is not persisted
            memoryStore = new VolatileMemoryStore();
            ITextEmbeddingGeneration embeddingGenerator;
            var loggerFactory = ConsoleLogger.LoggerFactory;
            IKernel? kernel = null;
            if (internet)
            {
                Console.WriteLine("Internet");

                embeddingGenerator = new OpenAITextEmbeddingGeneration("text-embedding-ada-002", Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new NotImplementedException("OPENAI_API_KEY"));

                kernel = Kernel.Builder
                .WithLoggerFactory(ConsoleLogger.LoggerFactory)
                .WithOpenAIChatCompletionService("gpt-3.5-turbo", Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new NotImplementedException("OPENAI_API_KEY"))
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
                var embedModelParams = new ModelParams(modelPath)
                {
                    Seed = seed,
                    EmbeddingMode = true,
                    ContextSize = 8192,
                };

                // Load weights into memory
                var modelParams = new ModelParams(modelPath)
                {
                    Seed = seed,
                    ContextSize = 8192,
                };

                var embedModel = LLamaWeights.LoadFromFile(embedModelParams);
                var model = LLamaWeights.LoadFromFile(modelParams);
                var embedding = new LLamaEmbedder(embedModel, embedModelParams);

                embeddingGenerator = new LLamaSharpEmbeddingGeneration(embedding);
                kernel = Kernel.Builder
                    .WithLoggerFactory(ConsoleLogger.LoggerFactory)
                    .WithAIService<ITextCompletion>("local-llama", new LLamaSharpTextCompletion(new InstructExecutor(model.CreateContext(modelParams))), true)
                    .WithAIService<ITextEmbeddingGeneration>("local-llama-embed", embeddingGenerator, true)
                    .WithMemoryStorage(new VolatileMemoryStore())
                    .Build();
            }

            // The combination of the text embedding generator and the memory store makes up the 'SemanticTextMemory' object used to
            // store and retrieve memories.
            using SemanticTextMemory textMemory = new(memoryStore, embeddingGenerator);

            /////////////////////////////////////////////////////////////////////////////////////////////////////
            // PART 1: Store and retrieve memories using the ISemanticTextMemory (textMemory) object.
            //
            // This is a simple way to store memories from a code perspective, without using the Kernel.
            /////////////////////////////////////////////////////////////////////////////////////////////////////
            Console.WriteLine("== PART 1a: Saving Memories through the ISemanticTextMemory object ==");

            Console.WriteLine("Saving memory with key 'info1': \"My name is Andrea\"");
            await textMemory.SaveInformationAsync(MemoryCollectionName, id: "info1", text: "My name is Andrea", cancellationToken: cancellationToken);

            Console.WriteLine("Saving memory with key 'info2': \"I work as a tourist operator\"");
            await textMemory.SaveInformationAsync(MemoryCollectionName, id: "info2", text: "I work as a tourist operator", cancellationToken: cancellationToken);

            Console.WriteLine("Saving memory with key 'info3': \"I've been living in Seattle since 2005\"");
            await textMemory.SaveInformationAsync(MemoryCollectionName, id: "info3", text: "I've been living in Seattle since 2005", cancellationToken: cancellationToken);

            Console.WriteLine("Saving memory with key 'info4': \"I visited France and Italy five times since 2015\"");
            await textMemory.SaveInformationAsync(MemoryCollectionName, id: "info4", text: "I visited France and Italy five times since 2015", cancellationToken: cancellationToken);

            // Retrieve a memory
            Console.WriteLine("== PART 1b: Retrieving Memories through the ISemanticTextMemory object ==");
            MemoryQueryResult? lookup = await textMemory.GetAsync(MemoryCollectionName, "info1", cancellationToken: cancellationToken);
            Console.WriteLine("Memory with key 'info1':" + lookup?.Metadata.Text ?? "ERROR: memory not found");
            Console.WriteLine();

            /////////////////////////////////////////////////////////////////////////////////////////////////////
            // PART 2: Create TextMemorySkill, store and retrieve memories through the Kernel.
            //
            // This enables semantic functions and the AI (via Planners) to access memories
            /////////////////////////////////////////////////////////////////////////////////////////////////////

            Console.WriteLine("== PART 2a: Saving Memories through the Kernel with TextMemorySkill and the 'Save' function ==");

            // Import the TextMemorySkill into the Kernel for other functions
            var memorySkill = new TextMemorySkill(textMemory);
            var memoryFunctions = kernel.ImportSkill(memorySkill);

            // Save a memory with the Kernel
            Console.WriteLine("Saving memory with key 'info5': \"My family is from New York\"");
            await kernel.RunAsync(memoryFunctions["Save"], new()
            {
                [TextMemorySkill.CollectionParam] = MemoryCollectionName,
                [TextMemorySkill.KeyParam] = "info5",
                ["input"] = "My family is from New York"
            }, cancellationToken);

            // Retrieve a specific memory with the Kernel
            Console.WriteLine("== PART 2b: Retrieving Memories through the Kernel with TextMemorySkill and the 'Retrieve' function ==");
            var result = await kernel.RunAsync(memoryFunctions["Retrieve"], new()
            {
                [TextMemorySkill.CollectionParam] = MemoryCollectionName,
                [TextMemorySkill.KeyParam] = "info5"
            }, cancellationToken);

            Console.WriteLine("Memory with key 'info5':" + result?.ToString() ?? "ERROR: memory not found");
            Console.WriteLine();

            /////////////////////////////////////////////////////////////////////////////////////////////////////
            // PART 3: Recall similar ideas with semantic search
            //
            // Uses AI Embeddings for fuzzy lookup of memories based on intent, rather than a specific key.
            /////////////////////////////////////////////////////////////////////////////////////////////////////

            Console.WriteLine("== PART 3: Recall (similarity search) with AI Embeddings ==");

            Console.WriteLine("== PART 3a: Recall (similarity search) with ISemanticTextMemory ==");
            Console.WriteLine("Ask: where did I grow up?");

            await foreach (var answer in textMemory.SearchAsync(
                collection: MemoryCollectionName,
                query: "where did I grow up?",
                limit: 2,
            minRelevanceScore: 0.79,
                withEmbeddings: true,
                cancellationToken: cancellationToken))
            {
                Console.WriteLine($"Answer: {answer.Metadata.Text}");
            }

            Console.WriteLine("== PART 3b: Recall (similarity search) with Kernel and TextMemorySkill 'Recall' function ==");
            Console.WriteLine("Ask: where do I live?");

            result = await kernel.RunAsync(memoryFunctions["Recall"], new()
            {
                [TextMemorySkill.CollectionParam] = MemoryCollectionName,
                [TextMemorySkill.LimitParam] = "2",
                [TextMemorySkill.RelevanceParam] = "0.79",
                ["input"] = "Ask: where do I live?"
            }, cancellationToken);

            Console.WriteLine($"Answer: {result}");
            Console.WriteLine();

            /*
            Output:

                Ask: where did I grow up?
                Answer:
                    ["My family is from New York","I\u0027ve been living in Seattle since 2005"]

                Ask: where do I live?
                Answer:
                    ["I\u0027ve been living in Seattle since 2005","My family is from New York"]
            */

            /////////////////////////////////////////////////////////////////////////////////////////////////////
            // PART 3: TextMemorySkill Recall in a Semantic Function
            //
            // Looks up related memories when rendering a prompt template, then sends the rendered prompt to
            // the text completion model to answer a natural language query.
            /////////////////////////////////////////////////////////////////////////////////////////////////////

            Console.WriteLine("== PART 4: Using TextMemorySkill 'Recall' function in a Semantic Function ==");

            // Build a semantic function that uses memory to find facts
            const string RecallFunctionDefinition = @"
Consider only the facts below when answering questions:

BEGIN FACTS
About me: {{recall 'where did I grow up?'}}
About me: {{recall 'where do I live now?'}}
END FACTS

Question: {{$input}}

Answer:
";

            var aboutMeOracle = kernel.CreateSemanticFunction(RecallFunctionDefinition, maxTokens: 100);

            result = await kernel.RunAsync(aboutMeOracle, new()
            {
                [TextMemorySkill.CollectionParam] = MemoryCollectionName,
                [TextMemorySkill.RelevanceParam] = "0.79",
                ["input"] = "Do I live in the same town where I grew up?"
            }, cancellationToken);

            Console.WriteLine("Ask: Do I live in the same town where I grew up?");
            Console.WriteLine($"Answer: {result}");

            /*
            Approximate Output:
                Answer: No, I do not live in the same town where I grew up since my family is from New York and I have been living in Seattle since 2005.
            */

            /////////////////////////////////////////////////////////////////////////////////////////////////////
            // PART 5: Cleanup, deleting database collection
            //
            /////////////////////////////////////////////////////////////////////////////////////////////////////

            Console.WriteLine("== PART 5: Cleanup, deleting database collection ==");

            Console.WriteLine("Printing Collections in DB...");
            var collections = memoryStore.GetCollectionsAsync(cancellationToken);
            await foreach (var collection in collections)
            {
                Console.WriteLine(collection);
            }
            Console.WriteLine();

            Console.WriteLine("Removing Collection {0}", MemoryCollectionName);
            await memoryStore.DeleteCollectionAsync(MemoryCollectionName, cancellationToken);
            Console.WriteLine();

            Console.WriteLine($"Printing Collections in DB (after removing {MemoryCollectionName})...");
            collections = memoryStore.GetCollectionsAsync(cancellationToken);
            await foreach (var collection in collections)
            {
                Console.WriteLine(collection);
            }
        }
    }
}
