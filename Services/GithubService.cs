using System.Diagnostics;
using System.Net.Http.Headers;
using Octokit;

namespace NipponQuest.Services
{
    public class GithubService
    {
        private readonly GitHubClient _client;

        public GithubService()
        {
            // Explicitly using Octokit namespace to resolve CS0104 ambiguity
            _client = new GitHubClient(new Octokit.ProductHeaderValue("NipponQuest-App-v2"));
        }

        public async Task<List<GitHubCommit>> GetLatestCommitsAsync(string owner, string repo)
        {
            try
            {
                // We only want the most recent 5 quests (commits)
                var request = new ApiOptions
                {
                    PageCount = 1,
                    PageSize = 5,
                    StartPage = 1
                };

                // Fetching as a ReadOnlyList
                var commits = await _client.Repository.Commit.GetAll(owner, repo, request);

                // Convert to a standard List to prevent casting errors in the View
                return commits.ToList();
            }
            catch (ApiException ex)
            {
                // Octokit specific exception: Log the status code (e.g., 404 if repo not found)
                Debug.WriteLine($"[GITHUB ERROR]: {ex.StatusCode} - {ex.Message}");
                return new List<GitHubCommit>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CRITICAL GITHUB SERVICE ERROR]: {ex.Message}");
                return new List<GitHubCommit>();
            }
        }
    }
}