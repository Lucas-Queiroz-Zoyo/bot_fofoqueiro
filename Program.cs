using bot_fofoqueiro;
using Newtonsoft.Json;
using System.Net.Http.Json;
using System.Text;

var stopwatch = System.Diagnostics.Stopwatch.StartNew();
var formContent = new FormUrlEncodedContent(new[]
{
    new KeyValuePair<string, string>("token", "xoxc-2651622568611-3634713226258-7805078621094-19b941820d3e750827f144b5ad0ea995e657bda665e7938f9487d7994abbcc32"),
    new KeyValuePair<string, string>("include_user_counts", "true"),
    new KeyValuePair<string, string>("team", "T0ZHLME6P"),
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
httpRequest.DefaultRequestHeaders.Add("cookie", "ssb_instance_id=0bfddbc7-8f1d-435b-b65b-5185e1c64532; d-s=1727791977; b=.2a15da562b9db5f595c568efa54f1368; tz=-180; shown_ssb_redirect_page=1; OptanonAlertBoxClosed=2024-10-16T11:57:52.832Z; utm=%7B%7D; _gcl_au=1.1.1152462969.1733242978; PageCount=1; _ga_QR4NFYRYGP=GS1.1.1733242978.1.0.1733242978.0.0.0; _ga=GA1.1.1216082973.1733242978; cjConsent=MHxOfDB8Tnww; cjUser=523abb92-bb60-4504-a34b-f21b5b8f8d2d; _cs_c=0; _cs_id=c809829f-fa3d-afdc-c296-79ae94c99771.1733242979.1.1733242979.1733242979.1.1767406979928.1; _ga_QTJQME5M5D=GS1.1.1733242978.1.1.1733243394.60.0.0; x=2a15da562b9db5f595c568efa54f1368.1733501511; OptanonConsent=isGpcEnabled=0&datestamp=Fri+Dec+06+2024+13%3A11%3A53+GMT-0300+(GMT-03%3A00)&version=202402.1.0&browserGpcFlag=0&isIABGlobal=false&hosts=&consentId=4e1e0d18-d74c-4f51-a65d-ef7ea3b2333c&interactionCount=1&isAnonUser=1&landingPath=NotLandingPage&groups=1%3A1%2C2%3A1%2C3%3A1%2C4%3A1&AwaitingReconsent=false&geolocation=BR%3BSP; d=xoxd-lOrMEYt%2F0B%2BOyMRYNgPXhOH9ntAX5hazbmsKVGmPY0ZRC2rEytdwSnCzQkiiaDVlhxJgousuruPZ8NHPmoAi2ldPlPuVZsfA08FhY0egTvn96yJ65alwBlVYLW%2FgQyXECqsxA9w4%2BruE7iGwbyrSEhMQ%2B6KF0ud1pJz%2F9dw09TNhunEVlDhvSQ%3D%3D");
httpRequest.DefaultRequestHeaders.Add("Authorization", "Bearer xoxc-2651622568611-3634713226258-7805078621094-19b941820d3e750827f144b5ad0ea995e657bda665e7938f9487d7994abbcc32");
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


// PRIVADO
_ = await httpRequest.PostAsync("https://hooks.slack.com/services/", content);
Console.WriteLine($"Tempo total de execução do envio de mensagem no slack (PV): {stopwatch.Elapsed.TotalSeconds} s");
stopwatch.Restart();

Console.ReadLine();

// DEPLOY 
// dotnet publish .\bot_fofoqueiro.csproj -o .\versao_estavel\