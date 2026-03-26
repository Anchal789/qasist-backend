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
            public const string Add = "INSERT INTO users (id, email, first_name, last_name, password_hash, role, created_by) VALUES (@p_id, @p_email, @p_first_name, @p_last_name, @p_password_hash, @p_role, @p_created_by);";
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

        public static class TestCases
        {
            // GET Queries
            public const string GetById = "SELECT * FROM get_test_case_by_id(@p_id);";
            public const string GetByProject = "SELECT * FROM get_test_cases_by_project(@p_project_id);";
            public const string GetPaged = "SELECT * FROM get_test_cases_paged(@p_project_id, @p_page_number, @p_page_size);";
            public const string GetCount = "SELECT get_test_case_count(@p_project_id);";
            public const string GetByStatus = "SELECT * FROM get_test_cases_by_status(@p_project_id, @p_status);";
            public const string GetAiGenerated = "SELECT * FROM get_ai_generated_test_cases(@p_project_id);";
            public const string GetByAssignee = "SELECT * FROM get_test_cases_by_assignee(@p_user_id);";

            // CREATE Queries
            public const string Create = "SELECT create_test_case(@p_id, @p_project_id, @p_endpoint, @p_method, @p_title, @p_steps, @p_expected_result, @p_priority, @p_status, @p_assigned_to, @p_is_ai_generated, @p_created_by);";
            //public const string BulkCreate = "SELECT bulk_create_test_cases(@p_test_cases, @p_project_id, @p_created_by);";
            public const string BulkCreate ="SELECT public.bulk_create_test_cases(@p_test_cases::jsonb, @p_project_id, @p_created_by);";


            // UPDATE Queries
            public const string Update = "SELECT update_test_case(@p_id, @p_endpoint, @p_method, @p_title, @p_steps, @p_expected_result, @p_priority, @p_status, @p_assigned_to, @p_updated_by);";
            public const string UpdateStatus = "SELECT update_test_case_status(@p_id, @p_status, @p_updated_by);";
            public const string Assign = "SELECT assign_test_case(@p_id, @p_assigned_to, @p_updated_by);";

            // DELETE Queries
            public const string Delete = "SELECT delete_test_case(@p_id, @p_deleted_by);";
            public const string BulkDeleteByProject = "SELECT delete_test_cases_by_project(@p_project_id, @p_deleted_by);";

            // STATISTICS Queries
            public const string GetStatistics = "SELECT * FROM get_test_case_statistics(@p_project_id);";
            public const string GetEndpointCoverage = "SELECT * FROM get_endpoint_coverage(@p_project_id);";

            // SEARCH Queries
            public const string Search = "SELECT * FROM search_test_cases(@p_project_id, @p_search_term);";

            // HELPER Queries
            public const string Exists = "SELECT test_case_exists(@p_id);";
        }

        public static class Roles
        {
            public const string GetUserRoles = "SELECT * FROM get_user_roles(@p_user_id);";
            public const string GetPrimaryRole = "SELECT get_user_primary_role(@p_user_id);";
            public const string AssignRole = "SELECT assign_user_role(@p_user_id, @p_role_id, @p_project_id, @p_assigned_by);";
            public const string RevokeRole = "SELECT revoke_user_role(@p_user_id, @p_role_id, @p_project_id, @p_revoked_by);";
            public const string HasPermission = "SELECT user_has_permission(@p_user_id, @p_permission_name, @p_resource_type, @p_action);";
            public const string GetAllRoles = "SELECT * FROM get_all_roles();";
            public const string GetUsersByRole = "SELECT * FROM get_users_by_role(@p_role_id);";
            public const string SyncPrimaryRole = "SELECT sync_user_primary_role(@p_user_id);";
            public const string InitializeRoles = "SELECT initialize_user_roles();";
        }
    }
}
