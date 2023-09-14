using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.AI.ImageGeneration;
using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SemanticKernelExamples.Examples
{
    public class StableDiffusion_Example
    {
        public static async Task Run(bool internet = false)
        {
            Console.Write("Please input your stable diffusion model path: ");
            var sdPath = Console.ReadLine();
            var imageGeneration = new StableDiffusionCppImageGeneration(sdPath, 20);

            var kernel = new KernelBuilder()
                .WithAIService<IImageGeneration>("local-sd", imageGeneration)
                .Build();

            IImageGeneration dallE = kernel.GetService<IImageGeneration>();

            var imageDescription = "A nice cat";
            var image = await dallE.GenerateImageAsync(imageDescription, 512, 512);

            Console.WriteLine(imageDescription);
            Console.WriteLine("Image URL: " + image);
        }
    }
}
