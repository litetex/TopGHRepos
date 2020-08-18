using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace TopGHRepos.Data.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RepositoryInfos",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DateCreated = table.Column<DateTime>(nullable: false),
                    DefaultBranch = table.Column<string>(nullable: true),
                    OpenIssuesCount = table.Column<int>(nullable: false),
                    PushedAt = table.Column<DateTimeOffset>(nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(nullable: false),
                    HasIssues = table.Column<bool>(nullable: false),
                    WatchersCount = table.Column<int>(nullable: false),
                    HasWiki = table.Column<bool>(nullable: false),
                    HasDownloads = table.Column<bool>(nullable: false),
                    HasPages = table.Column<bool>(nullable: false),
                    LicenseKey = table.Column<string>(nullable: true),
                    LicenseApiUrl = table.Column<string>(nullable: true),
                    StargazersCount = table.Column<int>(nullable: false),
                    ForksCount = table.Column<int>(nullable: false),
                    Fork = table.Column<bool>(nullable: false),
                    ApiUrl = table.Column<string>(nullable: true),
                    HtmlUrl = table.Column<string>(nullable: true),
                    CloneUrl = table.Column<string>(nullable: true),
                    GitUrl = table.Column<string>(nullable: true),
                    SshUrl = table.Column<string>(nullable: true),
                    SvnUrl = table.Column<string>(nullable: true),
                    MirrorUrl = table.Column<string>(nullable: true),
                    GitHubId = table.Column<long>(nullable: false),
                    NodeId = table.Column<string>(nullable: true),
                    OwnerLoginName = table.Column<string>(nullable: false),
                    Name = table.Column<string>(nullable: false),
                    FullName = table.Column<string>(nullable: true),
                    IsTemplate = table.Column<bool>(nullable: false),
                    Description = table.Column<string>(nullable: true),
                    Homepage = table.Column<string>(nullable: true),
                    Language = table.Column<string>(nullable: true),
                    Private = table.Column<bool>(nullable: false),
                    Size = table.Column<long>(nullable: false),
                    Archived = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepositoryInfos", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RepositoryInfos_GitHubId",
                table: "RepositoryInfos",
                column: "GitHubId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RepositoryInfos");
        }
    }
}
