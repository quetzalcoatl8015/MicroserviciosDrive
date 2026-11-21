START TRANSACTION;
CREATE INDEX `IX_Directorios_PadreId` ON `Directorios` (`PadreId`);

DROP INDEX IX_Directorios_PadreId_Nombre ON Directorios;

ALTER TABLE `Usuarios` ADD `EquipoId` bigint NOT NULL DEFAULT 0;

ALTER TABLE `Usuarios` ADD `NivelAdmin` int NOT NULL DEFAULT 0;

ALTER TABLE `Directorios` ADD `EquipoId` bigint NOT NULL DEFAULT 0;

CREATE TABLE `Equipos` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `Nombre` varchar(100) NOT NULL,
    `CodigoRegistro` varchar(64) NOT NULL,
    `CreadoEn` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`)
);

CREATE TABLE `ContenidosCompartidos` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `EquipoDestinoId` bigint NOT NULL,
    `ArchivoId` bigint NULL,
    `DirectorioId` bigint NULL,
    `CreadoEn` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `CK_Compartido_UnRecurso` CHECK ((`ArchivoId` IS NULL) <> (`DirectorioId` IS NULL)),
    CONSTRAINT `FK_ContenidosCompartidos_Archivos_ArchivoId` FOREIGN KEY (`ArchivoId`) REFERENCES `Archivos` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_ContenidosCompartidos_Directorios_DirectorioId` FOREIGN KEY (`DirectorioId`) REFERENCES `Directorios` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_ContenidosCompartidos_Equipos_EquipoDestinoId` FOREIGN KEY (`EquipoDestinoId`) REFERENCES `Equipos` (`Id`) ON DELETE CASCADE
);

CREATE INDEX `IX_Usuarios_EquipoId` ON `Usuarios` (`EquipoId`);

CREATE UNIQUE INDEX `IX_Directorios_EquipoId_PadreId_Nombre` ON `Directorios` (`EquipoId`, `PadreId`, `Nombre`);

CREATE INDEX `IX_ContenidosCompartidos_ArchivoId` ON `ContenidosCompartidos` (`ArchivoId`);

CREATE INDEX `IX_ContenidosCompartidos_DirectorioId` ON `ContenidosCompartidos` (`DirectorioId`);

CREATE UNIQUE INDEX `IX_ContenidosCompartidos_EquipoDestinoId_ArchivoId` ON `ContenidosCompartidos` (`EquipoDestinoId`, `ArchivoId`);

CREATE UNIQUE INDEX `IX_ContenidosCompartidos_EquipoDestinoId_DirectorioId` ON `ContenidosCompartidos` (`EquipoDestinoId`, `DirectorioId`);

CREATE UNIQUE INDEX `IX_Equipos_CodigoRegistro` ON `Equipos` (`CodigoRegistro`);

CREATE UNIQUE INDEX `IX_Equipos_Nombre` ON `Equipos` (`Nombre`);

INSERT INTO Equipos (Id, Nombre, CodigoRegistro, CreadoEn) VALUES (1, 'Equipo principal', REPLACE(UUID(), '-', ''), UTC_TIMESTAMP(6));

UPDATE Usuarios SET EquipoId = 1, NivelAdmin = CASE WHEN EsAdmin = 1 THEN 1 ELSE 0 END;

UPDATE Directorios SET EquipoId = 1;

UPDATE Usuarios u JOIN (SELECT MIN(Id) AS Id FROM Usuarios WHERE EsAdmin = 1 AND EstaActivo = 1) primero ON primero.Id = u.Id SET u.NivelAdmin = 2;

ALTER TABLE `Directorios` ADD CONSTRAINT `FK_Directorios_Equipos_EquipoId` FOREIGN KEY (`EquipoId`) REFERENCES `Equipos` (`Id`) ON DELETE RESTRICT;

ALTER TABLE `Usuarios` ADD CONSTRAINT `FK_Usuarios_Equipos_EquipoId` FOREIGN KEY (`EquipoId`) REFERENCES `Equipos` (`Id`) ON DELETE RESTRICT;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261118013915_EquiposYCompartidos', '10.0.12');

COMMIT;

