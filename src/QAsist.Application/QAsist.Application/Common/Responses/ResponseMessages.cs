namespace QAsist.Application.Common.Responses
{

    public static class ResponseMessages
    {
        // General
        public const string OperationSuccessful = "Operation completed successfully.";
        public const string OperationFailed = "Operation failed.";
        public const string RecordNotFound = "Record not found.";
        public const string RecordAlreadyExists = "Record already exists.";
        public const string ValidationFailed = "Validation failed.";
        public const string UnauthorizedAccess = "Unauthorized access.";
        public const string ForbiddenAccess = "You do not have permission to perform this action.";
        public const string InternalServerError = "An internal server error occurred.";

        // Project
        public const string ProjectCreatedSuccessfully = "Project created successfully.";
        public const string ProjectUpdatedSuccessfully = "Project updated successfully.";
        public const string ProjectDeletedSuccessfully = "Project deleted successfully.";
        public const string ProjectNotFound = "Project not found.";
        public const string ProjectCodeAlreadyExists = "Project code already exists.";

        // User
        public const string UserCreatedSuccessfully = "User created successfully.";
        public const string UserUpdatedSuccessfully = "User updated successfully.";
        public const string UserDeletedSuccessfully = "User deleted successfully.";
        public const string UserNotFound = "User not found.";
        public const string EmailAlreadyExists = "Email already exists.";
        public const string InvalidCredentials = "Invalid email or password.";
        public const string UserDeactivated = "User account is deactivated.";

        // Authentication
        public const string LoginSuccessful = "Login successful.";
        public const string LogoutSuccessful = "Logout successful.";
        public const string TokenRefreshedSuccessfully = "Token refreshed successfully.";
        public const string InvalidRefreshToken = "Invalid or expired refresh token.";
        public const string SessionExpired = "Session has expired.";
        public const string InvalidToken = "Invalid token.";
        public const string TokenExpired = "Token has expired.";

        // Validation
        public const string RequiredField = "{0} is required.";
        public const string InvalidEmail = "Invalid email format.";
        public const string InvalidLength = "{0} must be between {1} and {2} characters.";
        public const string InvalidFormat = "Invalid {0} format.";
    }
}
