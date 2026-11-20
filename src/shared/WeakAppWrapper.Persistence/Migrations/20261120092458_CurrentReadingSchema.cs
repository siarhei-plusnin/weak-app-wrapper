using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeakAppWrapper.Processor.Migrations
{
    /// <inheritdoc />
    public partial class CurrentReadingSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_readings_name",
                table: "readings");

            migrationBuilder.DropIndex(
                name: "ix_readings_type",
                table: "readings");

            migrationBuilder.RenameColumn(
                name: "received_at",
                table: "readings",
                newName: "processor_received_at");

            migrationBuilder.RenameColumn(
                name: "payload",
                table: "readings",
                newName: "raw_record");

            migrationBuilder.RenameColumn(
                name: "observed_at",
                table: "readings",
                newName: "captured_at");

            migrationBuilder.RenameIndex(
                name: "ix_readings_type_observed_at",
                table: "readings",
                newName: "ix_readings_type_captured_at");

            migrationBuilder.RenameIndex(
                name: "ix_readings_observed_at",
                table: "readings",
                newName: "ix_readings_captured_at");

            migrationBuilder.RenameIndex(
                name: "ix_readings_name_observed_at",
                table: "readings",
                newName: "ix_readings_name_captured_at");

            migrationBuilder.AddColumn<int>(
                name: "item_index",
                table: "readings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "source_message_id",
                table: "readings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "uq_readings_source_message_item",
                table: "readings",
                columns: new[] { "source", "source_message_id", "item_index" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uq_readings_source_message_item",
                table: "readings");

            migrationBuilder.DropColumn(
                name: "item_index",
                table: "readings");

            migrationBuilder.DropColumn(
                name: "source_message_id",
                table: "readings");

            migrationBuilder.RenameColumn(
                name: "raw_record",
                table: "readings",
                newName: "payload");

            migrationBuilder.RenameColumn(
                name: "processor_received_at",
                table: "readings",
                newName: "received_at");

            migrationBuilder.RenameColumn(
                name: "captured_at",
                table: "readings",
                newName: "observed_at");

            migrationBuilder.RenameIndex(
                name: "ix_readings_type_captured_at",
                table: "readings",
                newName: "ix_readings_type_observed_at");

            migrationBuilder.RenameIndex(
                name: "ix_readings_name_captured_at",
                table: "readings",
                newName: "ix_readings_name_observed_at");

            migrationBuilder.RenameIndex(
                name: "ix_readings_captured_at",
                table: "readings",
                newName: "ix_readings_observed_at");

            migrationBuilder.CreateIndex(
                name: "ix_readings_name",
                table: "readings",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_readings_type",
                table: "readings",
                column: "type");
        }
    }
}
