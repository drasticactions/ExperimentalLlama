using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SemanticKernelExamples
{
    public static class RepoFiles
    {
        /// <summary>
        /// Scan the local folders from the repo, looking for "samples/skills" folder.
        /// </summary>
        /// <returns>The full path to samples/skills</returns>
        public static string SampleSkillsPath()
        {
            const string Parent = "samples";
            const string Folder = "skills";

            bool SearchPath(string pathToFind, out string result, int maxAttempts = 10)
            {
                var currDir = Path.GetFullPath(Assembly.GetExecutingAssembly().Location);
                bool found;
                do
                {
                    result = Path.Join(currDir, pathToFind);
                    found = Directory.Exists(result);
                    currDir = Path.GetFullPath(Path.Combine(currDir, ".."));
                } while (maxAttempts-- > 0 && !found);

                return found;
            }

            if (!SearchPath(Parent + Path.DirectorySeparatorChar + Folder, out string path)
                && !SearchPath(Folder, out path))
            {
                throw new Exception("Skills directory not found. The app needs the skills from the repo to work.");
            }

            return path;
        }
    }

}
