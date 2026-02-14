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
    public enum TestCasePriority
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4
    }

    public enum TestCaseStatus
    {
        Draft = 1,
        Active = 2,
        Passed = 3,
        Failed = 4,
        Skipped = 5,
        Blocked = 6
    }
}
