using System.IO.Compression;

namespace Minecraft_Easy_Servers.Helpers
{
    public static class ArchiveHelper
    {
        public static string ExtractZipAndIsolateContentAddPrefix(string archivePath, string directoryForContentPath, string prefixName, string? searchForFileWithExtension = null, bool contentIsFolder = false)
        {
            string extractedFolderPath = Path.Combine(directoryForContentPath, Path.GetFileNameWithoutExtension(archivePath));
            if (Directory.Exists(extractedFolderPath))
                Directory.Delete(extractedFolderPath, true);
            ZipFile.ExtractToDirectory(archivePath, extractedFolderPath);

            string[] files = Directory.GetFiles(extractedFolderPath, "*", SearchOption.TopDirectoryOnly);
            string[] directories = Directory.GetDirectories(extractedFolderPath, "*", SearchOption.TopDirectoryOnly);

            if (files.Length == 0 && directories.Length == 0)
                throw new Exception("No files or folders found in the archive.");

            if (contentIsFolder)
            {
                // Check if the extracted folder contains a single folder or multiple items
                if (directories.Length == 1 && files.Length == 0)
                {
                    // Single folder case: Move the folder directly to the target directory with a new name
                    string singleFolderPath = directories[0];
                    string folderName = Path.GetFileName(singleFolderPath);
                    string destinationPath = Path.Combine(directoryForContentPath, $"{prefixName}_{folderName}");
                    if (Directory.Exists(destinationPath))
                        Directory.Delete(destinationPath, true);
                    Directory.Move(singleFolderPath, destinationPath);
                    Directory.Delete(extractedFolderPath, true);
                    return destinationPath;
                }
                else
                {
                    var targetDirectory = Path.Combine(directoryForContentPath, $"{prefixName}_{Path.GetFileNameWithoutExtension(archivePath)}");
                    if (Directory.Exists(targetDirectory))
                        Directory.Delete(targetDirectory, true);
                    Directory.CreateDirectory(targetDirectory);
                    // Multiple items case: Move all directories and files to the target directory
                    foreach (string directory in directories)
                    {
                        string folderName = Path.GetFileName(directory);
                        string destinationPath = Path.Combine(directoryForContentPath, folderName);
                        Directory.Move(directory, destinationPath);
                    }

                    foreach (string file in files)
                    {
                        string fileName = Path.GetFileName(file);
                        string finalPath = Path.Combine(directoryForContentPath, fileName);
                        File.Move(file, finalPath);
                    }
                    Directory.Delete(extractedFolderPath, true);
                    return targetDirectory;
                }
            }
            else
            {
                var file = files.FirstOrDefault(x => searchForFileWithExtension == null || Path.GetExtension(x) == searchForFileWithExtension) ?? throw new Exception($"File with extension {searchForFileWithExtension ?? string.Empty} not found");
                string fileName = Path.GetFileName(file);
                string destinationPath = Path.Combine(directoryForContentPath, $"{prefixName}_{fileName}");
                File.Move(file, destinationPath);
                Directory.Delete(extractedFolderPath, true);
                return destinationPath;
            }

        }
    }
}
