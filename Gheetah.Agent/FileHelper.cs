using System.IO.Compression;

namespace Gheetah.Agent
{
    public static class FileHelper
    {
        public static async Task<string> ExtractZipAsync(string zipPath, string processId)
        {
            try
            {
                string extractPath = Path.Combine(Path.GetTempPath(), $"Gheetah_{DateTime.Now:yyyyMMdd_HHmmss}_{processId}");
                Directory.CreateDirectory(extractPath);

                await Task.Run(() => ZipFile.ExtractToDirectory(zipPath, extractPath));
                Console.WriteLine($"Zip file successfully extracted: {extractPath}");
                return extractPath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Zip extraction error: {ex.Message}");
                throw;
            }
        }

        public static void CleanUp(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                    Console.WriteLine($"Temporary folder deleted: {path}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting temporary folder: {ex.Message}");
            }
        }
    }
}