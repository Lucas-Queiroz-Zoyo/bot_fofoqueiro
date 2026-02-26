public class SlackTeamInfoResponse
{
    public bool ok { get; set; }
    public Team team { get; set; }
}

public class Team
{
    public User_Counts user_counts { get; set; }
}

public class User_Counts
{
    public int active_members { get; set; }
}