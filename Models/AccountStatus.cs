namespace LoginSystem.Models
{
    public enum AccountStatus
    {
        Pending,   // just registered, not yet email-verified
        Active,    // can log in
        Locked,    // temporary, auto-set after repeated failed logins
        Suspended,
        Disabled
    }
}
