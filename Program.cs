using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text;

namespace bot_fofoqueiro
{
    public class Program
    {
        private static readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private static SlackBotConfiguration _config = null!;
        private static HttpClient _httpClient = null!;

        // Constantes
        private const int MAX_RETRY_ATTEMPTS = 10;
        private const int RETRY_DELAY_MS = 1000;
        private const int SEARCH_DAYS_BACK = 365;
        private const string ENTERPRISE_API_URL = "https://neon.enterprise.slack.com/api/enterprise.teams.info?_x_id=badc0ea1-1733501648.552&slack_route=E02K5JAGQHZ%3AT02K5JAGQHZ&_x_version_ts=noversion&fp=03&_x_num_retries=0";
        private const string USERS_LIST_API_URL = "https://slack.com/api/users.list";

        public static async Task Main(string[] args)
        {
            try
            {
                // Configurar encoding para exibir emojis corretamente no Windows
                Console.OutputEncoding = System.Text.Encoding.UTF8;

                Console.WriteLine("🤖 BOT FOFOQUEIRO INICIADO");

                // 1. Configuração inicial
                _config = LoadConfiguration();
                _httpClient = CreateHttpClient();

                // 2. Gerenciar arquivos de histórico
                var currentDate = DateTime.Now.Date;
                var (lastUserInfoFile, newUserInfoFile, hasHistory) = ManageUserInfoFiles(currentDate);

                // 3. Carregar dados anteriores
                var previousUsers = await LoadPreviousUserData(lastUserInfoFile);
                LogPerformance("Carregamento de dados anteriores");

                // 4. Buscar dados atuais do Slack
                var (teamInfo, currentUsers) = await FetchCurrentSlackData();
                LogPerformance("Busca de dados atuais do Slack");

                // 5. Processar e filtrar usuários
                var filteredUsers = ProcessCurrentUsers(currentUsers);

                // 6. Comparar dados e identificar mudanças
                var (newUsers, deletedUsers) = CompareUserData(previousUsers, filteredUsers);
                LogPerformance("Comparação de dados");

                // 7. Salvar dados atuais
                await SaveCurrentUserData(newUserInfoFile, filteredUsers);
                LogPerformance("Salvamento de dados");

                // 8. Gerar e enviar relatório (apenas se houver histórico)
                if (hasHistory)
                {
                    await SendSlackNotifications(teamInfo, filteredUsers, newUsers, deletedUsers, currentDate);
                    LogPerformance("Envio de notificações");
                }
                else
                {
                    Console.WriteLine("ℹ️ Primeira execução detectada - Notificações do Slack foram ignoradas");
                    Console.WriteLine($"💾 Dados iniciais salvos: {filteredUsers.Length} usuários");
                    LogPerformance("Primeira execução - Dados salvos");
                }

                Console.WriteLine("✅ BOT FOFOQUEIRO FINALIZADO COM SUCESSO");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERRO CRÍTICO: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
            finally
            {
                _httpClient?.Dispose();
                Console.WriteLine("\nPressione Enter para sair...");
                Console.ReadLine();
            }
        }

        #region Configuration and Setup

        private static SlackBotConfiguration LoadConfiguration()
        {
            Console.WriteLine("📋 Carregando configurações...");

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddUserSecrets<Program>();

            var configuration = builder.Build();
            var config = new SlackBotConfiguration();
            configuration.GetSection("SlackBotConfiguration").Bind(config);

            LogPerformance("Carregamento de configurações");
            return config;
        }

        private static HttpClient CreateHttpClient()
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Cookie", _config.SlackCookie);
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.SlackToken}");
            return client;
        }

        #endregion

        #region File Management

        private static (string lastFile, string newFile, bool hasHistory) ManageUserInfoFiles(DateTime currentDate)
        {
            Console.WriteLine("📁 Gerenciando arquivos de histórico...");

            var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var baseFilePath = Path.Combine(desktopPath, "SlackUserList", "lastUserInfo_[DATE].txt");

            var newUserInfoFile = baseFilePath.Replace("[DATE]", currentDate.ToString("dd_MM_yyyy"));
            var (lastUserInfoFile, hasHistory) = FindLastUserInfoFile(baseFilePath, currentDate);

            LogPerformance("Busca do último arquivo");
            return (lastUserInfoFile, newUserInfoFile, hasHistory);
        }

