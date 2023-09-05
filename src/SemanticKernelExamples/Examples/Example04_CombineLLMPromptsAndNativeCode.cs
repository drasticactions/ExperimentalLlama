using LLama;
using LLama.Common;
using LLamaSharp.SemanticKernel.TextCompletion;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.TextCompletion;
using Microsoft.SemanticKernel.Skills.Web;
using Microsoft.SemanticKernel.Skills.Web.Bing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SemanticKernelExamples.Examples
{
    internal class Example04_CombineLLMPromptsAndNativeCode
    {
        public static async Task Run(bool internet = false)
        {
            IKernel? kernel = null;
            Console.WriteLine("======== LLMPrompts ========");

            if (internet)
            {
                Console.WriteLine("Internet");
                var builder = new KernelBuilder();
                builder.WithOpenAIChatCompletionService("gpt-3.5-turbo", Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new NotImplementedException("OPENAI_API_KEY"));
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
                };
                var model = LLamaWeights.LoadFromFile(parameters);
                var context = model.CreateContext(parameters);
                var ex = new InteractiveExecutor(context);

                var builder = new KernelBuilder();
                builder.WithAIService<ITextCompletion>("local-llama", new LLamaSharpTextCompletion(ex), true);

                kernel = builder.Build();
            }

            var bingConnector = new BingConnector(Environment.GetEnvironmentVariable("BING") ?? throw new NotImplementedException("BING Missing"));
            var bing = new WebSearchEngineSkill(bingConnector);
            var search = kernel.ImportSkill(bing, "bing");

            // Load semantic skill defined with prompt templates
            string folder = RepoFiles.SampleSkillsPath();

            var sumSkill = kernel.ImportSemanticSkillFromDirectory(folder, "SummarizeSkill");

            // Run
            var ask = "What's the tallest building in South America";

            var result1 = await kernel.RunAsync(
                ask,
                search["Search"]
            );

            var result2 = await kernel.RunAsync(
                ask,
                search["Search"],
                sumSkill["Summarize"]
            );

            var result3 = await kernel.RunAsync(
                ask,
                search["Search"],
                sumSkill["Notegen"]
            );

            Console.WriteLine(ask + "\n");
            Console.WriteLine("Bing Answer: " + result1 + "\n");
            Console.WriteLine("Summary: " + result2 + "\n");
            Console.WriteLine("Notes: " + result3 + "\n");
        }
    }
}
