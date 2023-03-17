using System.Text.RegularExpressions;
using LibGit2Sharp;

namespace GitWordSearch;

public static class Program
{
    private static void Main()
    {
        const string repoPath = "D:\\__PROG__\\LaMUIette";
        const string searchText = "partial";

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
    }
}