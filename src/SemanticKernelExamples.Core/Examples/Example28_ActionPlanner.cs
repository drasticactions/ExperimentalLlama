using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.Planning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SemanticKernelExamples.Examples
{
    public class Example28_ActionPlanner
    {
        public static async Task Run(bool internet = false)
        {
            Console.WriteLine("======== Action Planner ========");

            IKernel kernel;
            if (internet)
            {
                Console.WriteLine("Internet");
                kernel = Kernel.Builder
                .WithLoggerFactory(ConsoleLogger.LoggerFactory)
                .WithOpenAIChatCompletionService("gpt-3.5-turbo", Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new NotImplementedException("OPENAI_API_KEY"), alsoAsTextCompletion: true)
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
                    ContextSize = 8192,
                    GpuLayerCount = 50
                };
                var model = LLamaWeights.LoadFromFile(parameters);
                var context = model.CreateContext(parameters);
                kernel = Kernel.Builder
                    .WithLoggerFactory(ConsoleLogger.LoggerFactory)
                    .WithAIService<ITextCompletion>("local-llama-text", new LLamaSharpTextCompletion(new InstructExecutor(context)), true)
                    .Build();
            }

            string folder = RepoFiles.SampleSkillsPath();
            kernel.ImportSemanticSkillFromDirectory(folder, "SummarizeSkill");
            kernel.ImportSemanticSkillFromDirectory(folder, "WriterSkill");
            kernel.ImportSemanticSkillFromDirectory(folder, "FunSkill");
            await StartChatAsync(kernel);
        }

        private static async Task StartChatAsync(IKernel kernel)
        {
            // Create an instance of ActionPlanner.
            // The ActionPlanner takes one goal and returns a single function to execute.
            var planner = new ActionPlanner(kernel);

            // We're going to ask the planner to find a function to achieve this goal.
            var goal = "Write a joke about Cleopatra in the style of Hulk Hogan.";

            // The planner returns a plan, consisting of a single function
            // to execute and achieve the goal requested.
            var plan = await planner.CreatePlanAsync(goal);

            // Execute the full plan (which is a single function)
            SKContext result = await plan.InvokeAsync();

            // Show the result, which should match the given goal
            Console.WriteLine(result);

            /* Output:
             *
             * Cleopatra was a queen
             * But she didn't act like one
             * She was more like a teen

             * She was always on the scene
             * And she loved to be seen
             * But she didn't have a queenly bone in her body
             */
        }
    }
}
