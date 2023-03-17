using System.Text.RegularExpressions;
using LibGit2Sharp;

namespace GitWordSearch;

public static class Program
{
    static void Main(string[] args)
    {
        if (args.Length != 2)
        {
            Console.WriteLine("Usage: GitSearchCommits <repository_path> <search_text>");
            return;
        }

        string repoPath = args[0];
        string searchText = args[1];

        using (var repo = new Repository(repoPath))
        {
            var regex = new Regex(Regex.Escape(searchText));

            foreach (var commit in repo.Commits)
            {
                foreach (var parent in commit.Parents)
                {
                    var changes = repo.Diff.Compare<TreeChanges>(parent.Tree, commit.Tree);

                    foreach (var change in changes)
                    {
                        if (change.Status == ChangeKind.Added || change.Status == ChangeKind.Modified)
                        {
                            try
                            {
                                var blob = (Blob)commit[change.Path].Target;
                                using (var contentStream = blob.GetContentStream())
                                using (var streamReader = new StreamReader(contentStream))
                                {
                                    string fileContent = streamReader.ReadToEnd();
                                    if (regex.IsMatch(fileContent))
                                    {
                                        Console.WriteLine($"Commit {commit.Sha} : {change.Path}");
                                    }
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
    }
}