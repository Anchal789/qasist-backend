namespace QAsist.Infrastructure.Persistence
{
    public static class SqlQueries
    {
        public static class Projects
        {
            public const string GetById = "SELECT * FROM get_project_by_id(@p_id);";
            public const string GetAll = "SELECT * FROM get_all_projects();";
            public const string GetPaged = "SELECT * FROM get_projects_paged(@p_page_number, @p_page_size);";
            public const string GetPagedCount = "SELECT COUNT(*) FROM projects WHERE is_deleted = false;";
            public const string Create = "SELECT create_project(@p_id, @p_name, @p_description, @p_code, @p_owner_id, @p_start_date, @p_end_date, @p_status, @p_created_by);";
            public const string Update = "SELECT update_project(@p_id, @p_name, @p_description, @p_code, @p_owner_id, @p_start_date, @p_end_date, @p_status, @p_updated_by);";
            public const string Delete = "SELECT delete_project(@p_id, @p_deleted_by);";
            public const string ExistsByCode = "SELECT EXISTS(SELECT 1 FROM projects WHERE code = @p_code AND is_deleted = false AND (@p_exclude_id IS NULL OR id != @p_exclude_id));";
        }

        public static class Users
        {
            public const string GetById = "SELECT * FROM get_user_by_id(@p_id);";
            public const string GetByEmail = "SELECT * FROM get_user_by_email(@p_email);";
            public const string Create = "SELECT create_user(@p_id, @p_email, @p_first_name, @p_last_name, @p_password_hash, @p_role, @p_created_by);";
            public const string Update = "SELECT update_user(@p_id, @p_email, @p_first_name, @p_last_name, @p_role, @p_is_active, @p_updated_by);";
            public const string ExistsByEmail = "SELECT EXISTS(SELECT 1 FROM users WHERE email = @p_email AND is_deleted = false AND (@p_exclude_id IS NULL OR id != @p_exclude_id));";
            public const string UpdateLastLogin = "SELECT update_user_last_login(@p_user_id);";
        }

        public static class RefreshTokens
        {
            public const string GetByToken = "SELECT * FROM get_refresh_token_by_token(@p_token);";
            public const string GetBySessionId = "SELECT * FROM get_refresh_token_by_session_id(@p_session_id);";
            public const string Create = "SELECT create_refresh_token(@p_id, @p_user_id, @p_token, @p_session_id, @p_expires_at, @p_ip_address, @p_user_agent, @p_created_by);";
            public const string Revoke = "SELECT revoke_refresh_token(@p_token);";
            public const string RevokeBySessionId = "SELECT revoke_refresh_token_by_session_id(@p_session_id);";
            public const string RevokeAllByUserId = "SELECT revoke_all_refresh_tokens_by_user_id(@p_user_id);";
            public const string DeleteExpired = "DELETE FROM refresh_tokens WHERE expires_at < NOW() OR is_revoked = true;";
        }
    }
}
