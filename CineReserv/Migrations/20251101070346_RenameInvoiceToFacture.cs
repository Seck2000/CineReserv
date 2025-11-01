using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CineReserv.Migrations
{
    /// <inheritdoc />
    public partial class RenameInvoiceToFacture : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // MySQL ne supporte pas IF/THEN directement, donc on utilise des procédures stockées temporaires
            // Vérifier et renommer seulement si nécessaire
            migrationBuilder.Sql(@"
                -- Créer une procédure temporaire pour vérifier et renommer
                DROP PROCEDURE IF EXISTS temp_rename_invoices;
                
                CREATE PROCEDURE temp_rename_invoices()
                BEGIN
                    DECLARE invoices_count INT DEFAULT 0;
                    DECLARE factures_count INT DEFAULT 0;
                    
                    SELECT COUNT(*) INTO invoices_count
                    FROM INFORMATION_SCHEMA.TABLES 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'Invoices';
                    
                    SELECT COUNT(*) INTO factures_count
                    FROM INFORMATION_SCHEMA.TABLES 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'Factures';
                    
                    -- Renommer seulement si Invoices existe et Factures n'existe pas
                    IF invoices_count > 0 AND factures_count = 0 THEN
                        SET @sql = 'RENAME TABLE `Invoices` TO `Factures`';
                        PREPARE stmt FROM @sql;
                        EXECUTE stmt;
                        DEALLOCATE PREPARE stmt;
                    END IF;
                    
                    -- Renommer les index s'ils existent encore avec l'ancien nom
                    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.STATISTICS 
                               WHERE TABLE_SCHEMA = DATABASE() 
                               AND TABLE_NAME = 'Factures' 
                               AND INDEX_NAME = 'IX_Invoices_ClientId') THEN
                        ALTER TABLE `Factures` RENAME INDEX `IX_Invoices_ClientId` TO `IX_Factures_ClientId`;
                    END IF;
                    
                    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.STATISTICS 
                               WHERE TABLE_SCHEMA = DATABASE() 
                               AND TABLE_NAME = 'Factures' 
                               AND INDEX_NAME = 'IX_Invoices_FournisseurId') THEN
                        ALTER TABLE `Factures` RENAME INDEX `IX_Invoices_FournisseurId` TO `IX_Factures_FournisseurId`;
                    END IF;
                    
                    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.STATISTICS 
                               WHERE TABLE_SCHEMA = DATABASE() 
                               AND TABLE_NAME = 'Factures' 
                               AND INDEX_NAME = 'IX_Invoices_ReservationId') THEN
                        ALTER TABLE `Factures` RENAME INDEX `IX_Invoices_ReservationId` TO `IX_Factures_ReservationId`;
                    END IF;
                END;
                
                CALL temp_rename_invoices();
                
                DROP PROCEDURE IF EXISTS temp_rename_invoices;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("RENAME TABLE `Factures` TO `Invoices`;");
            migrationBuilder.Sql("ALTER TABLE `Invoices` RENAME INDEX `IX_Factures_ClientId` TO `IX_Invoices_ClientId`;");
            migrationBuilder.Sql("ALTER TABLE `Invoices` RENAME INDEX `IX_Factures_FournisseurId` TO `IX_Invoices_FournisseurId`;");
            migrationBuilder.Sql("ALTER TABLE `Invoices` RENAME INDEX `IX_Factures_ReservationId` TO `IX_Invoices_ReservationId`;");
        }
    }
}
