using bot_fofoqueiro;
using Newtonsoft.Json;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.Configuration;

// Configuração
var builder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true)
    .AddUserSecrets<Program>();

var configuration = builder.Build();
var config = new SlackBotConfiguration();
configuration.GetSection("SlackBotConfiguration").Bind(config);

var stopwatch = System.Diagnostics.Stopwatch.StartNew();
var formContent = new FormUrlEncodedContent(new[]
{
    new KeyValuePair<string, string>("token", config.SlackToken),
    new KeyValuePair<string, string>("include_user_counts", "true"),
    new KeyValuePair<string, string>("team", config.SlackTeamId),
});
var currentDateProcess = DateTime.Now.Date;
var baseFilePath = $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}/SlackUserList/lastUserInfo_[DATE].txt";

var lastUserInfoFilePath = string.Empty;
var newUserInfoFilePath = baseFilePath.Replace("[DATE]", $"{currentDateProcess.Date:dd_MM_yyyy}");

foreach (var backDays in Enumerable.Range(1, 365))
{
    lastUserInfoFilePath = baseFilePath.Replace("[DATE]", $"{currentDateProcess.AddDays(-backDays).Date:dd_MM_yyyy}");

    if (File.Exists(lastUserInfoFilePath))
    {
        break;
    }

    lastUserInfoFilePath = string.Empty;
}

Console.WriteLine($"Tempo total de execução da busca do ultimo arquivo: {stopwatch.Elapsed.TotalSeconds} s");
stopwatch.Restart();

if (string.IsNullOrEmpty(lastUserInfoFilePath))
{
    lastUserInfoFilePath = baseFilePath.Replace("[DATE]", $"{currentDateProcess.AddDays(-1).Date:dd_MM_yyyy}");
    File.AppendAllText(lastUserInfoFilePath, string.Empty);
}


var lastUserInfo = File.ReadAllLines(lastUserInfoFilePath);
Console.WriteLine($"Tempo total de execução da leitura do ultimo arquivo: {stopwatch.Elapsed.TotalSeconds} s");
stopwatch.Restart();
var lastUserInfoList = lastUserInfo.Select(x => JsonConvert.DeserializeObject<Member>(x));
Console.WriteLine("Total de linhas do arquivo -> {0}", lastUserInfo.Length);
Console.WriteLine($"Tempo total de execução da serialização dos registros do ultimo arquivo: {stopwatch.Elapsed.TotalSeconds} s");
stopwatch.Restart();


Console.WriteLine("Executando requisição para obter informações de usuários.");
async Task<HttpResponseMessage> SendWithRetryAsync(Func<Task<HttpResponseMessage>> httpCall, string callName, int maxAttempts = 3)
{
    int attempt = 0;
    HttpResponseMessage response = null;
    while (attempt < maxAttempts)
    {
        attempt++;
        try
        {
            response = await httpCall();
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"{callName}: Sucesso na Tentativa {attempt}.");
                return response;
            }
            else
            {
                Console.WriteLine($"{callName}: Falha na Tentativa {attempt} (Status: {response.StatusCode}).");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{callName}: Exceção na Tentativa {attempt}: {ex.Message}");
        }
        await Task.Delay(1000); // Aguarda 1 segundo antes de tentar novamente
    }
    Console.WriteLine($"{callName}: Todas as {maxAttempts} tentativas falharam.");
    return response;
}

var httpRequest = new HttpClient();
httpRequest.DefaultRequestHeaders.Add("cookie", config.SlackCookie);
httpRequest.DefaultRequestHeaders.Add("Authorization", $"Bearer {config.SlackToken}");
var neonTeamInfoResponse = await SendWithRetryAsync(
    () => httpRequest.PostAsync("https://neon.enterprise.slack.com/api/enterprise.teams.info?_x_id=badc0ea1-1733501648.552&slack_route=E02K5JAGQHZ%3AT02K5JAGQHZ&_x_version_ts=noversion&fp=03&_x_num_retries=0", formContent),
    "neon.enterprise.slack.com/api",
    10
);
Console.WriteLine($"Tempo total de execução da consulta a API 'neon.enterprise.slack.com/api': {stopwatch.Elapsed.TotalSeconds} s");
stopwatch.Restart();

var neonUserListResponse = await SendWithRetryAsync(
    () => httpRequest.GetAsync("https://slack.com/api/users.list"),
    "slack.com/api/users.list",
    10
);
Console.WriteLine($"Tempo total de execução da consulta a API 'slack.com/api/users.list': {stopwatch.Elapsed.TotalSeconds} s");
stopwatch.Restart();


