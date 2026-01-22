using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EhrSecure.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientDoctorAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssignedDoctorEmail",
                table: "Patients",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AssignedDoctorId",
                table: "Patients",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactEmail",
                table: "Patients",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactPhone",
                table: "Patients",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssignedDoctorEmail",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "AssignedDoctorId",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "ContactEmail",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "ContactPhone",
                table: "Patients");
        }
    }
}
