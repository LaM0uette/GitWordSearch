using System.Diagnostics;
using System.Text.RegularExpressions;
using LibGit2Sharp;

namespace GitWordSearch;

public static class Program
{
    private static void Main()
    {
        const string repoPath = "D:\\__PROG__\\LaMUIette";
        const string searchText = "XD5965";
        const string branchName = "master";

        using var repo = new Repository(repoPath);
        
        var regex = new Regex(Regex.Escape(searchText));

        foreach (var commit in repo.Commits)
        {
            foreach (var parent in commit.Parents)
            {
                var changes = repo.Diff.Compare<TreeChanges>(parent.Tree, commit.Tree);

                foreach (var change in changes)
                {
                    if (change.Status != ChangeKind.Added && change.Status != ChangeKind.Modified) continue;
                    
                    try
                    {
                        var blob = (Blob)commit[change.Path].Target;
                        
                        using var contentStream = blob.GetContentStream();
                        using var streamReader = new StreamReader(contentStream);
                        
                        var fileContent = streamReader.ReadToEnd();
                        
                        if (regex.IsMatch(fileContent))
                        {
                            Console.WriteLine($"Commit {commit.Sha} : {change.Path}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing file: {change.Path}\n{ex.Message}");
                    }
                }
            }
        }
        
        Console.WriteLine("Do you want to remove lines containing the search text from all files and all commits? [y/N]");
        var response = Console.ReadLine()?.ToLower();

        if (response != "y") return;
        
        ExecuteCommand("git", $"-C \"{repoPath}\" filter-branch -f --tree-filter \"find . -type f ! -path './.git/*' -exec sed -i '/{searchText}/d' {{}} +\" -- --all");
        ExecuteCommand("git", $"-C \"{repoPath}\" push --force origin {branchName}");
    }

    private static void ExecuteCommand(string command, string arguments)
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = command,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = processInfo };
        
        process.OutputDataReceived += (_, e) => Console.WriteLine(e.Data);
        process.ErrorDataReceived += (_, e) => Console.WriteLine(e.Data);

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();
    }
}