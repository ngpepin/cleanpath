/*
 
 SYNOPSIS
    This console application recursively deletes zero-byte files and files matching user-defined regular expressions in a given directory.

 DESCRIPTION
    The application accepts several optional command-line parameters to specify the directory, matching files, and other behaviors.
    It traverses the given directory recursively (if specified) and deletes zero-byte files and files that match the user-defined regex.
    The application supports "safe mode" that prompts the user for confirmation before actual deletion.
    It also provides an option to back up files before deletion and to log the deleted file paths.

 PARAMETER --target-dir
    Specifies the directory you want to clean. Defaults to the current directory if not specified.

 PARAMETER --matches
    Specifies the regular expressions for file types to be deleted. Separate multiple expressions with a comma.

 PARAMETER -R
    Enables recursive deletion in subdirectories.

 PARAMETER --safe
    Enables safe mode, which prompts the user for confirmation before deletion.

 PARAMETER --safe-limit
    Specifies the number of files to show in safe mode before asking for confirmation. Default is 15.

 PARAMETER --logfile
    Specifies the path of the log file where deleted file paths will be stored.

 PARAMETER --backup
    Specifies the directory where files will be backed up before deletion.

 PARAMETER --verbose
    If provided, the application outputs additional information, such as the parameters chosen and directories being examined.

 PARAMETER --dir-matches
    Specifies the regular expressions for directory names to be deleted. Separate multiple expressions with a comma.

 EXAMPLE
    cleanpath --target-dir "C:\Folder1" --matches ".txt|.log" -R

    Deletes zero-byte files and files ending with .txt or .log in C:\Folder1 and its subdirectories.

 EXAMPLE
    cleanpath --target-dir "C:\Folder1" --matches ".tmp" --safe --safe-limit 10 --backup "C:\Backup"

    Deletes files ending with .tmp in C:\Folder1, but asks for user confirmation after showing the first 10 matches. 
    Also backs up the deleted files to C:\Backup.

 NOTES
    1. The application will skip directories it does not have permission to access, such as 'System Volume Information'.
    2. Be cautious when specifying the backup directory to ensure it does not conflict with the target directory.
    3. If in safe mode, the application will prompt the user for confirmation, showing up to 'safe-limit' number of files.

 
 */

