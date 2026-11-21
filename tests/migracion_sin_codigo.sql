START TRANSACTION;

DROP INDEX `IX_Equipos_CodigoRegistro` ON `Equipos`;

ALTER TABLE `Equipos` DROP COLUMN `CodigoRegistro`;

ALTER TABLE `Usuarios` MODIFY `EquipoId` bigint NULL;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261121221734_AutoregistroSinCodigoDeEquipo', '10.0.12');

COMMIT;
