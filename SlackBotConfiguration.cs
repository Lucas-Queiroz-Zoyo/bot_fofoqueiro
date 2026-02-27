namespace bot_fofoqueiro
{
    public class SlackBotConfiguration
    {
        public string SlackToken { get; set; } = string.Empty;
        public string SlackCookie { get; set; } = string.Empty;
        public string SlackTeamId { get; set; } = string.Empty;
        public SlackWebhooks Webhooks { get; set; } = new();
    }

    public class SlackWebhooks
    {
        public string ChoroChannel { get; set; } = string.Empty;
        public string PrivateChannel { get; set; } = string.Empty;
    }
}