var neonTeamInfoResponseStringContent = await neonTeamInfoResponse.Content.ReadFromJsonAsync<SlackTeamInfoResponse>();
var neonUserListResponseStringContent = await neonUserListResponse.Content.ReadFromJsonAsync<SlackUserInfoResponse>();
neonUserListResponseStringContent.Members = neonUserListResponseStringContent.Members.Where(x => !x.Is_bot).OrderBy(x => x.Profile.Real_name).ToArray();
Console.WriteLine("Requisição para obter informações de usuários executada.");
Console.WriteLine("Total de usuários ativos V1 [{0}] / V2 [{1}]", neonTeamInfoResponseStringContent.team.user_counts.active_members, neonUserListResponseStringContent.Members.Where(x => x.Deleted == false).Count());

var newUsers = new List<Member>();
var deletedUsers = new List<Member>();

var lastUserInfoDict = lastUserInfoList.ToDictionary(x => x.Id);

foreach (var user in neonUserListResponseStringContent.Members)
{
    lastUserInfoDict.TryGetValue(user.Id, out var lastUser);

    if (lastUser is null)
    {
        newUsers.Add(user);
    }
    else if (!lastUser.Deleted && user.Deleted)
    {
        deletedUsers.Add(user);
    }
}
Console.WriteLine($"Tempo total de execução da validação: {stopwatch.Elapsed.TotalSeconds} s");
stopwatch.Restart();

File.WriteAllLines(newUserInfoFilePath, neonUserListResponseStringContent.Members.Select(x => JsonConvert.SerializeObject(x)));
Console.WriteLine($"Tempo total de execução da escrita: {stopwatch.Elapsed.TotalSeconds} s");

var message = $@"{{
    ""blocks"": [
        {{
            ""type"": ""header"",
            ""text"": {{
                ""type"": ""plain_text"",
                ""text"": "":warning: BOT FOFOQUEIRO :warning:"",
                ""emoji"": true
            }}
        }},
        {{
            ""type"": ""section"",
            ""text"": {{
                ""type"": ""mrkdwn"",
                ""text"": ""*TOTAL DE USUÁRIOS ATIVOS [V1]* - {neonTeamInfoResponseStringContent.team.user_counts.active_members} MEMBROS""
            }}
        }},
        {{
            ""type"": ""section"",
            ""text"": {{
                ""type"": ""mrkdwn"",
                ""text"": ""*TOTAL DE USUÁRIOS ATIVOS [V2]* - {neonUserListResponseStringContent.Members.Where(x => x.Deleted == false).Count()} MEMBROS""
            }}
        }},
        {{
            ""type"": ""section"",
            ""text"": {{
                ""type"": ""mrkdwn"",
                ""text"": ""*NOVOS USUÁRIOS* - [{newUsers.Count}] ||{string.Join("||", newUsers.Select(x => $"<@{x.Id}> {x.Profile.Real_name}"))}""
            }}
        }},
        {{
            ""type"": ""section"",
            ""text"": {{
                ""type"": ""mrkdwn"",
                ""text"": ""*USUÁRIOS REMOVIDOS* - [{deletedUsers.Count}] ||{string.Join("||", deletedUsers.Select(x => $"<@{x.Id}> {x.Profile.Real_name} {(x.Profile.Start_date != DateTime.MinValue ? $"({(currentDateProcess - x.Profile.Start_date).TotalDays} dias)" : "")}"))}""
            }}
        }}
    ]
}}";

message = message.Replace("||", "\n");
httpRequest = new HttpClient();
var content = new StringContent(message, Encoding.UTF8, "application/json");

// CANAL CHORO
_ = await httpRequest.PostAsync(config.Webhooks.ChoroChannel, content);
Console.WriteLine($"Tempo total de execução do envio de mensagem no slack (CHORO): {stopwatch.Elapsed.TotalSeconds} s");
stopwatch.Restart();

// PRIVADO
_ = await httpRequest.PostAsync(config.Webhooks.PrivateChannel, content);
Console.WriteLine($"Tempo total de execução do envio de mensagem no slack (PV): {stopwatch.Elapsed.TotalSeconds} s");
stopwatch.Restart();

Console.ReadLine();

// DEPLOY 
// dotnet publish .\bot_fofoqueiro.csproj -o .\versao_estavel\