        private static (string filePath, bool hasHistory) FindLastUserInfoFile(string baseFilePath,
                                                                                DateTime currentDate)
        {
            // Procurar arquivo dos últimos 365 dias
            for (int daysBack = 1; daysBack <= SEARCH_DAYS_BACK; daysBack++)
            {
                var searchDate = currentDate.AddDays(-daysBack);
                var filePath = baseFilePath.Replace("[DATE]", searchDate.ToString("dd_MM_yyyy"));

                if (File.Exists(filePath))
                {
                    Console.WriteLine($"📖 Arquivo anterior encontrado: {Path.GetFileName(filePath)}");
                    return (filePath, true);
                }
            }

            // Se não encontrou nenhum arquivo, criar um vazio para ontem
            var yesterdayFile = baseFilePath.Replace("[DATE]", currentDate.AddDays(-1).ToString("dd_MM_yyyy"));

            // Garantir que o diretório existe
            var directory = Path.GetDirectoryName(yesterdayFile);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory!);
            }

            File.WriteAllText(yesterdayFile, string.Empty);
            Console.WriteLine($"📄 Primeira execução detectada - Arquivo histórico criado: {Path.GetFileName(yesterdayFile)}");
            return (yesterdayFile, false);
        }

        #endregion

        #region Data Loading and Saving

        private static async Task<List<Member>> LoadPreviousUserData(string filePath)
        {
            Console.WriteLine("📚 Carregando dados de usuários anteriores...");

            if (!File.Exists(filePath))
            {
                return new List<Member>();
            }

            var lines = await File.ReadAllLinesAsync(filePath);
            Console.WriteLine($"📊 Total de registros anteriores: {lines.Length}");

            var users = new List<Member>();
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                try
                {
                    var user = JsonConvert.DeserializeObject<Member>(line);
                    if (user != null)
                    {
                        users.Add(user);
                    }
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"⚠️ Erro ao deserializar linha: {ex.Message}");
                }
            }

            LogPerformance("Deserialização de dados anteriores");
            return users;
        }

        private static async Task SaveCurrentUserData(string filePath,
                                                      Member[] users)
        {
            Console.WriteLine("💾 Salvando dados atuais...");

            // Garantir que o diretório existe
            var directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory!);
            }

            var lines = users.Select(user => JsonConvert.SerializeObject(user));
            await File.WriteAllLinesAsync(filePath, lines);

            Console.WriteLine($"💾 Dados salvos: {users.Length} usuários em {Path.GetFileName(filePath)}");
        }

        #endregion

        #region Slack API Integration

        private static async Task<(SlackTeamInfoResponse teamInfo, SlackUserInfoResponse userList)> FetchCurrentSlackData()
        {
            Console.WriteLine("🌐 Buscando dados atuais do Slack...");

            // Preparar formulário para API enterprise
            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("token", _config.SlackToken),
                new KeyValuePair<string, string>("include_user_counts", "true"),
                new KeyValuePair<string, string>("team", _config.SlackTeamId),
            });

            // Buscar informações da equipe
            var teamInfoResponse = await SendWithRetryAsync(
                () => _httpClient.PostAsync(ENTERPRISE_API_URL, formContent),
                "Enterprise Teams Info API",
                MAX_RETRY_ATTEMPTS
            );

            // Buscar lista de usuários
            var userListResponse = await SendWithRetryAsync(
                () => _httpClient.GetAsync(USERS_LIST_API_URL),
                "Users List API",
                MAX_RETRY_ATTEMPTS
            );

            // Deserializar respostas
            var teamInfo = await teamInfoResponse.Content.ReadFromJsonAsync<SlackTeamInfoResponse>();
            var userList = await userListResponse.Content.ReadFromJsonAsync<SlackUserInfoResponse>();

            if (teamInfo == null || userList == null)
            {
                throw new InvalidOperationException("Falha ao deserializar respostas da API do Slack");
            }

            Console.WriteLine("✅ Dados do Slack obtidos com sucesso");
            return (teamInfo, userList);
        }

        private static async Task<HttpResponseMessage> SendWithRetryAsync(Func<Task<HttpResponseMessage>> httpCall,
                                                                          string callName,
                                                                          int maxAttempts = 3)
        {
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    var response = await httpCall();
                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"✅ {callName}: Sucesso na tentativa {attempt}");
                        LogPerformance($"API Call - {callName}");
                        return response;
                    }

                    Console.WriteLine($"⚠️ {callName}: Falha na tentativa {attempt} (Status: {response.StatusCode})");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ {callName}: Exceção na tentativa {attempt}: {ex.Message}");
                }

                if (attempt < maxAttempts)
                {
                    Console.WriteLine($"⏳ Aguardando {RETRY_DELAY_MS}ms antes da próxima tentativa...");
                    await Task.Delay(RETRY_DELAY_MS);
                }
            }

            throw new InvalidOperationException($"Todas as {maxAttempts} tentativas falharam para {callName}");
        }

        #endregion

        #region Data Processing

        private static Member[] ProcessCurrentUsers(SlackUserInfoResponse userList)
        {
            Console.WriteLine("🤖 Processando usuários atuais...");

            var filteredUsers = userList.Members
                .Where(x => !x.Is_bot)
                .OrderBy(x => x.Profile.Real_name)
                .ToArray();

            Console.WriteLine($"👥 Usuários processados: {filteredUsers.Length} (bots removidos)");
            LogPerformance("Processamento de usuários");

            return filteredUsers;
        }

        private static (List<Member> newUsers, List<Member> deletedUsers) CompareUserData(List<Member> previousUsers,
                                                                                          Member[] currentUsers)
        {
            Console.WriteLine("🔍 Comparando dados de usuários...");

            var newUsers = new List<Member>();
            var deletedUsers = new List<Member>();

            var previousUsersDict = previousUsers.ToDictionary(x => x.Id);

            foreach (var currentUser in currentUsers)
            {
                if (!previousUsersDict.TryGetValue(currentUser.Id, out var previousUser))
                {
                    // Usuário novo
                    newUsers.Add(currentUser);
                }
                else if (!previousUser.Deleted && currentUser.Deleted)
                {
                    // Usuário foi removido
                    deletedUsers.Add(currentUser);
                }
            }

            var activeCount = currentUsers.Count(x => !x.Deleted);
            Console.WriteLine($"📊 Usuários ativos: {activeCount}");
            Console.WriteLine($"🆕 Novos usuários: {newUsers.Count}");
            Console.WriteLine($"❌ Usuários removidos: {deletedUsers.Count}");

            return (newUsers, deletedUsers);
        }

        #endregion

        #region Slack Notifications

        private static async Task SendSlackNotifications(SlackTeamInfoResponse teamInfo,
                                                         Member[] currentUsers,
                                                         List<Member> newUsers,
                                                         List<Member> deletedUsers,
                                                         DateTime currentDate)
        {
            Console.WriteLine("📤 Enviando notificações para o Slack...");

            var message = GenerateSlackMessage(teamInfo, currentUsers, newUsers, deletedUsers, currentDate);
            var content = new StringContent(message, Encoding.UTF8, "application/json");

            // Novo HttpClient para evitar conflitos de headers
            using var notificationClient = new HttpClient();

            // Enviar para canal #choro
            await SendToWebhook(notificationClient, _config.Webhooks.ChoroChannel, content, "Canal CHORO");

            // Enviar para canal privado
            await SendToWebhook(notificationClient, _config.Webhooks.PrivateChannel, content, "Canal PRIVADO");

            Console.WriteLine("✅ Notificações enviadas com sucesso!");
        }

        private static async Task SendToWebhook(HttpClient client,
                                                string webhookUrl,
                                                StringContent content,
                                                string channelName)
        {
            try
            {
                var response = await client.PostAsync(webhookUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"✅ Mensagem enviada para {channelName}");
                    LogPerformance($"Envio para {channelName}");
                }
                else
                {
                    Console.WriteLine($"⚠️ Falha ao enviar para {channelName}: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erro ao enviar para {channelName}: {ex.Message}");
            }
        }

        private static string GenerateSlackMessage(SlackTeamInfoResponse teamInfo,
                                                   Member[] currentUsers,
                                                   List<Member> newUsers,
                                                   List<Member> deletedUsers,
                                                   DateTime currentDate)
        {
            Console.WriteLine("📝 Gerando mensagem para Slack...");

            var activeUsersV1 = teamInfo.team.user_counts.active_members;
            var activeUsersV2 = currentUsers.Count(x => !x.Deleted);

            var newUsersText = newUsers.Count == 0
                ? "Nenhum usuário novo"
                : string.Join("\n", newUsers.Select(x => $"<@{x.Id}> {x.Profile.Real_name}"));

            var deletedUsersText = deletedUsers.Count == 0
                ? "Nenhum usuário removido"
                : string.Join("\n", deletedUsers.Select(x =>
                {
                    var daysText = x.Profile.Start_date != DateTime.MinValue
                        ? $" ({(currentDate - x.Profile.Start_date).TotalDays:F0} dias)"
                        : "";
                    return $"<@{x.Id}> {x.Profile.Real_name}{daysText}";
                }));

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
                ""text"": ""*TOTAL DE USUÁRIOS ATIVOS [V1]* - {activeUsersV1} MEMBROS""
            }}
        }},
        {{
            ""type"": ""section"",
            ""text"": {{
                ""type"": ""mrkdwn"",
                ""text"": ""*TOTAL DE USUÁRIOS ATIVOS [V2]* - {activeUsersV2} MEMBROS""
            }}
        }},
        {{
            ""type"": ""section"",
            ""text"": {{
                ""type"": ""mrkdwn"",
                ""text"": ""*NOVOS USUÁRIOS* - [{newUsers.Count}]\n{newUsersText}""
            }}
        }},
        {{
            ""type"": ""section"",
            ""text"": {{
                ""type"": ""mrkdwn"",
                ""text"": ""*USUÁRIOS REMOVIDOS* - [{deletedUsers.Count}]\n{deletedUsersText}""
            }}
        }}
    ]
}}";

            LogPerformance("Geração de mensagem");
            return message;
        }

        #endregion

        #region Utilities

        private static void LogPerformance(string operation)
        {
            Console.WriteLine($"⏱️ {operation}: {_stopwatch.Elapsed.TotalSeconds:F2}s");
            _stopwatch.Restart();
        }

        #endregion
    }
}