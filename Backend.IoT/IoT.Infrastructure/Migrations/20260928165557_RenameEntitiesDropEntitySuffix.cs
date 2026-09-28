using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IoT.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameEntitiesDropEntitySuffix : Migration
    {
        /// <inheritdoc />
        // Intentionally empty. Dropping the "Entity" suffix from the domain types is a
        // C# rename only: every table name is pinned with ToTable in Configurations/, so
        // the relational model is byte-for-byte identical. This migration exists solely to
        // keep the model snapshot in step with the renamed types, so the next real
        // migration diffs against an accurate baseline.
        protected override void Up(MigrationBuilder migrationBuilder)
        {
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
