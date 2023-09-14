using LLamaSharp.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.ChatCompletion;
using Microsoft.SemanticKernel.Memory;

namespace SemanticKernelExamples.Examples
{
    public class Example48_GroundednessChecks
    {
        public static async Task Run(bool internet = false)
        {
            IKernel kernel;
            if (internet)
            {
                Console.WriteLine("Internet");
                kernel = Kernel.Builder
                .WithLoggerFactory(ConsoleLogger.LoggerFactory)
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
                };
                var model = LLamaWeights.LoadFromFile(parameters);
                var context = model.CreateContext(parameters);
                kernel = Kernel.Builder
                    .WithLoggerFactory(ConsoleLogger.LoggerFactory)
                    .WithAIService<ITextCompletion>("local-llama-text", new LLamaSharpTextCompletion(new InstructExecutor(context)), true)
                    .Build();
            }

            await StartChatAsync(kernel);
        }

        private static async Task StartChatAsync(IKernel kernel)
        {
            Console.WriteLine("======== Groundedness Checks ========");

            string folder = RepoFiles.SampleSkillsPath();
            var functions = kernel.ImportSemanticSkillFromDirectory(folder,
                "SummarizeSkill",
                "GroundingSkill");

            var create_summary = functions["Summarize"];
            var entityExtraction = functions["ExtractEntities"];
            var reference_check = functions["ReferenceCheckEntities"];
            var entity_excision = functions["ExciseEntities"];

            var summaryText = @"
My father, a respected resident of Milan, was a close friend of a merchant named Beaufort who, after a series of
misfortunes, moved to Zurich in poverty. My father was upset by his friend's troubles and sought him out,
finding him in a mean street. Beaufort had saved a small sum of money, but it was not enough to support him and
his daughter, Mary. Mary procured work to eek out a living, but after ten months her father died, leaving
her a beggar. My father came to her aid and two years later they married.
";

            var context = kernel.CreateNewContext();
            context.Variables.Update(summaryText);
            context.Variables.Set("topic", "people and places");
            context.Variables.Set("example_entities", "John, Jane, mother, brother, Paris, Rome");

            var extractionResult = (await entityExtraction.InvokeAsync(context)).Result;

            Console.WriteLine("======== Extract Entities ========");
            Console.WriteLine(extractionResult);

            context.Variables.Update(extractionResult);
            context.Variables.Set("reference_context", s_groundingText);

            var groundingResult = (await reference_check.InvokeAsync(context)).Result;

            Console.WriteLine("======== Reference Check ========");
            Console.WriteLine(groundingResult);

            context.Variables.Update(summaryText);
            context.Variables.Set("ungrounded_entities", groundingResult);
            var excisionResult = await entity_excision.InvokeAsync(context);

            Console.WriteLine("======== Excise Entities ========");
            Console.WriteLine(excisionResult.Result);
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

        private static string s_groundingText = @"""I am by birth a Genevese, and my family is one of the most distinguished of that republic.
My ancestors had been for many years counsellors and syndics, and my father had filled several public situations
with honour and reputation.He was respected by all who knew him for his integrity and indefatigable attention
to public business.He passed his younger days perpetually occupied by the affairs of his country; a variety
of circumstances had prevented his marrying early, nor was it until the decline of life that he became a husband
and the father of a family.

As the circumstances of his marriage illustrate his character, I cannot refrain from relating them.One of his
most intimate friends was a merchant who, from a flourishing state, fell, through numerous mischances, into poverty.
This man, whose name was Beaufort, was of a proud and unbending disposition and could not bear to live in poverty
and oblivion in the same country where he had formerly been distinguished for his rank and magnificence. Having
paid his debts, therefore, in the most honourable manner, he retreated with his daughter to the town of Lucerne,
where he lived unknown and in wretchedness.My father loved Beaufort with the truest friendship and was deeply
grieved by his retreat in these unfortunate circumstances.He bitterly deplored the false pride which led his friend
to a conduct so little worthy of the affection that united them.He lost no time in endeavouring to seek him out,
with the hope of persuading him to begin the world again through his credit and assistance.

Beaufort had taken effectual measures to conceal himself, and it was ten months before my father discovered his
abode.Overjoyed at this discovery, he hastened to the house, which was situated in a mean street near the Reuss.
But when he entered, misery and despair alone welcomed him. Beaufort had saved but a very small sum of money from
the wreck of his fortunes, but it was sufficient to provide him with sustenance for some months, and in the meantime
he hoped to procure some respectable employment in a merchant's house. The interval was, consequently, spent in
inaction; his grief only became more deep and rankling when he had leisure for reflection, and at length it took
so fast hold of his mind that at the end of three months he lay on a bed of sickness, incapable of any exertion.

His daughter attended him with the greatest tenderness, but she saw with despair that their little fund was
rapidly decreasing and that there was no other prospect of support.But Caroline Beaufort possessed a mind of an
uncommon mould, and her courage rose to support her in her adversity. She procured plain work; she plaited straw
and by various means contrived to earn a pittance scarcely sufficient to support life.

Several months passed in this manner.Her father grew worse; her time was more entirely occupied in attending him;
    her means of subsistence decreased; and in the tenth month her father died in her arms, leaving her an orphan and
a beggar.This last blow overcame her, and she knelt by Beaufort's coffin weeping bitterly, when my father entered
the chamber. He came like a protecting spirit to the poor girl, who committed herself to his care; and after the
interment of his friend he conducted her to Geneva and placed her under the protection of a relation.Two years
after this event Caroline became his wife.""";
    }
}
