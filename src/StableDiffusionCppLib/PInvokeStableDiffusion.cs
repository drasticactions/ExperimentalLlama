using System.Runtime.InteropServices;

namespace StableDiffusionCppLib
{
    public enum SDLogLevel { DEBUG, INFO, WARN, ERROR }
    public enum RNGType { STD_DEFAULT_RNG, CUDA_RNG }
    public enum SampleMethod { EULAR_A }

    public static class PInvokeStableDiffusion
    {
#if WINDOWS
        private const string DllName = $"stable-diffusion";
#elif LINUX || MACOS
        private const string DllName = $"libstable-diffusion";
#endif

        [DllImport(DllName)]
        public static extern IntPtr StableDiffusion_Create(int n_threads,
                                                          bool vae_decode_only,
                                                          bool free_params_immediately,
                                                          RNGType rng_type);

        [DllImport(DllName)]
        public static extern bool StableDiffusion_LoadFromFile(IntPtr sd, string file_path);

        [DllImport(DllName)]
        public static extern void StableDiffusion_Txt2Img_Path(IntPtr sd,
                                                            string prompt,
                                                            string negative_prompt,
                                                            float cfg_scale,
                                                            int width,
                                                            int height,
                                                            SampleMethod sample_method,
                                                            int sample_steps,
                                                            long seed,
                                                            string outputPath);

        [DllImport(DllName)]
        public static extern IntPtr StableDiffusion_Img2Img_Path(IntPtr sd,
                                                            string path,
                                                            string prompt,
                                                            string negative_prompt,
                                                            float cfg_scale,
                                                            int width,
                                                            int height,
                                                            SampleMethod sample_method,
                                                            int sample_steps,
                                                            float strength,
                                                            long seed,
                                                            string outputPath);
    }
}
