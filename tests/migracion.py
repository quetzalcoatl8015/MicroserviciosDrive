#!/usr/bin/env python3
"""Valida el salto desde el esquema original con datos en una BD temporal propia."""
from pathlib import Path
import os
import subprocess
import uuid

CONTENEDOR = os.environ.get('MYSQL_CONTAINER', 'microservicios-equipos-pruebas-mysql-1')
BD = 'MigracionPrueba_' + uuid.uuid4().hex
RAIZ = Path(__file__).resolve().parent


def sql(texto):
    r = subprocess.run(['docker', 'exec', '-i', CONTENEDOR, 'sh', '-c',
                        'export MYSQL_PWD="$MYSQL_ROOT_PASSWORD"; exec mysql -uroot --batch --skip-column-names'],
                       input=texto, text=True, capture_output=True, check=True)
    return r.stdout.strip()


def main():
    sql(f'CREATE DATABASE `{BD}` CHARACTER SET utf8mb4;')
    try:
        sql(f'USE `{BD}`;\n' + (RAIZ / 'esquema_inicial.sql').read_text(encoding='utf-8-sig'))
        sql(f'''USE `{BD}`;
        INSERT INTO Usuarios (Id, Nombre, Correo, Contrasena, FechaNacimiento, EsAdmin, EstaActivo, CreadoEn, ActualizadoEn)
        VALUES (10, 'Admin original', 'anterior@prueba.local', 'hash-intacto', '1990-01-01', 1, 1, NOW(), NOW()),
        (20, 'Segundo admin', 'segundo@prueba.local', 'otro-hash', '1991-01-01', 1, 1, NOW(), NOW()),
        (30, 'Usuario original', 'usuario@prueba.local', 'tercer-hash', '1992-01-01', 0, 1, NOW(), NOW());
        INSERT INTO Directorios (Id, Nombre, PadreId, PropietarioId, CreadoEn, ActualizadoEn)
        VALUES (100, 'Raíz anterior', NULL, 10, NOW(), NOW()), (101, 'Hija anterior', 100, 30, NOW(), NOW());
        INSERT INTO Archivos (Id, Nombre, Extension, NombreAlmacenamiento, TamanoBytes, TipoContenido, DirectorioId, PropietarioId, CreadoEn, ActualizadoEn)
        VALUES (500, 'Anterior', '.pdf', 'original-inmutable.pdf', 123, 'application/pdf', 101, 30, NOW(), NOW());
        INSERT INTO TokensSesion (Id, UsuarioId, Token, ExpiraEn, CreadoEn) VALUES (1, 10, REPEAT('a',64), DATE_ADD(NOW(), INTERVAL 1 DAY), NOW());
        ''')
        sql(f'USE `{BD}`;\n' + (RAIZ / 'migracion_equipos.sql').read_text(encoding='utf-8-sig'))
        assert sql(f'USE `{BD}`; SELECT CONCAT(Id, ":", EquipoId, ":", NivelAdmin, ":", Contrasena) FROM Usuarios ORDER BY Id;') == '10:1:2:hash-intacto\n20:1:1:otro-hash\n30:1:0:tercer-hash'
        assert sql(f'USE `{BD}`; SELECT CONCAT(Id, ":", EquipoId, ":", IFNULL(PadreId,0)) FROM Directorios ORDER BY Id;') == '100:1:0\n101:1:100'
        assert sql(f'USE `{BD}`; SELECT CONCAT(Id, ":", DirectorioId, ":", NombreAlmacenamiento, ":", TamanoBytes) FROM Archivos;') == '500:101:original-inmutable.pdf:123'
        assert sql(f'USE `{BD}`; SELECT COUNT(*) FROM TokensSesion;') == '1'
        assert sql(f'USE `{BD}`; SELECT COUNT(*) FROM Equipos WHERE Id=1 AND Nombre="Equipo principal" AND LENGTH(CodigoRegistro)=32;') == '1'
        assert sql(f'USE `{BD}`; SELECT COUNT(*) FROM __EFMigrationsHistory;') == '2'
        print('OK: migración conserva usuarios, contraseñas, tokens, árbol y referencias de archivos; asigna equipo 1 y niveles 0/1/2.')
    finally:
        # Solo se elimina la BD aleatoria creada por esta ejecución.
        sql(f'DROP DATABASE `{BD}`;')


if __name__ == '__main__':
    main()
