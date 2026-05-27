using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyPortfolioAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class RestrictContactPlatforms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Instagram",
                table: "ContactInfos",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Facebook",
                table: "ContactInfos",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "GitHub",
                table: "ContactInfos",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                """
                UPDATE ci
                SET
                    LinkedIn = COALESCE(NULLIF(ci.LinkedIn, ''), linkedInLink.Url, ''),
                    Instagram = COALESCE(instagramLink.Url, ''),
                    Facebook = COALESCE(facebookLink.Url, ''),
                    GitHub = COALESCE(gitHubLink.Url, '')
                FROM ContactInfos AS ci
                OUTER APPLY (
                    SELECT TOP (1) cl.Url
                    FROM ContactLinks AS cl
                    WHERE cl.ContactInfoId = ci.Id
                      AND LOWER(LTRIM(RTRIM(cl.Label))) IN ('linkedin', 'linked in', 'linked-in')
                ) AS linkedInLink
                OUTER APPLY (
                    SELECT TOP (1) cl.Url
                    FROM ContactLinks AS cl
                    WHERE cl.ContactInfoId = ci.Id
                      AND LOWER(LTRIM(RTRIM(cl.Label))) = 'instagram'
                ) AS instagramLink
                OUTER APPLY (
                    SELECT TOP (1) cl.Url
                    FROM ContactLinks AS cl
                    WHERE cl.ContactInfoId = ci.Id
                      AND LOWER(LTRIM(RTRIM(cl.Label))) = 'facebook'
                ) AS facebookLink
                OUTER APPLY (
                    SELECT TOP (1) cl.Url
                    FROM ContactLinks AS cl
                    WHERE cl.ContactInfoId = ci.Id
                      AND LOWER(LTRIM(RTRIM(cl.Label))) IN ('github', 'git hub', 'git-hub')
                ) AS gitHubLink;
                """);

            migrationBuilder.DropTable(
                name: "ContactLinks");

            migrationBuilder.DropColumn(
                name: "Website",
                table: "ContactInfos");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Website",
                table: "ContactInfos",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ContactLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContactInfoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Url = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContactLinks_ContactInfos_ContactInfoId",
                        column: x => x.ContactInfoId,
                        principalTable: "ContactInfos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContactLinks_ContactInfoId",
                table: "ContactLinks",
                column: "ContactInfoId");

            migrationBuilder.Sql(
                """
                INSERT INTO ContactLinks (Id, ContactInfoId, Label, Url)
                SELECT NEWID(), Id, 'Instagram', Instagram
                FROM ContactInfos
                WHERE NULLIF(Instagram, '') IS NOT NULL;

                INSERT INTO ContactLinks (Id, ContactInfoId, Label, Url)
                SELECT NEWID(), Id, 'Facebook', Facebook
                FROM ContactInfos
                WHERE NULLIF(Facebook, '') IS NOT NULL;

                INSERT INTO ContactLinks (Id, ContactInfoId, Label, Url)
                SELECT NEWID(), Id, 'GitHub', GitHub
                FROM ContactInfos
                WHERE NULLIF(GitHub, '') IS NOT NULL;
                """);

            migrationBuilder.DropColumn(
                name: "Facebook",
                table: "ContactInfos");

            migrationBuilder.DropColumn(
                name: "GitHub",
                table: "ContactInfos");

            migrationBuilder.DropColumn(
                name: "Instagram",
                table: "ContactInfos");
        }
    }
}