using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace CleanPath
{
    class Program
    {
        static string? logfile = null, defaultLogfile = null;
        static bool verbose = false;
        static void Main(string[] args)
        {
            // Parse command-line arguments
            string? targetDir = null, backup = null;
            string[]? matches = null, dirMatches = null;
            bool R = false, safe = false, help = false;
            int safeLimit = 15;

            WriteLineOutput();
            if (verbose)
            {
                WriteLineOutput("  | Cleanpath performs automated file cleanup based on user-defined criteria.");
                WriteLineOutput("  | It deletes zero-byte files by default and files matching specific regular expressions");
                WriteOutput("  | if supplied by the user (and does so recursively if asked).");
            }
            
            if ((args.Length > 0) && (args[0] != "--help"))
            {
                if (verbose)
                {
                    WriteLineOutput(" Choose '--help' for more details.");
                    WriteLineOutput();
                }
  
                for (int i = 0; i < args.Length; i++)
                {
                    switch (args[i])
                    {
                        case "--target-dir":
                            targetDir = args[++i];
                            if (verbose) WriteLineOutput($" - Target directory: {targetDir}");
                            break;
                        case "--matches":
                            matches = args[++i].Split(',');
                            if (verbose) WriteLineOutput($" - File matches: {string.Join(", ", matches)}");
                            break;
                        case "--dir-matches":
                            dirMatches = args[++i].Split(',');
                            if (verbose) WriteLineOutput($" - Directory matches: {string.Join(", ", dirMatches)}");
                            break;
                        case "-R":
                            R = true;
                            if (verbose) WriteLineOutput(" - Recursive");
                            break;
                        case "--safe":
                            safe = true;
                            if (verbose) WriteLineOutput(" - Safe mode");
                            break;
                        case "--safe-limit":
                            safeLimit = int.Parse(args[++i]);
                            if (verbose) WriteLineOutput($" - Safe limit: {safeLimit}");
                            break;
                        case "--logfile":
                            logfile = args[++i];
                            if (verbose) WriteLineOutput($" - Logfile: {logfile}");
                            break;
                        case "--backup":
                            backup = args[++i];
                            if (verbose) WriteLineOutput($" - Backup: {backup}");
                            break;
                        case "-v":
                        case "--verbose":
                            verbose = true;
                            break;
                        case "--help":
                            help = true;
                            break;
                        default:
                            WriteLineOutput($"Unknown option: {args[i]}");
                            return;
                    }
                    /*
                    switch (args[i])
                    {
                        case "--target-dir":
                            targetDir = args[++i];
                            WriteLineOutput($" - Target directory: {targetDir}");
                            break;
                        case "--matches":
                            matches = args[++i].Split(',');
                            WriteOutput(" - Matching deletion regex(ex):");
                            foreach (string myRegex in matches)
                            {
                                WriteOutput($" {myRegex}");
                            }
                            WriteLineOutput(" + zero-byte files");

                            break;
                        case "-R":
                            R = true;
                            WriteLineOutput(" - Recursive");
                            break;
                        case "--safe":
                            safe = true;
                            break;
                        case "--safe-limit":
                            safeLimit = int.Parse(args[++i]);
                            break;
                        case "--logfile":
                            logfile = args[++i];
                            WriteLineOutput($" - Log file: {logfile}");
                            break;
                        case "--backup":
                            backup = args[++i];
                            WriteLineOutput($" - Backup directory: {backup}");
                            break;
                        case "--help":
                            help = true;
                            break;
                    }
                    */
                }
   
                if (safe && verbose)
                {
                    WriteLineOutput($" - Safe deletion for the first {safeLimit} files");
                    WriteLineOutput($"   (user asked for permission to proceed with ANY deletions)");
                }
            }
            else if (verbose)
            {
                WriteLineOutput();
            }

            // Show help if requested
            if (help || args[0] == "--help")
            {
                WriteLineOutput();
                ShowHelp();
                return;
            }

            // Main Execution
            string rootPath = string.IsNullOrEmpty(targetDir) ? Directory.GetCurrentDirectory() : targetDir;

            if (!Directory.Exists(rootPath))
            {
                WriteLineOutput("Error: The target directory does not exist.");
                return;
            }

            // Check for backup folder conflict
            if (!string.IsNullOrEmpty(backup))
            {
                string rootPathStr = Path.GetFullPath(rootPath);
                string backupPathStr = Path.GetFullPath(backup);
                if (rootPathStr == backupPathStr || backupPathStr.StartsWith(rootPathStr + Path.DirectorySeparatorChar))
                {
                    WriteLineOutput("Error: The backup folder path conflicts with the folder being cleaned.");
                    return;
                }
            }
            defaultLogfile = logfile;

            WriteLineOutput();
            WriteLineOutput("Loading entire directory structure. This may take a few minutes.");
            DeleteFilesAndFolders(rootPath, matches, dirMatches, R, safe, safeLimit, logfile, backup);

            if (R)
            {
                try
                {
                    foreach (var dir in Directory.GetDirectories(rootPath, "*", SearchOption.AllDirectories))
                    {
                        DeleteFilesAndFolders(dir, matches, dirMatches, false, safe, safeLimit, logfile, backup);
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    WriteLineOutput($"Skipping folder due to permission issues: {ex.Message}");
                }
                catch (Exception e)
                {
                    WriteLineOutput($"Skipping folder due to exception: {e.Message}");
                }
            }
        }

        static void ShowHelp()
        {
            WriteLineOutput("Usage: cleanpath [OPTIONS]");
            WriteLineOutput();
            WriteLineOutput("Options:");
            WriteLineOutput("  --target-dir    The target directory to clean. Defaults to the current directory if not specified.");
            WriteLineOutput("  --matches       Comma-separated list of regular expressions to match files for deletion.");
            WriteLineOutput("  --dir-matches   Comma-separated list of regular expressions to match directories for deletion.");
            WriteLineOutput("  -R              Enable recursive deletion in subdirectories.");
            WriteLineOutput("  --safe          Enable safe mode, which prompts the user before deletion.");
            WriteLineOutput("  --safe-limit    The limit for the number of files shown in safe mode before deletion. Default is 15.");
            WriteLineOutput("  --logfile       Specify a log file to which deleted file paths will be written.");
            WriteLineOutput("  --backup        Specify a backup folder where files will be copied before deletion.");
            WriteLineOutput("  -v, --verbose   Enable verbose output.");
            WriteLineOutput("  --help          Show this help message and exit.");
        }

        static void ExamineDirectory(string directoryPath)
        {
            string[] filePaths;

            // Get the list of files in the directory
            try
            {
                filePaths = Directory.GetFiles(directoryPath);
            }
            catch (UnauthorizedAccessException ex)
            {
                WriteLineOutput($"Skipping folder due to permission issues: {ex.Message}");
                return;
            }
            WriteOutput($"Examining directory: {directoryPath}");
            // Loop through each file and print a "." for each
            foreach (var filePath in filePaths)
            {
                WriteOutput(".");
            }

            // Print a newline after examining all files in the directory
            WriteLineOutput();
        }

        /*
        static void DeleteFiles(string path, string[]? matches, bool recursive, bool safe, int safeLimit, string? logfile, string? backup)
        {
            List<string> filesToDelete = new List<string>();

            // Call the ExamineDirectory method here


            ExamineDirectory(path);

            try
            {
                filesToDelete = Directory.GetFiles(path, "*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                                        .Where(file => new FileInfo(file).Length == 0 ||
                                         (matches != null && matches.Any(m => Regex.IsMatch(Path.GetFileName(file), m))))
                                        .ToList();
            }
            catch (UnauthorizedAccessException ep)
            {

                WriteLineOutput($"Skipping folder due to permission issues: {ep.Message}", logfile);
                return;
            }
            catch (Exception ed)
            {
                WriteLineOutput($"Skipping folder due to exception: {ed.Message}");
                return;
            }

            if (safe && filesToDelete.Count > 0)
            {
                var filesToShow = filesToDelete.Take(safeLimit);
                WriteLineOutput("Files to be deleted:");
                foreach (var file in filesToShow)
                {
                    WriteLineOutput(file);
                }

                Console.Write("Continue to delete? (Y/N): ");
                var choice = Console.ReadLine();
                if (!string.IsNullOrEmpty(choice))
                {
                    if (choice.ToLower() != "y")
                    {
                        return;
                    }
                }
            }

            foreach (var file in filesToDelete)
            {
                if (!string.IsNullOrEmpty(file))
                {
                    if (!string.IsNullOrEmpty(backup))
                    {
                        string? backupPath = Path.Combine(backup, Path.GetRelativePath(path, file));
                        string? backupDir = Path.GetDirectoryName(backupPath);
                        if (!string.IsNullOrEmpty(backupPath))
                        {

                            if (!string.IsNullOrEmpty(backupDir))
                            {
                                if (!Directory.Exists(backupDir))
                                {
                                    Directory.CreateDirectory(backupDir);
                                }
                            }
                            if (!string.IsNullOrEmpty(backupPath))
                            {
                                File.Copy(file, backupPath, true);
                            }
                        }
                    }
                    try
                    {
                        File.Delete(file);
                        WriteLineOutput($"Deleting {file}");
                    }
                    catch (Exception e)
                    {
                        WriteLineOutput($"Error deleting {file} ({e})");
                    }

                    if (!string.IsNullOrEmpty(logfile))
                    {
                        File.AppendAllText(logfile, file + Environment.NewLine);
                    }

                }
            }
        }
        */
        static void DeleteFilesAndFolders(string? targetDir, string[]? fileMatches, string[]? dirMatches, bool recursive, bool safe, int safeLimit, string? logfile, string? backup)
        {
            if (string.IsNullOrEmpty(targetDir) || !Directory.Exists(targetDir))
            {
                WriteLineOutput("Error: Target directory is either null or doesn't exist.");
                return;
            }

            try
            {
                // Delete files
                var filesToDelete = Directory.GetFiles(targetDir, "*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                    .Where(file =>
                        new FileInfo(file).Length == 0 ||
                        (fileMatches != null && fileMatches.Any(m => Regex.IsMatch(Path.GetFileName(file), m))))
                    .ToList();

                int deletedItemCount = 0;

                if (safe && filesToDelete.Count > 0)
                {
                    var filesToShow = filesToDelete.Take(safeLimit);
                    WriteLineOutput("Files to be deleted:");
                    foreach (var file in filesToShow)
                    {
                        WriteLineOutput(file);
                    }

                    WriteLineOutput("Continue to delete these files? (Y/N): ");
                    if (Console.ReadLine()?.ToLower() != "y")
                    {
                        return;
                    }

                    deletedItemCount += filesToShow.Count();
                }

                foreach (var file in filesToDelete)
                {
                    if (verbose) WriteLineOutput($"Deleting file: {file}");

                    if (!string.IsNullOrEmpty(backup))
                    {
                        var backupPath = Path.Combine(backup, Path.GetRelativePath(targetDir, file));
                        File.Copy(file, backupPath, true);
                    }

                    File.Delete(file);
                    if (!string.IsNullOrEmpty(logfile))
                    {
                        File.AppendAllText(logfile, $"Deleted File: {file}" + Environment.NewLine);
                    }
                }

                // Delete directories
                var dirsToDelete = Directory.GetDirectories(targetDir, "*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                    .Where(dir => dirMatches != null && dirMatches.Any(m => Regex.IsMatch(Path.GetFileName(dir), m)))
                    .ToList();

                if (safe && dirsToDelete.Count > 0)
                {
                    var dirsToShow = dirsToDelete.Take(safeLimit - deletedItemCount);
                    WriteLineOutput("Directories to be deleted (including all files and child folders):");
                    foreach (var dir in dirsToShow)
                    {
                        WriteLineOutput(dir);
                    }

                    WriteLineOutput("Continue to delete these directories? (Y/N): ");
                    if (Console.ReadLine()?.ToLower() != "y")
                    {
                        return;
                    }
                }

                foreach (var dir in dirsToDelete)
                {
                    if (verbose) WriteLineOutput($"Deleting directory (including all files and child folders): {dir}");

                    Directory.Delete(dir, true);
                    if (!string.IsNullOrEmpty(logfile))
                    {
                        File.AppendAllText(logfile, $"Deleted Directory: {dir}" + Environment.NewLine);
                    }
                }
            }
            catch (Exception e)
            {
                WriteLineOutput($"An error occurred: {e.Message}");
            }
        }

        static void WriteLineOutput(string? message = null, string? logfile = null)
        {

            Console.WriteLine(message);
            if (string.IsNullOrEmpty(logfile))
            {
                logfile = defaultLogfile;
            }
            if (!string.IsNullOrEmpty(logfile))
            {
                File.AppendAllText(logfile, message + Environment.NewLine);
            }

        }

        static void WriteOutput(string? message = null, string? logfile = null)
        {
            Console.Write(message);
            if (string.IsNullOrEmpty(logfile))
            {
                logfile = defaultLogfile;
            }
            if (!string.IsNullOrEmpty(logfile))
            {
                File.AppendAllText(logfile, message);
            }
        }


    }
}


