using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceLaptop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVariantSupportToProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH('dbo.Products', 'ParentProductId') IS NULL
                    ALTER TABLE [Products] ADD [ParentProductId] int NULL;

                IF COL_LENGTH('dbo.Products', 'VariantName') IS NULL
                    ALTER TABLE [Products] ADD [VariantName] nvarchar(255) NULL;

                IF COL_LENGTH('dbo.Products', 'VariantSku') IS NULL
                    ALTER TABLE [Products] ADD [VariantSku] nvarchar(50) NULL;

                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE [name] = N'IX_Products_ParentProductId'
                      AND [object_id] = OBJECT_ID(N'[dbo].[Products]')
                )
                    CREATE INDEX [IX_Products_ParentProductId] ON [Products] ([ParentProductId]);

                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE [name] = N'IX_Products_VariantSku'
                      AND [object_id] = OBJECT_ID(N'[dbo].[Products]')
                )
                    CREATE UNIQUE INDEX [IX_Products_VariantSku] ON [Products] ([VariantSku])
                    WHERE [VariantSku] IS NOT NULL;

                IF NOT EXISTS (
                    SELECT 1 FROM sys.foreign_keys
                    WHERE [name] = N'FK_Products_ParentProduct'
                      AND [parent_object_id] = OBJECT_ID(N'[dbo].[Products]')
                )
                    ALTER TABLE [Products] ADD CONSTRAINT [FK_Products_ParentProduct]
                    FOREIGN KEY ([ParentProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1 FROM sys.foreign_keys
                    WHERE [name] = N'FK_Products_ParentProduct'
                      AND [parent_object_id] = OBJECT_ID(N'[dbo].[Products]')
                )
                    ALTER TABLE [Products] DROP CONSTRAINT [FK_Products_ParentProduct];

                IF EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE [name] = N'IX_Products_VariantSku'
                      AND [object_id] = OBJECT_ID(N'[dbo].[Products]')
                )
                    DROP INDEX [IX_Products_VariantSku] ON [Products];

                IF EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE [name] = N'IX_Products_ParentProductId'
                      AND [object_id] = OBJECT_ID(N'[dbo].[Products]')
                )
                    DROP INDEX [IX_Products_ParentProductId] ON [Products];

                IF COL_LENGTH('dbo.Products', 'VariantSku') IS NOT NULL
                    ALTER TABLE [Products] DROP COLUMN [VariantSku];

                IF COL_LENGTH('dbo.Products', 'VariantName') IS NOT NULL
                    ALTER TABLE [Products] DROP COLUMN [VariantName];

                IF COL_LENGTH('dbo.Products', 'ParentProductId') IS NOT NULL
                    ALTER TABLE [Products] DROP COLUMN [ParentProductId];
                """);
        }
    }
}
