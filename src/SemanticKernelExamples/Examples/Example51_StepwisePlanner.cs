using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.Skills.Core;
using Microsoft.SemanticKernel.Skills.Web.Bing;
using Microsoft.SemanticKernel.Skills.Web;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SemanticKernel.Planning;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using LLamaSharp.SemanticKernel.ChatCompletion;

namespace SemanticKernelExamples.Examples
{
    internal class Example51_StepwisePlanner
    {
        // Used to override the max allowed tokens when running the plan
        internal static int? ChatMaxTokens = null;
        internal static int? TextMaxTokens = null;

        // Used to quickly modify the chat model used by the planner
        internal static string? ChatModelOverride = null; //"gpt-35-turbo";
        internal static string? TextModelOverride = null; //"text-davinci-003";

        internal static string? Suffix = null;

        public static async Task Run(bool internet = false)
        {
            ChatMaxTokens = (new Microsoft.SemanticKernel.Planning.Stepwise.StepwisePlannerConfig()).MaxTokens;
            TextMaxTokens = (new Microsoft.SemanticKernel.Planning.Stepwise.StepwisePlannerConfig()).MaxTokens;
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
                    .WithAIService<IChatCompletion>("local-llama", new LLamaSharpChatCompletion(new InteractiveExecutor(context)), true)
                    .WithAIService<ITextCompletion>("local-llama-text", new LLamaSharpTextCompletion(new InstructExecutor(context)), true)
                    .Build();
            }

            var bingConnector = new BingConnector(Environment.GetEnvironmentVariable("BING") ?? throw new NotImplementedException("BING Missing"));
            var webSearchEngineSkill = new WebSearchEngineSkill(bingConnector);

            kernel.ImportSkill(webSearchEngineSkill, "WebSearch");
            kernel.ImportSkill(new LanguageCalculatorSkill(kernel), "semanticCalculator");
            kernel.ImportSkill(new TimeSkill(), "time");

            // StepwisePlanner is instructed to depend on available functions.
            // We expose this function to increase the flexibility in it's ability to answer
            // given the relatively small number of functions we have in this example.
            // This seems to be particularly helpful in these examples with gpt-35-turbo -- even though it
            // does not *use* this function. It seems to help the planner find a better path to the answer.
            kernel.CreateSemanticFunction(
                "Generate an answer for the following question: {{$input}}",
                functionName: "GetAnswerForQuestion",
                skillName: "AnswerBot",
                description: "Given a question, get an answer and return it as the result of the function");

            await StartChatAsync(kernel);
        }

        private static async Task StartChatAsync(IKernel kernel)
        {
            // Spacecraft and Capital failed on Internet
            // Local: GGML_ASSERT: ggml.c:4785: view_src == NULL || data_size + view_offs <= ggml_nbytes(view_src)
            string[] questions = new string[]
        {
            //"If a spacecraft travels at 0.99 the speed of light and embarks on a journey to the nearest star system, Alpha Centauri, which is approximately 4.37 light-years away, how much time would pass on Earth during the spacecraft's voyage?",
            "What color is the sky?",
            "What is the weather in Seattle?",
            "What is the tallest mountain on Earth? How tall is it divided by 2?",
            //"What is the capital of France? Who is that city's current mayor? What percentage of their life has been in the 21st century as of today?",
            "What is the current day of the calendar year? Using that as an angle in degrees, what is the area of a unit circle with that angle?",
        };

            foreach (var question in questions)
            {
                for (int i = 0; i < 1; i++)
                {
                    await RunTextCompletion(question, kernel);
                    // await RunChatCompletion(question, kernel);
                }
            }

            PrintResults();
        }

