using LLama;
using LLama.Common;
using LLamaSharp.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.ChatCompletion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SemanticKernelExamples.Examples
{
    internal class Example17_ChatGPT
    {
        public static async Task Run(bool internet = false)
        {
            IChatCompletion? chatGPT = null;
            if (internet)
            {
                Console.WriteLine("Internet");
                chatGPT = new OpenAIChatCompletion("gpt-3.5-turbo", Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new NotImplementedException("OPENAI_API_KEY"));
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
                using var model = LLamaWeights.LoadFromFile(parameters);
                using var context = model.CreateContext(parameters);
                var ex = new InteractiveExecutor(context);

                chatGPT = new LLamaSharpChatCompletion(ex);
            }

            await StartChatAsync(chatGPT);
        }

        private static async Task StartChatAsync(IChatCompletion chatGPT)
        {
            Console.WriteLine("Chat content:");
            Console.WriteLine("------------------------");

            var chatHistory = chatGPT.CreateNewChat("You are a librarian, expert about books");

            // First user message
            chatHistory.AddUserMessage("Hi, I'm looking for book suggestions");
            await MessageOutputAsync(chatHistory);

            // First bot assistant message
            string reply = await chatGPT.GenerateMessageAsync(chatHistory);
            chatHistory.AddAssistantMessage(reply);
            await MessageOutputAsync(chatHistory);

            // Second user message
            chatHistory.AddUserMessage("I love history and philosophy, I'd like to learn something new about Greece, any suggestion");
            await MessageOutputAsync(chatHistory);

            // Second bot assistant message
            reply = await chatGPT.GenerateMessageAsync(chatHistory);
            chatHistory.AddAssistantMessage(reply);
            await MessageOutputAsync(chatHistory);
        }

        /// <summary>
        /// Outputs the last message of the chat history
        /// </summary>
        private static Task MessageOutputAsync(Microsoft.SemanticKernel.AI.ChatCompletion.ChatHistory chatHistory)
        {
            var message = chatHistory.Messages.Last();

            Console.WriteLine($"{message.Role}: {message.Content}");
            Console.WriteLine("------------------------");

            return Task.CompletedTask;
        }
    }
}
