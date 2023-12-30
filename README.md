# CleanPath

## Synopsis
CleanPath is a Windows console application that recursively deletes zero-byte files and files matching user-defined regular expressions in a specified directory.

## Description
CleanPath accepts various command-line parameters to specify directories, matching file patterns, and other behaviors. It can traverse directories recursively and delete files that are zero-byte or match user-defined regex patterns. The application features a "safe mode" for confirmation before deletion, options for backup before deletion, and logging capabilities.

## Parameters

- `--target-dir`: Specifies the directory to clean. Defaults to the current directory if not specified.
- `--matches`: Specifies regex patterns for file types to be deleted. Patterns should be separated by a comma.
- `-R`: Enables recursive deletion in subdirectories.
- `--safe`: Enables safe mode, prompting the user for confirmation before each deletion.
- `--safe-limit`: Specifies the number of files to show in safe mode before asking for confirmation. Default is 15.
- `--logfile`: Specifies the path of the log file to record deleted file paths.
- `--backup`: Specifies the directory to back up files before deletion.
- `--verbose`: Outputs additional information about the operations being performed.
- `--dir-matches`: Specifies regex patterns for directory names to be excluded. Patterns should be separated by a comma.

## Examples

### Example 1: Delete zero-byte and .txt or .log files in `C:\Folder1` and its subdirectories:

``` powershell
cleanpath --target-dir "C:\Folder1" --matches ".txt|.log" -R
```

### Example 2: Deleting Specific File Types Without Recursion

``` powershell
cleanpath --target-dir "D:\Documents" --matches ".bak|.tmp"
```

This command will delete files ending with `.bak` or `.tmp` in `D:\Documents` but will not check subdirectories.

### Example 3: Using Verbose Mode and Logging

``` powershell
cleanpath --target-dir "C:\Users\JohnDoe\Downloads" -R --matches ".old" --verbose --logfile "C:\Logs\CleanPath.log"
```

This command recursively deletes `.old` files in `C:\Users\JohnDoe\Downloads`, outputs detailed information about the process, and logs the deleted file paths to `C:\Logs\CleanPath.log`.

### Example 4: Safe Mode with Directory Exclusion

``` powershell
cleanpath --target-dir "C:\Projects" -R --safe --dir-matches "Archive|Temp"
```

This command prompts for user confirmation before deleting zero-byte files in `C:\Projects` and its subdirectories, excluding any directories named `Archive` or `Temp`.

## Notes

- Be cautious with regex patterns to avoid unintended deletions.
- Directories without access permissions, like 'System Volume Information', will be skipped.
- Ensure the backup directory does not conflict with the target directory.
- In safe mode, confirmation is required before deletions proceed.

---

