namespace QAsist.Domain.Enums
{
    public enum ProjectStatus
    {
        Planning = 1,
        Active = 2,
        OnHold = 3,
        Completed = 4,
        Cancelled = 5
    }

    public enum UserRole
    {
        SuperAdmin = 1,
        Admin = 2,
        ProjectManager = 3,
        QALead = 4,
        QAEngineer = 5,
        Developer = 6,
        Viewer = 7
    }
}
