using Microsoft.SemanticKernel.Connectors.AI.OpenAI.TextCompletion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SemanticKernelExamples.Examples
{
    internal class Example32_StreamingCompletion
    {
        public static async Task Run(bool internet = false)
        {
            ITextCompletion? textCompletion = null;

            if (internet)
            {
                textCompletion = new OpenAITextCompletion("text-davinci-003", Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new NotImplementedException("OPENAI_API_KEY"));
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
                var ex = new InstructExecutor(context);
                textCompletion = new LLamaSharpTextCompletion(ex);
            }

            await TextCompletionStreamAsync(textCompletion);
        }

        private static async Task TextCompletionStreamAsync(ITextCompletion textCompletion)
        {
            var requestSettings = new CompleteRequestSettings()
            {
                MaxTokens = 100,
                FrequencyPenalty = 0,
                PresencePenalty = 0,
                Temperature = 1,
                TopP = 0.5
            };

            var prompt = "Write one paragraph why AI is awesome.";

            Console.WriteLine("Prompt: " + prompt);
            await foreach (string message in textCompletion.CompleteStreamAsync(prompt, requestSettings))
            {
                Console.Write(message);
            }

            Console.WriteLine();
        }
    }
}
