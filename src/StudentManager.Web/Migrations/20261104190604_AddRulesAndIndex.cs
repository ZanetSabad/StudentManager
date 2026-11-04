using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentManager.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddRulesAndIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Students_SchoolClasses_SchoolClassId",
                table: "Students");

            migrationBuilder.DropForeignKey(
                name: "FK_Students_SchoolYears_SchoolYearId",
                table: "Students");

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "Students",
                type: "TEXT",
                nullable: false,
                collation: "NOCASE",
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.CreateIndex(
                name: "IX_Students_StudentIdentifier",
                table: "Students",
                column: "StudentIdentifier",
                unique: true,
                filter: "StudentIdentifier IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Students_Username",
                table: "Students",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SchoolYears_Name",
                table: "SchoolYears",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SchoolClasses_Name",
                table: "SchoolClasses",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Students_SchoolClasses_SchoolClassId",
                table: "Students",
                column: "SchoolClassId",
                principalTable: "SchoolClasses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Students_SchoolYears_SchoolYearId",
                table: "Students",
                column: "SchoolYearId",
                principalTable: "SchoolYears",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Students_SchoolClasses_SchoolClassId",
                table: "Students");

            migrationBuilder.DropForeignKey(
                name: "FK_Students_SchoolYears_SchoolYearId",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Students_StudentIdentifier",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Students_Username",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_SchoolYears_Name",
                table: "SchoolYears");

            migrationBuilder.DropIndex(
                name: "IX_SchoolClasses_Name",
                table: "SchoolClasses");

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "Students",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldCollation: "NOCASE");

            migrationBuilder.AddForeignKey(
                name: "FK_Students_SchoolClasses_SchoolClassId",
                table: "Students",
                column: "SchoolClassId",
                principalTable: "SchoolClasses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Students_SchoolYears_SchoolYearId",
                table: "Students",
                column: "SchoolYearId",
                principalTable: "SchoolYears",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
