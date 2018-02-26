using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace DataLoader
{
    public static class Uploader
    {
        public static async Task Upload(string data)
        {
            File.WriteAllText("scheduleData", data);
            // return;

            var auth = new HttpClientHandler
            {
                Credentials = new NetworkCredential
                {
                    UserName = RepoConstants.Username,
                    Password = RepoConstants.Password
                }
            };
            var client = new HttpClient(auth);

            /*var beforeRedirect = await client.GetAsync(baseUrl + "/src");
            beforeRedirect.EnsureSuccessStatusCode();

            var baseSrc = beforeRedirect.RequestMessage.RequestUri.ToString();
            var prevDataResponse = await client.GetAsync(
                $"{baseSrc}/{dataFilename}"
            );*/
            var prevDataResponse =
                await client.GetAsync(
                    $"https://bitbucket.org/{RepoConstants.RepoName}/raw/HEAD/{RepoConstants.DataFilename}");

            var prevData = "";
            if (prevDataResponse.IsSuccessStatusCode) // If file found
                prevData = await prevDataResponse.Content.ReadAsStringAsync();

            if (Parser.LoadTables(data).Equals(Parser.LoadTables(prevData)))
                return;

            var result = await client.PostAsync(
                $"{RepoConstants.BaseUrl}/src?files={RepoConstants.DataFilename}&message={RepoConstants.CommitMessage}",
                new MultipartFormDataContent
                {
                    {new StringContent(data), RepoConstants.DataFilename, RepoConstants.DataFilename}
                }
            );
            var response = await result.Content.ReadAsStringAsync(); // For debugging
            result.EnsureSuccessStatusCode();
        }
    }
}