        // print out summary table of ExecutionResults
        private static void PrintResults()
        {
            Console.WriteLine("**************************");
            Console.WriteLine("Execution Results Summary:");
            Console.WriteLine("**************************");

            foreach (var question in ExecutionResults.Select(s => s.question).Distinct())
            {
                Console.WriteLine("Question: " + question);
                Console.WriteLine("Mode\tModel\tAnswer\tStepsTaken\tIterations\tTimeTaken");
                foreach (var er in ExecutionResults.OrderByDescending(s => s.model).Where(s => s.question == question))
                {
                    Console.WriteLine($"{er.mode}\t{er.model}\t{er.stepsTaken}\t{er.iterations}\t{er.timeTaken}\t{er.answer}");
                }
            }
        }

        private struct ExecutionResult
        {
            public string mode;
            public string? model;
            public string? question;
            public string? answer;
            public string? stepsTaken;
            public string? iterations;
            public string? timeTaken;
        }

        private static List<ExecutionResult> ExecutionResults = new();

        private static async Task RunTextCompletion(string question, IKernel kernel)
        {
            Console.WriteLine("RunTextCompletion");
            ExecutionResult currentExecutionResult = default;
            currentExecutionResult.mode = "RunTextCompletion";
            await RunWithQuestion(kernel, currentExecutionResult, question, TextMaxTokens);
        }

        private static async Task RunChatCompletion(string question, IKernel kernel, string? model = null)
        {
            Console.WriteLine("RunChatCompletion");
            ExecutionResult currentExecutionResult = default;
            currentExecutionResult.mode = "RunChatCompletion";
            await RunWithQuestion(kernel, currentExecutionResult, question, ChatMaxTokens);
        }

        private static async Task RunWithQuestion(IKernel kernel, ExecutionResult currentExecutionResult, string question, int? MaxTokens = null)
        {
            currentExecutionResult.question = question;
        

            Console.WriteLine("*****************************************************");
            Console.WriteLine("Question: " + question);

            var plannerConfig = new Microsoft.SemanticKernel.Planning.Stepwise.StepwisePlannerConfig();
            plannerConfig.ExcludedFunctions.Add("TranslateMathProblem");
            plannerConfig.ExcludedFunctions.Add("DaysAgo");
            plannerConfig.ExcludedFunctions.Add("DateMatchingLastDayName");
            plannerConfig.MinIterationTimeMs = 1500;
            plannerConfig.MaxIterations = 25;

            if (!string.IsNullOrEmpty(Suffix))
            {
                plannerConfig.Suffix = $"{Suffix}\n{plannerConfig.Suffix}";
                currentExecutionResult.question = $"[Assisted] - {question}";
            }

            if (MaxTokens.HasValue)
            {
                plannerConfig.MaxTokens = MaxTokens.Value;
            }
            Stopwatch sw = new();

            SKContext result;
            sw.Start();

            try
            {
                StepwisePlanner planner = new(kernel: kernel, config: plannerConfig);
                var plan = planner.CreatePlan(question);

                result = await plan.InvokeAsync(kernel.CreateNewContext());

                if (result.Result.Contains("Result not found, review _stepsTaken to see what", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Could not answer question in " + plannerConfig.MaxIterations + " iterations");
                    currentExecutionResult.answer = "Could not answer question in " + plannerConfig.MaxIterations + " iterations";
                }
                else
                {
                    Console.WriteLine("Result: " + result.Result);
                    currentExecutionResult.answer = result.Result;
                }

                if (result.Variables.TryGetValue("stepCount", out string? stepCount))
                {
                    Console.WriteLine("Steps Taken: " + stepCount);
                    currentExecutionResult.stepsTaken = stepCount;
                }

                if (result.Variables.TryGetValue("skillCount", out string? skillCount))
                {
                    Console.WriteLine("Skills Used: " + skillCount);
                }

                if (result.Variables.TryGetValue("iterations", out string? iterations))
                {
                    Console.WriteLine("Iterations: " + iterations);
                    currentExecutionResult.iterations = iterations;
                }
            }
#pragma warning disable CA1031
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex);
            }

            Console.WriteLine("Time Taken: " + sw.Elapsed);
            currentExecutionResult.timeTaken = sw.Elapsed.ToString();
            ExecutionResults.Add(currentExecutionResult);
            Console.WriteLine("*****************************************************");
        }
    }
}
