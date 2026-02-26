namespace bot_fofoqueiro
{
    public class SlackUserInfoResponse
    {
        public bool Ok { get; set; }
        public Member[] Members { get; set; }
    }

    public class Member
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public bool Deleted { get; set; }
        public bool Is_bot { get; set; }
        public string Real_name { get; set; }
        public Profile Profile { get; set; }
    }

    public class Profile
    {
        public string Real_name { get; set; }
        public string Real_name_normalized { get; set; }
        public string Display_name { get; set; }
        public string Display_name_normalized { get; set; }
        public DateTime Start_date { get; set; }
        public string Email { get; set; }
    }
}