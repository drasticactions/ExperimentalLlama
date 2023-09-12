﻿using SemanticKernelExamples.Examples;

internal class Program
{
    private static async Task Main(string[] args)
    {
        Console.WriteLine("Please input a number to choose an example to run:");
        Console.WriteLine("0: Internet");
        Console.WriteLine("1: Local");

        while(true)
        {
            Console.Write("\nYour choice: ");
            int choice = int.Parse(Console.ReadLine());

            if (choice == 0)
            {
                await Runner(true);
            }
            else if (choice == 1)
            {
                await Runner(false);
            }
            else
            {
                Console.WriteLine("Cannot parse your choice. Please select again.");
                continue;
            }
            break;
        }
    }

    private static async Task Runner(bool internet)
    {
        Console.WriteLine("Please input a number to choose an example to run:");
        Console.WriteLine("0: Example04_CombineLLMPromptsAndNativeCode");
        Console.WriteLine("1: Example13_ConversationSummarySkill");
        Console.WriteLine("2: Example14_SemanticMemory");
        Console.WriteLine("3: Example17_ChatGPT");
        Console.WriteLine("4: Example18_DallE");
        Console.WriteLine("5: Example28_ActionPlanner");
        Console.WriteLine("6: Example32_StreamingCompletion");
        Console.WriteLine("7: Example48_GroundednessChecks");
        Console.WriteLine("8: Example49_LogitBias");
        Console.WriteLine("9: Example51_StepwisePlanner");
        Console.WriteLine("10: StableDiffusion_Example");
        Console.WriteLine("11: Example07_BingAndGoogleSkills");
        Console.WriteLine("12: Example15_TextMemorySkill");
        while (true)
        {
            Console.Write("\nYour choice: ");
            int choice = int.Parse(Console.ReadLine());

            if (choice == 0)
            {
                await Example04_CombineLLMPromptsAndNativeCode.Run(internet);
            }
            else if (choice == 1)
            {
                await Example13_ConversationSummarySkill.Run(internet);
            }
            else if (choice == 2)
            {
                await Example14_SemanticMemory.Run(internet);
            }
            else if (choice == 3)
            {
                await Example17_ChatGPT.Run(internet);
            }
            else if (choice == 4)
            {
                await Example18_DallE.Run(internet);
            }
            else if (choice == 5)
            {
                await Example28_ActionPlanner.Run(internet);
            }
            else if (choice == 6)
            {
                await Example32_StreamingCompletion.Run(internet);
            }
            else if (choice == 7)
            {
                await Example48_GroundednessChecks.Run(internet);
            }
            else if (choice == 8)
            {
                await Example49_LogitBias.Run(internet);
            }
            else if (choice == 9)
            {
                await Example51_StepwisePlanner.Run(internet);
            }
            else if (choice == 10)
            {
                await StableDiffusion_Example.Run(internet);
            }
            else if (choice == 11)
            {
                await Example07_BingAndGoogleSkills.Run(internet);
            }
            else if (choice == 12)
            {
                await Example15_TextMemorySkill.Run(internet);
            }
            else
            {
                Console.WriteLine("Cannot parse your choice. Please select again.");
                continue;
            }
            break;
        }
    }
}