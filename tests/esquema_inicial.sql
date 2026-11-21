CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) NOT NULL,
    `ProductVersion` varchar(32) NOT NULL,
    PRIMARY KEY (`MigrationId`)
);

START TRANSACTION;
CREATE TABLE `Usuarios` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `Nombre` varchar(100) NOT NULL,
    `Correo` varchar(255) NOT NULL,
    `Contrasena` varchar(255) NOT NULL,
    `FechaNacimiento` date NOT NULL,
    `EsAdmin` tinyint(1) NOT NULL,
    `EstaActivo` tinyint(1) NOT NULL,
    `CreadoEn` datetime(6) NOT NULL,
    `ActualizadoEn` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`)
);

CREATE TABLE `Directorios` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `Nombre` varchar(200) NOT NULL,
    `PadreId` bigint NULL,
    `PropietarioId` bigint NOT NULL,
    `CreadoEn` datetime(6) NOT NULL,
    `ActualizadoEn` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Directorios_Directorios_PadreId` FOREIGN KEY (`PadreId`) REFERENCES `Directorios` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_Directorios_Usuarios_PropietarioId` FOREIGN KEY (`PropietarioId`) REFERENCES `Usuarios` (`Id`) ON DELETE CASCADE
);

CREATE TABLE `TokensSesion` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `UsuarioId` bigint NOT NULL,
    `Token` varchar(64) NOT NULL,
    `ExpiraEn` datetime(6) NOT NULL,
    `CreadoEn` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_TokensSesion_Usuarios_UsuarioId` FOREIGN KEY (`UsuarioId`) REFERENCES `Usuarios` (`Id`) ON DELETE CASCADE
);

CREATE TABLE `Archivos` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `Nombre` varchar(200) NOT NULL,
    `Extension` varchar(20) NOT NULL,
    `NombreAlmacenamiento` varchar(80) NOT NULL,
    `TamanoBytes` bigint NOT NULL,
    `TipoContenido` varchar(150) NOT NULL,
    `DirectorioId` bigint NOT NULL,
    `PropietarioId` bigint NOT NULL,
    `CreadoEn` datetime(6) NOT NULL,
    `ActualizadoEn` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Archivos_Directorios_DirectorioId` FOREIGN KEY (`DirectorioId`) REFERENCES `Directorios` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_Archivos_Usuarios_PropietarioId` FOREIGN KEY (`PropietarioId`) REFERENCES `Usuarios` (`Id`) ON DELETE CASCADE
);

CREATE INDEX `IX_Archivos_DirectorioId_Nombre` ON `Archivos` (`DirectorioId`, `Nombre`);

CREATE UNIQUE INDEX `IX_Archivos_NombreAlmacenamiento` ON `Archivos` (`NombreAlmacenamiento`);

CREATE INDEX `IX_Archivos_PropietarioId` ON `Archivos` (`PropietarioId`);

CREATE UNIQUE INDEX `IX_Directorios_PadreId_Nombre` ON `Directorios` (`PadreId`, `Nombre`);

CREATE INDEX `IX_Directorios_PropietarioId` ON `Directorios` (`PropietarioId`);

CREATE UNIQUE INDEX `IX_TokensSesion_Token` ON `TokensSesion` (`Token`);

CREATE UNIQUE INDEX `IX_TokensSesion_UsuarioId` ON `TokensSesion` (`UsuarioId`);

CREATE UNIQUE INDEX `IX_Usuarios_Correo` ON `Usuarios` (`Correo`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261021155958_CreacionInicial', '10.0.12');

COMMIT;

