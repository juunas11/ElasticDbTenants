using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ElasticDbTenants.CatalogDb.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(maxLength: 128, nullable: false),
                    CreationStatus = table.Column<int>(nullable: false),
                    ConnectionString = table.Column<string>(maxLength: 512, nullable: false),
                    ServerName = table.Column<string>(maxLength: 128, nullable: false),
                    DatabaseName = table.Column<string>(maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Tenants");
        }
    }
}
