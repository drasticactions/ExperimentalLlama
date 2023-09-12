using LLamaSharp.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Skills.Web.Bing;
using Microsoft.SemanticKernel.Skills.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SemanticKernel.TemplateEngine.Prompt;

namespace SemanticKernelExamples.Examples
{
    internal class Example07_BingAndGoogleSkills
    {
        public static async Task Run(bool internet = false)
        {
            IKernel kernel;

            if (internet)
            {
                kernel = new KernelBuilder()
                .WithLoggerFactory(ConsoleLogger.LoggerFactory)
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
                    ContextSize = 8192,
                    GpuLayerCount = 50
                };
                var model = LLamaWeights.LoadFromFile(parameters);
                var context = model.CreateContext(parameters);
                var ex = new InstructExecutor(context);
                var text = new LLamaSharpTextCompletion(ex);
                kernel = new KernelBuilder()
                    .WithAIService<ITextCompletion>("local-llama-text", text)
                    .Build();
            }

            var bingConnector = new BingConnector(Environment.GetEnvironmentVariable("BING") ?? throw new NotImplementedException("BING Missing"));
            var bing = new WebSearchEngineSkill(bingConnector);
            var search = kernel.ImportSkill(bing, "bing");

            await Example1Async(kernel, "bing");
            await Example2Async(kernel);
        }

        private static async Task Example1Async(IKernel kernel, string searchSkillId)
        {
            Console.WriteLine("======== Bing and Google Search Skill ========");

            // Run
            var question = "What's the largest building in the world?";
            var result = await kernel.Func(searchSkillId, "search").InvokeAsync(question);

            Console.WriteLine(question);
            Console.WriteLine($"----{searchSkillId}----");
            Console.WriteLine(result);

            /* OUTPUT:

                What's the largest building in the world?
                ----
                The Aerium near Berlin, Germany is the largest uninterrupted volume in the world, while Boeing's
                factory in Everett, Washington, United States is the world's largest building by volume. The AvtoVAZ
                main assembly building in Tolyatti, Russia is the largest building in area footprint.
                ----
                The Aerium near Berlin, Germany is the largest uninterrupted volume in the world, while Boeing's
                factory in Everett, Washington, United States is the world's ...
           */
        }

        private static async Task Example2Async(IKernel kernel)
        {
            Console.WriteLine("======== Use Search Skill to answer user questions ========");

            const string SemanticFunction = @"Answer questions only when you know the facts or the information is provided.
When you don't have sufficient information you reply with a list of commands to find the information needed.
When answering multiple questions, use a bullet point list.
Note: make sure single and double quotes are escaped using a backslash char.

[COMMANDS AVAILABLE]
- bing.search

[INFORMATION PROVIDED]
{{ $externalInformation }}

[EXAMPLE 1]
Question: what's the biggest lake in Italy?
Answer: Lake Garda, also known as Lago di Garda.

[EXAMPLE 2]
Question: what's the biggest lake in Italy? What's the smallest positive number?
Answer:
* Lake Garda, also known as Lago di Garda.
* The smallest positive number is 1.

[EXAMPLE 3]
Question: what's Ferrari stock price? Who is the current number one female tennis player in the world?
Answer:
{{ '{{' }} bing.search ""what\\'s Ferrari stock price?"" {{ '}}' }}.
{{ '{{' }} bing.search ""Who is the current number one female tennis player in the world?"" {{ '}}' }}.

[END OF EXAMPLES]

[TASK]
Question: {{ $input }}.
Answer: ";

            var questions = "Who is the most followed person on TikTok right now? What's the exchange rate EUR:USD?";
            Console.WriteLine(questions);

            var oracle = kernel.CreateSemanticFunction(SemanticFunction, maxTokens: 200, temperature: 0, topP: 1);

            var answer = await kernel.RunAsync(oracle, new(questions)
            {
                ["externalInformation"] = string.Empty
            });

            // If the answer contains commands, execute them using the prompt renderer.
            if (answer.Result.Contains("bing.search", StringComparison.OrdinalIgnoreCase))
            {
                var promptRenderer = new PromptTemplateEngine();

                Console.WriteLine("---- Fetching information from Bing...");
                var information = await promptRenderer.RenderAsync(answer.Result, kernel.CreateNewContext());

                Console.WriteLine("Information found:");
                Console.WriteLine(information);

                // Run the semantic function again, now including information from Bing
                answer = await kernel.RunAsync(oracle, new(questions)
                {
                    // The rendered prompt contains the information retrieved from search engines
                    ["externalInformation"] = information
                });
            }
            else
            {
                Console.WriteLine("AI had all the information, no need to query Bing.");
            }

            Console.WriteLine("---- ANSWER:");
            Console.WriteLine(answer);

            /* OUTPUT:

                Who is the most followed person on TikTok right now? What's the exchange rate EUR:USD?
                ---- Fetching information from Bing...
                Information found:

                Khaby Lame is the most-followed user on TikTok. This list contains the top 50 accounts by number
                of followers on the Chinese social media platform TikTok, which was merged with musical.ly in 2018.
                [1] The most-followed individual on the platform is Khaby Lame, with over 153 million followers..
                EUR – Euro To USD – US Dollar 1.00 Euro = 1.10 37097 US Dollars 1 USD = 0.906035 EUR We use the
                mid-market rate for our Converter. This is for informational purposes only. You won’t receive this
                rate when sending money. Check send rates Convert Euro to US Dollar Convert US Dollar to Euro..
                ---- ANSWER:

                * The most followed person on TikTok right now is Khaby Lame, with over 153 million followers.
                * The exchange rate for EUR to USD is 1.1037097 US Dollars for 1 Euro.
             */
        }
    }
}
