//
// CleanPath - Automated File Cleanup Tool
//
// SYNOPSIS
//   This console application recursively deletes zero-byte files and files matching user-defined regular expressions in a given directory.

// DESCRIPTION
//    The application accepts several optional command-line parameters to specify the directory, matching files, and other behaviors.
//    It traverses the given directory recursively (if specified) and deletes zero-byte files and files that match the user-defined regex.
//    The application supports "safe mode" that prompts the user for confirmation before actual deletion.
//    It also provides an option to back up files before deletion and to log the deleted file paths.

//  OPTIONS
//              -d --target-dir      The target directory to clean. Defaults to the current directory if not specified.
//              -m --matches         Comma-separated list of regular expressions to match files for deletion.
//              -dm --dir-matches    Comma-separated list of regular expressions to match directories for deletion.
//              -R -r                Enable recursive deletion in subdirectories.
//              -s --safe            Enable safe mode, which prompts the user before deletion.
//              -sl --safe-limit     The limit for the number of files shown in safe mode before deletion. Default is 15.
//              -l --logfile         Specify a log file to which deleted file paths will be written.
//              -b --backup          Specify a backup folder where files will be copied before deletion.
//              -v, --verbose        Enable verbose output.
//              -h --help            Show this help message and exit.
//
// NOTES
//    1. The application will skip directories it does not have permission to access, such as 'System Volume Information'.
//    2. Be cautious when specifying the backup directory to ensure it does not conflict or overlap with the target directory.
//    3. If in safe mode, the application will prompt the user for confirmation for the first nth files determined by 'safe-limit'.
//


using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using static System.Net.WebRequestMethods;

namespace CleanPath
{
    class Program
    {
        static string? logfile = null, defaultLogfile = null;
        static bool verbose = false;
        static int Main(string[] args)
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

            if (args != null)
            {
                if (args.Length > 0)
                {
                    for (int i = 0; i < args.Length; i++)
                    {
                        switch (args[i])
                        {
                            case "--target-dir":
                            case "-d":
                                targetDir = args[++i];
                                if (verbose) WriteLineOutput($" - Target directory: {targetDir}");
                                break;
                            case "--matches":
                            case "-m":
                                matches = args[++i].Split(',');
                                if (verbose) WriteLineOutput($" - File matches: {string.Join(", ", matches)}");
                                break;
                            case "--dir-matches":
                            case "-dm":
                                dirMatches = args[++i].Split(',');
                                if (verbose) WriteLineOutput($" - Directory matches: {string.Join(", ", dirMatches)}");
                                break;
                            case "-R":
                            case "-r":
                                R = true;
                                if (verbose) WriteLineOutput(" - Recursive");
                                break;
                            case "--safe":
                            case "-s":
                                safe = true;
                                if (verbose) WriteLineOutput(" - Safe mode");
                                break;
                            case "--safe-limit":
                            case "-sl":
                                safeLimit = int.Parse(args[++i]);
                                if (verbose) WriteLineOutput($" - Safe limit: {safeLimit}");
                                break;
                            case "--logfile":
                            case "-l":
                                logfile = args[++i];
                                if (verbose) WriteLineOutput($" - Logfile: {logfile}");
                                break;
                            case "--backup":
                            case "-b":
                                backup = args[++i];
                                if (verbose) WriteLineOutput($" - Backup: {backup}");
                                break;
                            case "-v":
                            case "--verbose":
                                verbose = true;
                                break;
                            case "-h":
                            case "--help":
                                help = true;
                                break;
                            default:
                                WriteLineOutput($"Unknown option: {args[i]}");
                                return 1;
                        }
                    }
                }
                else
                {
                    WriteLineOutput("Error: No arguments provided.");
                    WriteLineOutput("Choose '-h' or '--help' for instructions on use.");
                    return 1;
                }

                if (safe && verbose)
                {
                    WriteLineOutput($" - Safe deletion for the first {safeLimit} files");
                    WriteLineOutput($"   (user asked for permission to proceed with ANY deletions)");
                }
            }
            else
            {
                WriteLineOutput("Error: No arguments provided.");
                WriteLineOutput("Choose '-h' or '--help' for instructions on use.");
                return 1;
            }

            if (verbose)
            {
                WriteLineOutput();
            }

            // Show help if requested
            if (help || args[0] == "--help")
            {
                WriteLineOutput();
                ShowHelp();
                return 0;
            }

            // Main Execution
            string rootPath = string.IsNullOrEmpty(targetDir) ? Directory.GetCurrentDirectory() : targetDir;

            if (!Directory.Exists(rootPath))
            {
                WriteLineOutput("Error: The target directory does not exist.");
                return 1;
            }

            // Check for backup folder conflict
            if (!string.IsNullOrEmpty(backup))
            {
                string rootPathStr = Path.GetFullPath(rootPath);
                string backupPathStr = Path.GetFullPath(backup);
                if (rootPathStr == backupPathStr || backupPathStr.StartsWith(rootPathStr + Path.DirectorySeparatorChar))
                {
                    WriteLineOutput("Error: The backup folder path conflicts with the folder being cleaned.");
                    return 1;
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
            return 0;
        }

        static void ShowHelp()
        {
            WriteLineOutput("Usage: cleanpath [OPTIONS]");
            WriteLineOutput();
            WriteOutput(@"Recursively deletes zero-byte files, as well as file and directories matching user-defined regular expressions in a given directory. ");
            WriteLineOutput(@"Intended to clean directory structures that may contain zero-length files or other unwanted files and folders, possibly due to synchronization failures.");
            WriteLineOutput();
            WriteLineOutput("Options:");
            WriteLineOutput("  -d --target-dir      The target directory to clean. Defaults to the current directory if not specified.");
            WriteLineOutput("  -m --matches         Comma-separated list of regular expressions to match files for deletion.");
            WriteLineOutput("  -dm --dir-matches    Comma-separated list of regular expressions to match directories for deletion.");
            WriteLineOutput("  -R -r                Enable recursive deletion in subdirectories.");
            WriteLineOutput("  -s --safe            Enable safe mode, which prompts the user before deletion.");
            WriteLineOutput("  -sl --safe-limit     The limit for the number of files shown in safe mode before deletion. Default is 15.");
            WriteLineOutput("  -l --logfile         Specify a log file to which deleted file paths will be written.");
            WriteLineOutput("  -b --backup          Specify a backup folder where files will be copied before deletion.");
            WriteLineOutput("  -v, --verbose        Enable verbose output.");
            WriteLineOutput("  -h --help            Show this help message and exit.");
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

        static void DeleteFilesAndFolders(string? targetDir, string[]? fileMatches, string[]? dirMatches, bool recursive, bool safe, int safeLimit, string? logfile, string? backup)
        {
            if (string.IsNullOrEmpty(targetDir) || !Directory.Exists(targetDir))
            {
                WriteLineOutput("Error: Target directory is either null or doesn't exist.");
                return;
            }
            if (verbose)
            {
                ExamineDirectory(targetDir);
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
                        System.IO.File.Copy(file, backupPath, true);
                    }

                    System.IO.File.Delete(file);
                    if (!string.IsNullOrEmpty(logfile))
                    {
                        System.IO.File.AppendAllText(logfile, $"Deleted File: {file}" + Environment.NewLine);
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
                        System.IO.File.AppendAllText(logfile, $"Deleted Directory: {dir}" + Environment.NewLine);
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
                System.IO.File.AppendAllText(logfile, message + Environment.NewLine);
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
                System.IO.File.AppendAllText(logfile, message);
            }
        }


    }
}


