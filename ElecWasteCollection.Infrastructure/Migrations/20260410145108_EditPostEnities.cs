using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElecWasteCollection.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EditPostEnities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CompanyId",
                table: "Post",
                newName: "CollectionCompanyId");

            migrationBuilder.RenameIndex(
                name: "IX_Post_CompanyId",
                table: "Post",
                newName: "IX_Post_CollectionCompanyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CollectionCompanyId",
                table: "Post",
                newName: "CompanyId");

            migrationBuilder.RenameIndex(
                name: "IX_Post_CollectionCompanyId",
                table: "Post",
                newName: "IX_Post_CompanyId");
        }
    }
}
