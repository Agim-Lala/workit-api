using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Workit.Core.Shared.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameUserRoleToWorker : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""UPDATE users SET "Role" = 'Worker' WHERE "Role" = 'User';""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""UPDATE users SET "Role" = 'User' WHERE "Role" = 'Worker';""");
        }
    }
}
