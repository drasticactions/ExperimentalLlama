using Azure.AI.OpenAI;
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
    internal class Example49_LogitBias
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
                var model = LLamaWeights.LoadFromFile(parameters);
                var context = model.CreateContext(parameters);
                var ex = new InteractiveExecutor(context);

                chatGPT = new LLamaSharpChatCompletion(ex);
            }

            await StartChatAsync(chatGPT);
        }

        private static async Task StartChatAsync(IChatCompletion chatCompletion)
        {
            // The following text is the tokenized version of the book related tokens
            // "novel literature reading author library story chapter paperback hardcover ebook publishing fiction nonfiction manuscript textbook bestseller bookstore reading list bookworm"
            var keys = new[] { 3919, 626, 17201, 1300, 25782, 9800, 32016, 13571, 43582, 20189, 1891, 10424, 9631, 16497, 12984, 20020, 24046, 13159, 805, 15817, 5239, 2070, 13466, 32932, 8095, 1351, 25323 };

            var settings = new ChatRequestSettings() { MaxTokens = 512 };

            // This will make the model try its best to avoid any of the above related words.
            foreach (var key in keys)
            {
                //This parameter maps tokens to an associated bias value from -100 (a potential ban) to 100 (exclusive selection of the token).

                //-100 to potentially ban all the tokens from the list.
                settings.TokenSelectionBiases.Add(key, -100);
            }

            Console.WriteLine("Chat content:");
            Console.WriteLine("------------------------");

            var chatHistory = chatCompletion.CreateNewChat("You are a librarian expert");

            // First user message
            chatHistory.AddUserMessage("Hi, I'm looking some suggestions");
            await MessageOutputAsync(chatHistory);

            string reply = await chatCompletion.GenerateMessageAsync(chatHistory, settings);
            chatHistory.AddAssistantMessage(reply);
            await MessageOutputAsync(chatHistory);

            chatHistory.AddUserMessage("I love history and philosophy, I'd like to learn something new about Greece, any suggestion");
            await MessageOutputAsync(chatHistory);

            reply = await chatCompletion.GenerateMessageAsync(chatHistory, settings);
            chatHistory.AddAssistantMessage(reply);
            await MessageOutputAsync(chatHistory);

            /* Output:
            Chat content:
            ------------------------
            User: Hi, I'm looking some suggestions
            ------------------------
            Assistant: Sure, what kind of suggestions are you looking for?
            ------------------------
            User: I love history and philosophy, I'd like to learn something new about Greece, any suggestion?
            ------------------------
            Assistant: If you're interested in learning about ancient Greece, I would recommend the book "The Histories" by Herodotus. It's a fascinating account of the Persian Wars and provides a lot of insight into ancient Greek culture and society. For philosophy, you might enjoy reading the works of Plato, particularly "The Republic" and "The Symposium." These texts explore ideas about justice, morality, and the nature of love.
            ------------------------
            */
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
