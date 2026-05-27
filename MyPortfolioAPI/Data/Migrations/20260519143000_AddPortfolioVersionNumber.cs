using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyPortfolioAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPortfolioVersionNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "VersionNumber",
                table: "PortfolioVersions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                """
                WITH NumberedVersions AS (
                    SELECT
                        Id,
                        ROW_NUMBER() OVER (
                            PARTITION BY PortfolioId
                            ORDER BY GeneratedAt, Id
                        ) AS VersionNumber
                    FROM PortfolioVersions
                )
                UPDATE pv
                SET VersionNumber = numbered.VersionNumber
                FROM PortfolioVersions AS pv
                INNER JOIN NumberedVersions AS numbered
                    ON numbered.Id = pv.Id;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VersionNumber",
                table: "PortfolioVersions");
        }
    }
}
