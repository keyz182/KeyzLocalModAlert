using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace KeyzLocalModAlert;

public class GitOps
{
    public static string GitError = null;
    public static bool GitCheckDone = false;
    public static bool GitAvailable = false;

    public static bool IsGitAvailable(out string errorMessage)
    {
        if (GitCheckDone)
        {
            errorMessage = GitError;
            return GitAvailable;
        }
        GitCheckDone = true;

        try
        {
            Process process = new()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();

            if (process.ExitCode == 0)
            {
                errorMessage = string.Empty;
                GitAvailable = true;
                return true;
            }

            string error = process.StandardError.ReadToEnd().Trim();
            errorMessage = $"Git command error: {error}";
            ModLog.Error(errorMessage);
            return false;
        }
        catch (Exception e)
        {
            errorMessage = $"Failed to check Git availability: {e.Message}";
            GitError = errorMessage;
            ModLog.Error("Exception thrown while checking Git availability", e);
            return false;
        }
    }
    public static bool CheckGit(DirectoryInfo directoryPath, out string errorMessage, out string versionDetails, out string currentBranch, out bool outOfSync)
    {
        try
        {
            errorMessage = string.Empty;
            versionDetails = string.Empty;
            currentBranch = string.Empty;
            outOfSync = false;

            // Validate the directory
            if (!directoryPath.Exists)
            {
                errorMessage = "Directory not found";
                return false;
            }

            // Check if the directory is a Git repository
            if (!IsGitRepository(directoryPath))
            {
                errorMessage = "Not a git repository";
                return false;
            }

            // Get the current branch
            currentBranch = GetCurrentBranch(directoryPath);

            if (string.IsNullOrEmpty(currentBranch))
            {
                errorMessage = "Cannot determine branch";
                return false;
            }

            // Check if the current branch is behind/ahead/up-to-date with the remote
            (int Ahead, int Behind)? syncStatus = CheckSyncStatus(directoryPath, currentBranch);

            if (syncStatus == null)
            {
                errorMessage = "Not tracking a remote branch";
                return false;
            }

            StringBuilder sb = new();
            sb.Append("Ahead: ");
            if (syncStatus.Value.Ahead != 0)
            {
                sb.Append("<color=green>");
                sb.Append(syncStatus.Value.Ahead);
                sb.Append("</color>");
                outOfSync = true;
            }
            else
            {
                sb.Append(syncStatus.Value.Ahead);
            }

            sb.Append(" | Behind: ");
            if (syncStatus.Value.Behind != 0)
            {
                sb.Append("<color=red>");
                sb.Append(syncStatus.Value.Behind);
                sb.Append("</color>");
                outOfSync = true;
            }
            else
            {
                sb.Append(syncStatus.Value.Behind);
            }

            versionDetails = sb.ToString();
            return true;
        }
        catch (Exception e)
        {
            outOfSync = false;
            versionDetails = string.Empty;
            currentBranch = string.Empty;
            errorMessage = "See Logs for error";
            ModLog.Error("Error checking git", e);
            return false;
        }
    }

    private static bool IsGitRepository(DirectoryInfo directoryPath)
    {
        return RunGitCommand(directoryPath, "rev-parse --is-inside-work-tree").Trim() == "true";
    }

    private static string GetCurrentBranch(DirectoryInfo directoryPath)
    {
        return RunGitCommand(directoryPath, "rev-parse --abbrev-ref HEAD").Trim();
    }

    private static (int Ahead, int Behind)? CheckSyncStatus(DirectoryInfo directoryPath, string currentBranch)
    {
        string status = RunGitCommand(directoryPath, $"rev-list --left-right --count origin/{currentBranch}...{currentBranch}");

        if (string.IsNullOrEmpty(status))
        {
            return null; // Branch isn't tracking a remote
        }

        string[] parts = status.Split('\t');
        if (parts.Length == 2 &&
            int.TryParse(parts[0], out int behind) &&
            int.TryParse(parts[1], out int ahead))
        {
            return (ahead, behind);
        }

        return null; // Unable to parse the output
    }

    private static string RunGitCommand(DirectoryInfo workingDirectory, string arguments)
    {
        Process process = new()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = arguments,
                WorkingDirectory = workingDirectory.ToString(),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        string output = process.StandardOutput.ReadToEnd().Trim();
        process.WaitForExit();

        if (process.ExitCode == 0)
        {
            return output;
        }

        string error = process.StandardError.ReadToEnd().Trim();
        ModLog.Error($"Git command error: {error}");

        return output;
    }
}
