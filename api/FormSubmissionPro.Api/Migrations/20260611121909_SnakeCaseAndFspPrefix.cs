using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FormSubmissionPro.Api.Migrations
{
    /// <inheritdoc />
    public partial class SnakeCaseAndFspPrefix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApiKeys_Organizations_OrganizationId",
                table: "ApiKeys");

            migrationBuilder.DropForeignKey(
                name: "FK_Forms_Organizations_OrganizationId",
                table: "Forms");

            migrationBuilder.DropForeignKey(
                name: "FK_Submissions_Forms_FormId",
                table: "Submissions");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Organizations_OrganizationId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Webhooks_Forms_FormId",
                table: "Webhooks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Webhooks",
                table: "Webhooks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Users",
                table: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Submissions",
                table: "Submissions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Organizations",
                table: "Organizations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Forms",
                table: "Forms");

            migrationBuilder.DropPrimaryKey(
                name: "PK_EmailConfigurations",
                table: "EmailConfigurations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ApiKeys",
                table: "ApiKeys");

            migrationBuilder.RenameTable(
                name: "Webhooks",
                newName: "fsp_webhooks");

            migrationBuilder.RenameTable(
                name: "Users",
                newName: "fsp_users");

            migrationBuilder.RenameTable(
                name: "Submissions",
                newName: "fsp_submissions");

            migrationBuilder.RenameTable(
                name: "Organizations",
                newName: "fsp_organizations");

            migrationBuilder.RenameTable(
                name: "Forms",
                newName: "fsp_forms");

            migrationBuilder.RenameTable(
                name: "EmailConfigurations",
                newName: "fsp_email_configurations");

            migrationBuilder.RenameTable(
                name: "ApiKeys",
                newName: "fsp_api_keys");

            migrationBuilder.RenameColumn(
                name: "Url",
                table: "fsp_webhooks",
                newName: "url");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "fsp_webhooks",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "OrganizationId",
                table: "fsp_webhooks",
                newName: "organization_id");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "fsp_webhooks",
                newName: "is_active");

            migrationBuilder.RenameColumn(
                name: "FormId",
                table: "fsp_webhooks",
                newName: "form_id");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "fsp_webhooks",
                newName: "created_at");

            migrationBuilder.RenameIndex(
                name: "IX_Webhooks_FormId",
                table: "fsp_webhooks",
                newName: "ix_fsp_webhooks_form_id");

            migrationBuilder.RenameColumn(
                name: "Email",
                table: "fsp_users",
                newName: "email");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "fsp_users",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "PasswordHash",
                table: "fsp_users",
                newName: "password_hash");

            migrationBuilder.RenameColumn(
                name: "OrganizationId",
                table: "fsp_users",
                newName: "organization_id");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "fsp_users",
                newName: "created_at");

            migrationBuilder.RenameIndex(
                name: "IX_Users_OrganizationId",
                table: "fsp_users",
                newName: "ix_fsp_users_organization_id");

            migrationBuilder.RenameIndex(
                name: "IX_Users_Email",
                table: "fsp_users",
                newName: "ix_fsp_users_email");

            migrationBuilder.RenameColumn(
                name: "Subject",
                table: "fsp_submissions",
                newName: "subject");

            migrationBuilder.RenameColumn(
                name: "Data",
                table: "fsp_submissions",
                newName: "data");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "fsp_submissions",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UserAgent",
                table: "fsp_submissions",
                newName: "user_agent");

            migrationBuilder.RenameColumn(
                name: "SenderEmail",
                table: "fsp_submissions",
                newName: "sender_email");

            migrationBuilder.RenameColumn(
                name: "OrganizationId",
                table: "fsp_submissions",
                newName: "organization_id");

            migrationBuilder.RenameColumn(
                name: "IsSpam",
                table: "fsp_submissions",
                newName: "is_spam");

            migrationBuilder.RenameColumn(
                name: "IpAddress",
                table: "fsp_submissions",
                newName: "ip_address");

            migrationBuilder.RenameColumn(
                name: "FormId",
                table: "fsp_submissions",
                newName: "form_id");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "fsp_submissions",
                newName: "created_at");

            migrationBuilder.RenameIndex(
                name: "IX_Submissions_OrganizationId_CreatedAt",
                table: "fsp_submissions",
                newName: "ix_fsp_submissions_organization_id_created_at");

            migrationBuilder.RenameIndex(
                name: "IX_Submissions_FormId_CreatedAt",
                table: "fsp_submissions",
                newName: "ix_fsp_submissions_form_id_created_at");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "fsp_organizations",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "fsp_organizations",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "PlanTier",
                table: "fsp_organizations",
                newName: "plan_tier");

            migrationBuilder.RenameColumn(
                name: "NotificationEmail",
                table: "fsp_organizations",
                newName: "notification_email");

            migrationBuilder.RenameColumn(
                name: "MonthlyQuota",
                table: "fsp_organizations",
                newName: "monthly_quota");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "fsp_organizations",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "fsp_forms",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "fsp_forms",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "ToEmailOverride",
                table: "fsp_forms",
                newName: "to_email_override");

            migrationBuilder.RenameColumn(
                name: "RequireCaptcha",
                table: "fsp_forms",
                newName: "require_captcha");

            migrationBuilder.RenameColumn(
                name: "RedirectUrl",
                table: "fsp_forms",
                newName: "redirect_url");

            migrationBuilder.RenameColumn(
                name: "OrganizationId",
                table: "fsp_forms",
                newName: "organization_id");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "fsp_forms",
                newName: "is_active");

            migrationBuilder.RenameColumn(
                name: "FromName",
                table: "fsp_forms",
                newName: "from_name");

            migrationBuilder.RenameColumn(
                name: "DefaultSubject",
                table: "fsp_forms",
                newName: "default_subject");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "fsp_forms",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "CaptchaSecret",
                table: "fsp_forms",
                newName: "captcha_secret");

            migrationBuilder.RenameColumn(
                name: "CaptchaProvider",
                table: "fsp_forms",
                newName: "captcha_provider");

            migrationBuilder.RenameColumn(
                name: "AutoresponderSubject",
                table: "fsp_forms",
                newName: "autoresponder_subject");

            migrationBuilder.RenameColumn(
                name: "AutoresponderEnabled",
                table: "fsp_forms",
                newName: "autoresponder_enabled");

            migrationBuilder.RenameColumn(
                name: "AutoresponderBody",
                table: "fsp_forms",
                newName: "autoresponder_body");

            migrationBuilder.RenameColumn(
                name: "AllowedDomains",
                table: "fsp_forms",
                newName: "allowed_domains");

            migrationBuilder.RenameColumn(
                name: "AccessKey",
                table: "fsp_forms",
                newName: "access_key");

            migrationBuilder.RenameIndex(
                name: "IX_Forms_OrganizationId",
                table: "fsp_forms",
                newName: "ix_fsp_forms_organization_id");

            migrationBuilder.RenameIndex(
                name: "IX_Forms_AccessKey",
                table: "fsp_forms",
                newName: "ix_fsp_forms_access_key");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "fsp_email_configurations",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "fsp_email_configurations",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "GraphTenantId",
                table: "fsp_email_configurations",
                newName: "graph_tenant_id");

            migrationBuilder.RenameColumn(
                name: "GraphSender",
                table: "fsp_email_configurations",
                newName: "graph_sender");

            migrationBuilder.RenameColumn(
                name: "GraphClientSecret",
                table: "fsp_email_configurations",
                newName: "graph_client_secret");

            migrationBuilder.RenameColumn(
                name: "GraphClientId",
                table: "fsp_email_configurations",
                newName: "graph_client_id");

            migrationBuilder.RenameColumn(
                name: "DefaultFromName",
                table: "fsp_email_configurations",
                newName: "default_from_name");

            migrationBuilder.RenameColumn(
                name: "Prefix",
                table: "fsp_api_keys",
                newName: "prefix");

            migrationBuilder.RenameColumn(
                name: "Label",
                table: "fsp_api_keys",
                newName: "label");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "fsp_api_keys",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "RevokedAt",
                table: "fsp_api_keys",
                newName: "revoked_at");

            migrationBuilder.RenameColumn(
                name: "OrganizationId",
                table: "fsp_api_keys",
                newName: "organization_id");

            migrationBuilder.RenameColumn(
                name: "LastUsedAt",
                table: "fsp_api_keys",
                newName: "last_used_at");

            migrationBuilder.RenameColumn(
                name: "KeyHash",
                table: "fsp_api_keys",
                newName: "key_hash");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "fsp_api_keys",
                newName: "created_at");

            migrationBuilder.RenameIndex(
                name: "IX_ApiKeys_OrganizationId",
                table: "fsp_api_keys",
                newName: "ix_fsp_api_keys_organization_id");

            migrationBuilder.RenameIndex(
                name: "IX_ApiKeys_KeyHash",
                table: "fsp_api_keys",
                newName: "ix_fsp_api_keys_key_hash");

            migrationBuilder.AddPrimaryKey(
                name: "pk_fsp_webhooks",
                table: "fsp_webhooks",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_fsp_users",
                table: "fsp_users",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_fsp_submissions",
                table: "fsp_submissions",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_fsp_organizations",
                table: "fsp_organizations",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_fsp_forms",
                table: "fsp_forms",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_fsp_email_configurations",
                table: "fsp_email_configurations",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_fsp_api_keys",
                table: "fsp_api_keys",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_fsp_api_keys_organizations_organization_id",
                table: "fsp_api_keys",
                column: "organization_id",
                principalTable: "fsp_organizations",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_fsp_forms_organizations_organization_id",
                table: "fsp_forms",
                column: "organization_id",
                principalTable: "fsp_organizations",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_fsp_submissions_fsp_forms_form_id",
                table: "fsp_submissions",
                column: "form_id",
                principalTable: "fsp_forms",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_fsp_users_fsp_organizations_organization_id",
                table: "fsp_users",
                column: "organization_id",
                principalTable: "fsp_organizations",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_fsp_webhooks_fsp_forms_form_id",
                table: "fsp_webhooks",
                column: "form_id",
                principalTable: "fsp_forms",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_fsp_api_keys_organizations_organization_id",
                table: "fsp_api_keys");

            migrationBuilder.DropForeignKey(
                name: "fk_fsp_forms_organizations_organization_id",
                table: "fsp_forms");

            migrationBuilder.DropForeignKey(
                name: "fk_fsp_submissions_fsp_forms_form_id",
                table: "fsp_submissions");

            migrationBuilder.DropForeignKey(
                name: "fk_fsp_users_fsp_organizations_organization_id",
                table: "fsp_users");

            migrationBuilder.DropForeignKey(
                name: "fk_fsp_webhooks_fsp_forms_form_id",
                table: "fsp_webhooks");

            migrationBuilder.DropPrimaryKey(
                name: "pk_fsp_webhooks",
                table: "fsp_webhooks");

            migrationBuilder.DropPrimaryKey(
                name: "pk_fsp_users",
                table: "fsp_users");

            migrationBuilder.DropPrimaryKey(
                name: "pk_fsp_submissions",
                table: "fsp_submissions");

            migrationBuilder.DropPrimaryKey(
                name: "pk_fsp_organizations",
                table: "fsp_organizations");

            migrationBuilder.DropPrimaryKey(
                name: "pk_fsp_forms",
                table: "fsp_forms");

            migrationBuilder.DropPrimaryKey(
                name: "pk_fsp_email_configurations",
                table: "fsp_email_configurations");

            migrationBuilder.DropPrimaryKey(
                name: "pk_fsp_api_keys",
                table: "fsp_api_keys");

            migrationBuilder.RenameTable(
                name: "fsp_webhooks",
                newName: "Webhooks");

            migrationBuilder.RenameTable(
                name: "fsp_users",
                newName: "Users");

            migrationBuilder.RenameTable(
                name: "fsp_submissions",
                newName: "Submissions");

            migrationBuilder.RenameTable(
                name: "fsp_organizations",
                newName: "Organizations");

            migrationBuilder.RenameTable(
                name: "fsp_forms",
                newName: "Forms");

            migrationBuilder.RenameTable(
                name: "fsp_email_configurations",
                newName: "EmailConfigurations");

            migrationBuilder.RenameTable(
                name: "fsp_api_keys",
                newName: "ApiKeys");

            migrationBuilder.RenameColumn(
                name: "url",
                table: "Webhooks",
                newName: "Url");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Webhooks",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "organization_id",
                table: "Webhooks",
                newName: "OrganizationId");

            migrationBuilder.RenameColumn(
                name: "is_active",
                table: "Webhooks",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "form_id",
                table: "Webhooks",
                newName: "FormId");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "Webhooks",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "ix_fsp_webhooks_form_id",
                table: "Webhooks",
                newName: "IX_Webhooks_FormId");

            migrationBuilder.RenameColumn(
                name: "email",
                table: "Users",
                newName: "Email");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Users",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "password_hash",
                table: "Users",
                newName: "PasswordHash");

            migrationBuilder.RenameColumn(
                name: "organization_id",
                table: "Users",
                newName: "OrganizationId");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "Users",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "ix_fsp_users_organization_id",
                table: "Users",
                newName: "IX_Users_OrganizationId");

            migrationBuilder.RenameIndex(
                name: "ix_fsp_users_email",
                table: "Users",
                newName: "IX_Users_Email");

            migrationBuilder.RenameColumn(
                name: "subject",
                table: "Submissions",
                newName: "Subject");

            migrationBuilder.RenameColumn(
                name: "data",
                table: "Submissions",
                newName: "Data");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Submissions",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "user_agent",
                table: "Submissions",
                newName: "UserAgent");

            migrationBuilder.RenameColumn(
                name: "sender_email",
                table: "Submissions",
                newName: "SenderEmail");

            migrationBuilder.RenameColumn(
                name: "organization_id",
                table: "Submissions",
                newName: "OrganizationId");

            migrationBuilder.RenameColumn(
                name: "is_spam",
                table: "Submissions",
                newName: "IsSpam");

            migrationBuilder.RenameColumn(
                name: "ip_address",
                table: "Submissions",
                newName: "IpAddress");

            migrationBuilder.RenameColumn(
                name: "form_id",
                table: "Submissions",
                newName: "FormId");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "Submissions",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "ix_fsp_submissions_organization_id_created_at",
                table: "Submissions",
                newName: "IX_Submissions_OrganizationId_CreatedAt");

            migrationBuilder.RenameIndex(
                name: "ix_fsp_submissions_form_id_created_at",
                table: "Submissions",
                newName: "IX_Submissions_FormId_CreatedAt");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "Organizations",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Organizations",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "plan_tier",
                table: "Organizations",
                newName: "PlanTier");

            migrationBuilder.RenameColumn(
                name: "notification_email",
                table: "Organizations",
                newName: "NotificationEmail");

            migrationBuilder.RenameColumn(
                name: "monthly_quota",
                table: "Organizations",
                newName: "MonthlyQuota");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "Organizations",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "Forms",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Forms",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "to_email_override",
                table: "Forms",
                newName: "ToEmailOverride");

            migrationBuilder.RenameColumn(
                name: "require_captcha",
                table: "Forms",
                newName: "RequireCaptcha");

            migrationBuilder.RenameColumn(
                name: "redirect_url",
                table: "Forms",
                newName: "RedirectUrl");

            migrationBuilder.RenameColumn(
                name: "organization_id",
                table: "Forms",
                newName: "OrganizationId");

            migrationBuilder.RenameColumn(
                name: "is_active",
                table: "Forms",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "from_name",
                table: "Forms",
                newName: "FromName");

            migrationBuilder.RenameColumn(
                name: "default_subject",
                table: "Forms",
                newName: "DefaultSubject");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "Forms",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "captcha_secret",
                table: "Forms",
                newName: "CaptchaSecret");

            migrationBuilder.RenameColumn(
                name: "captcha_provider",
                table: "Forms",
                newName: "CaptchaProvider");

            migrationBuilder.RenameColumn(
                name: "autoresponder_subject",
                table: "Forms",
                newName: "AutoresponderSubject");

            migrationBuilder.RenameColumn(
                name: "autoresponder_enabled",
                table: "Forms",
                newName: "AutoresponderEnabled");

            migrationBuilder.RenameColumn(
                name: "autoresponder_body",
                table: "Forms",
                newName: "AutoresponderBody");

            migrationBuilder.RenameColumn(
                name: "allowed_domains",
                table: "Forms",
                newName: "AllowedDomains");

            migrationBuilder.RenameColumn(
                name: "access_key",
                table: "Forms",
                newName: "AccessKey");

            migrationBuilder.RenameIndex(
                name: "ix_fsp_forms_organization_id",
                table: "Forms",
                newName: "IX_Forms_OrganizationId");

            migrationBuilder.RenameIndex(
                name: "ix_fsp_forms_access_key",
                table: "Forms",
                newName: "IX_Forms_AccessKey");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "EmailConfigurations",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "EmailConfigurations",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "graph_tenant_id",
                table: "EmailConfigurations",
                newName: "GraphTenantId");

            migrationBuilder.RenameColumn(
                name: "graph_sender",
                table: "EmailConfigurations",
                newName: "GraphSender");

            migrationBuilder.RenameColumn(
                name: "graph_client_secret",
                table: "EmailConfigurations",
                newName: "GraphClientSecret");

            migrationBuilder.RenameColumn(
                name: "graph_client_id",
                table: "EmailConfigurations",
                newName: "GraphClientId");

            migrationBuilder.RenameColumn(
                name: "default_from_name",
                table: "EmailConfigurations",
                newName: "DefaultFromName");

            migrationBuilder.RenameColumn(
                name: "prefix",
                table: "ApiKeys",
                newName: "Prefix");

            migrationBuilder.RenameColumn(
                name: "label",
                table: "ApiKeys",
                newName: "Label");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "ApiKeys",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "revoked_at",
                table: "ApiKeys",
                newName: "RevokedAt");

            migrationBuilder.RenameColumn(
                name: "organization_id",
                table: "ApiKeys",
                newName: "OrganizationId");

            migrationBuilder.RenameColumn(
                name: "last_used_at",
                table: "ApiKeys",
                newName: "LastUsedAt");

            migrationBuilder.RenameColumn(
                name: "key_hash",
                table: "ApiKeys",
                newName: "KeyHash");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "ApiKeys",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "ix_fsp_api_keys_organization_id",
                table: "ApiKeys",
                newName: "IX_ApiKeys_OrganizationId");

            migrationBuilder.RenameIndex(
                name: "ix_fsp_api_keys_key_hash",
                table: "ApiKeys",
                newName: "IX_ApiKeys_KeyHash");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Webhooks",
                table: "Webhooks",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Users",
                table: "Users",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Submissions",
                table: "Submissions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Organizations",
                table: "Organizations",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Forms",
                table: "Forms",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_EmailConfigurations",
                table: "EmailConfigurations",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ApiKeys",
                table: "ApiKeys",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ApiKeys_Organizations_OrganizationId",
                table: "ApiKeys",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Forms_Organizations_OrganizationId",
                table: "Forms",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Submissions_Forms_FormId",
                table: "Submissions",
                column: "FormId",
                principalTable: "Forms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Organizations_OrganizationId",
                table: "Users",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Webhooks_Forms_FormId",
                table: "Webhooks",
                column: "FormId",
                principalTable: "Forms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
