using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IoT.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ScheduleGroupPairing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Pending schedules from before this change cannot survive it. ScheduleGroupId
            // is nullable, so existing rows would get NULL -- and NULL now *means*
            // "system-raised auto-close". A user's old unpaired Open would therefore be
            // misread as an auto-close: superseded on the next open, shown as automatic in
            // the UI, and impossible for them to delete.
            //
            // Backfilling a group id would be worse: an Open with a group but no matching
            // Close is an invisible half-period that still fires, leaving the door open --
            // exactly what the pairing exists to prevent. There is no correct closing time
            // to invent, so these rows go.
            //
            // Only PENDING rows are removed. Triggered and deactivated rows are inert
            // history and stay for the event log.
            migrationBuilder.Sql(
                "DELETE FROM Schedules WHERE IsActive = 1 AND WasTriggered = 0;");

            migrationBuilder.DropIndex(
                name: "IX_Schedules_DeviceId",
                table: "Schedules");

            migrationBuilder.AddColumn<Guid>(
                name: "ScheduleGroupId",
                table: "Schedules",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_DeviceId_IsActive_WasTriggered",
                table: "Schedules",
                columns: new[] { "DeviceId", "IsActive", "WasTriggered" });

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_ScheduleGroupId",
                table: "Schedules",
                column: "ScheduleGroupId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Schedules_DeviceId_IsActive_WasTriggered",
                table: "Schedules");

            migrationBuilder.DropIndex(
                name: "IX_Schedules_ScheduleGroupId",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "ScheduleGroupId",
                table: "Schedules");

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_DeviceId",
                table: "Schedules",
                column: "DeviceId");
        }
    }
}
