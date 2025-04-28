using System.Formats.Tar;
using System.IO.Compression;

namespace Minecraft_Easy_Servers.Helpers
{
    public static class ArchiveHelper
    {
        public static string ExtractArchiveAndIsolateContentAddPrefix(string archivePath, string directoryForContentPath, string prefixName, string? searchForFileWithExtension = null, bool contentIsFolder = false)
        {
            string extractedFolderPath = Path.Combine(directoryForContentPath, Path.GetFileNameWithoutExtension(archivePath));
            if (Directory.Exists(extractedFolderPath))
                Directory.Delete(extractedFolderPath, true);

            // Détection du type d'archive
            if (Path.GetExtension(archivePath).Equals(".zip", StringComparison.OrdinalIgnoreCase))
            {
                ZipFile.ExtractToDirectory(archivePath, extractedFolderPath);
            }
            else if (Path.GetExtension(archivePath).Equals(".gz", StringComparison.OrdinalIgnoreCase) && Path.GetExtension(Path.GetFileNameWithoutExtension(archivePath)).Equals(".tar", StringComparison.OrdinalIgnoreCase))
            {
                // Extraction des fichiers tar.gz
                using (FileStream fileStream = new FileStream(archivePath, FileMode.Open, FileAccess.Read))
                using (GZipStream gzipStream = new GZipStream(fileStream, CompressionMode.Decompress))
                using (FileStream tarStream = new FileStream(extractedFolderPath + ".tar", FileMode.Create, FileAccess.Write))
                {
                    gzipStream.CopyTo(tarStream);
                }

                // Extraction du fichier TAR
                string tarExtractedFolderPath = extractedFolderPath;
                Directory.CreateDirectory(tarExtractedFolderPath);
                using (FileStream tarFileStream = new FileStream(extractedFolderPath + ".tar", FileMode.Open, FileAccess.Read))
                {
                    TarFile.ExtractToDirectory(tarFileStream, tarExtractedFolderPath, overwriteFiles: true);
                }

                // Supprimer le fichier TAR temporaire
                File.Delete(extractedFolderPath + ".tar");
            }
            else
            {
                throw new NotSupportedException("Unsupported archive format. Only .zip and .tar.gz are supported.");
            }

            // Reste du traitement (identique à votre méthode existante)
            string[] files = Directory.GetFiles(extractedFolderPath, "*", SearchOption.TopDirectoryOnly);
            string[] directories = Directory.GetDirectories(extractedFolderPath, "*", SearchOption.TopDirectoryOnly);

            if (files.Length == 0 && directories.Length == 0)
                throw new Exception("No files or folders found in the archive.");

            if (contentIsFolder)
            {
                if (directories.Length == 1 && files.Length == 0)
                {
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
                File.Move(file, destinationPath, overwrite: true);
                Directory.Delete(extractedFolderPath, true);
                return destinationPath;
            }
        }
    }
}
