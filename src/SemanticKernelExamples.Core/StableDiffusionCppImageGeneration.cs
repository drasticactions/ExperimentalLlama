using Microsoft.SemanticKernel.AI.ImageGeneration;
using StableDiffusionCppLib;

namespace SemanticKernelExamples;

public class StableDiffusionCppImageGeneration : IImageGeneration, IDisposable
{
    private bool disposedValue;

    private IntPtr sd = IntPtr.Zero;
    private int steps = 20;
    private string imageLocation;

    public StableDiffusionCppImageGeneration(string model, int steps = 20, string? imageLocation = default)
    {
        this.steps = steps;
        this.sd = PInvokeStableDiffusion.StableDiffusion_Create(10, false, false, RNGType.STD_DEFAULT_RNG);
        var result = PInvokeStableDiffusion.StableDiffusion_LoadFromFile(sd, model);
        if (!result)
        {
            throw new Exception("Failed to load model.");
        }

        this.imageLocation = imageLocation ?? Path.GetTempPath();
    }

    public Task<string> GenerateImageAsync(string description, int width, int height, CancellationToken cancellationToken = default)
    {
        var outputPath = Path.Combine(imageLocation, $"{Guid.NewGuid()}.png");
        PInvokeStableDiffusion.StableDiffusion_Txt2Img_Path(sd, description, "", 1.0f, width, height, SampleMethod.EULAR_A, steps, 1, outputPath);
        return Task.FromResult(outputPath);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                this.sd = IntPtr.Zero;
            }

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
