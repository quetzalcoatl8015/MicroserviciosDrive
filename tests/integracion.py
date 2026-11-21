#!/usr/bin/env python3
"""Pruebas HTTP contra una instalación desechable. Crea datos propios; no borra datos existentes."""
import json
import os
import uuid
from urllib.request import Request, urlopen
from urllib.error import HTTPError

BASE = os.environ.get('API_URL', 'http://localhost:15339/api').rstrip('/')
SUFIJO = uuid.uuid4().hex[:10]
PASSWORD = 'PruebasEquipos2026!'
comprobaciones = 0


def pedir(metodo, ruta, datos=None, token=None, esperado=200, multipart=None):
    global comprobaciones
    headers = {}
    if token:
        headers['Authorization'] = f'Bearer {token}'
    if multipart is not None:
        cuerpo, tipo = multipart
        headers['Content-Type'] = tipo
    elif datos is not None:
        cuerpo = json.dumps(datos).encode()
        headers['Content-Type'] = 'application/json'
    else:
        cuerpo = None
    req = Request(BASE + ruta, data=cuerpo, headers=headers, method=metodo)
    try:
        with urlopen(req, timeout=30) as r:
            estado, contenido = r.status, r.read()
    except HTTPError as e:
        estado, contenido = e.code, e.read()
    assert estado == esperado, f'{metodo} {ruta}: esperado {esperado}, obtenido {estado}: {contenido[:800]!r}'
    comprobaciones += 1
    if not contenido:
        return None
    try:
        return json.loads(contenido)
    except (ValueError, UnicodeDecodeError):
        return contenido


def login(correo, clave=PASSWORD):
    return pedir('POST', '/autenticacion/iniciar-sesion', {'correo': correo, 'contrasena': clave})


def subir(token, directorio, esperado=201, contenido=b'contenido privado del equipo', nombre='informe.txt'):
    boundary = 'Prueba' + uuid.uuid4().hex
    body = (f'--{boundary}\r\nContent-Disposition: form-data; name="DirectorioId"\r\n\r\n{directorio}\r\n'
            f'--{boundary}\r\nContent-Disposition: form-data; name="Archivo"; filename="{nombre}"\r\n'
            'Content-Type: text/plain\r\n\r\n').encode() + contenido + f'\r\n--{boundary}--\r\n'.encode()
    return pedir('POST', '/archivos', token=token, esperado=esperado, multipart=(body, f'multipart/form-data; boundary={boundary}'))


def main():
    root = login(os.environ.get('ADMIN_EMAIL', 'admin@proyectom.com'), os.environ.get('ADMIN_PASSWORD', 'AdminPruebas2026!'))['tokenAcceso']
    yo = pedir('GET', '/autenticacion/yo', token=root)
    assert yo['nivelAdmin'] == 2
    equipos = {}
    for key in ['A', 'B', 'C']:
        equipos[key] = pedir('POST', '/equipos', {'nombre': f'Equipo {key} {SUFIJO}'}, root, 201)
    a, b, c = [equipos[k]['id'] for k in ['A', 'B', 'C']]

    def crear(nombre, equipo, nivel):
        modelo = {'nombre': nombre, 'correo': f'{nombre}-{SUFIJO}@prueba.local', 'contrasena': PASSWORD,
                  'confirmarContrasena': PASSWORD, 'fechaNacimiento': '1995-05-10', 'equipoId': equipo, 'nivelAdmin': nivel}
        u = pedir('POST', '/usuarios', modelo, root, 201)
        return u, login(u['correo'])['tokenAcceso'], modelo

    admina, ta, ma = crear('admina', a, 1)
    adminb, tb, mb = crear('adminb', b, 1)
    ua, tua, mua = crear('usuarioa', a, 0)
    ua2, tua2, _ = crear('usuarioa2', a, 0)
    ub, tub, mub = crear('usuariob', b, 0)
    global_a, tg, mg = crear('globala', a, 2)
    print('Usuarios, equipos y dos grados de administrador: OK')
    pedir('POST', '/equipos', {'nombre': 'Prohibido'}, ta, 403)
    pedir('POST', '/equipos', {'nombre': 'Prohibido'}, tua, 403)
    pedir('GET', '/usuarios', token=tua, esperado=403)
    assert {e['id'] for e in pedir('GET', '/equipos', token=tua)} == {a}
    assert 'codigoRegistro' not in pedir('GET', '/equipos', token=tua)[0]
    lista = pedir('GET', '/usuarios?tamanoPagina=100', token=ta)['elementos']
    assert all(u['equipoId'] == a for u in lista)
    pedir('GET', f'/usuarios/{ub["id"]}', token=ta, esperado=404)
    pedir('GET', f'/usuarios?equipoId={b}', token=ta, esperado=403)
    pedir('PUT', f'/usuarios/{ub["id"]}', mub, ta, 404)
    pedir('DELETE', f'/usuarios/{ub["id"]}', token=ta, esperado=404)
    pedir('PATCH', f'/usuarios/{ub["id"]}/estado', {'estaActivo': False}, ta, 404)
    pedir('DELETE', f'/usuarios/{ub["id"]}/token-sesion', token=ta, esperado=404)
    pedir('PUT', f'/usuarios/{global_a["id"]}', mg, ta, 403)
    pedir('PATCH', f'/usuarios/{global_a["id"]}/estado', {'estaActivo': False}, ta, 403)
    pedir('DELETE', f'/usuarios/{global_a["id"]}/token-sesion', token=ta, esperado=403)
    pedir('DELETE', f'/usuarios/{global_a["id"]}', token=ta, esperado=403)
    pedir('POST', '/usuarios', {**mua, 'correo': f'escala-{SUFIJO}@prueba.local', 'nivelAdmin': 2}, ta, 403)
    pedir('PUT', f'/usuarios/{ua["id"]}', {**mua, 'nivelAdmin': 2}, ta, 403)
    pedir('PUT', f'/usuarios/{ua["id"]}', {**mua, 'equipoId': b}, ta, 403)

    da = pedir('POST', '/directorios', {'nombre': 'Documentos', 'equipoId': a}, tua, 201)
    db = pedir('POST', '/directorios', {'nombre': 'Documentos', 'equipoId': b}, tub, 201)
    pedir('POST', '/directorios', {'nombre': 'Documentos', 'equipoId': a}, tua, 409)
    hijo = pedir('POST', '/directorios', {'nombre': 'Subcarpeta', 'padreId': da['id']}, tua, 201)
    nieto = pedir('POST', '/directorios', {'nombre': 'Profunda', 'padreId': hijo['id']}, tua, 201)
    hermano = pedir('POST', '/directorios', {'nombre': 'Privada', 'equipoId': a}, tua, 201)
    fa = subir(tua, hijo['id'])
    fb = subir(tub, db['id'])
    profundo = subir(tua, nieto['id'])
    privado = subir(tua, hermano['id'])
    assert fa['extension'] == '.txt'
    assert len(pedir('GET', '/directorios', token=tua)['elementos']) == 2
    for token in [tub, tb]:
        pedir('GET', f'/directorios/{da["id"]}', token=token, esperado=404)
        pedir('GET', f'/directorios?padreId={da["id"]}', token=token, esperado=404)
        pedir('GET', f'/directorios?equipoId={a}', token=token, esperado=403)
        pedir('GET', f'/archivos?directorioId={hijo["id"]}', token=token, esperado=404)
        pedir('GET', f'/archivos/{fa["id"]}', token=token, esperado=404)
        pedir('GET', f'/archivos/{fa["id"]}/descargar', token=token, esperado=404)
        pedir('PATCH', f'/archivos/{fa["id"]}', {'nombre': 'Ataque'}, token, 404)
        pedir('DELETE', f'/archivos/{fa["id"]}', token=token, esperado=404)
        pedir('PATCH', f'/directorios/{da["id"]}', {'nombre': 'Ataque'}, token, 404)
        pedir('DELETE', f'/directorios/{da["id"]}', token=token, esperado=404)
        pedir('POST', '/directorios', {'nombre': 'Ataque', 'padreId': da['id']}, token, 404)
        subir(token, hijo['id'], 400)
    pedir('POST', '/directorios', {'nombre': 'Mal', 'equipoId': b, 'padreId': da['id']}, root, 400)
    pedir('POST', '/directorios', {'nombre': 'Mal', 'equipoId': b}, tua, 403)
    pedir('PATCH', f'/archivos/{fa["id"]}', {'nombre': 'Otro'}, tua2, 403)
    pedir('DELETE', f'/directorios/{da["id"]}', token=tua, esperado=400)
    pedir('PATCH', f'/archivos/{fa["id"]}', {'nombre': 'Renombrado'}, ta, 204)
    assert pedir('GET', f'/archivos/{fa["id"]}', token=tua)['nombreCompleto'] == 'Renombrado.txt'
    assert pedir('GET', f'/archivos/{fa["id"]}/descargar', token=tua2) == b'contenido privado del equipo'
    subir(tua, hijo['id'], 400, b'x' * (10 * 1024 * 1024 + 1))
    print('Aislamiento de lectura, escritura, usuarios e IDs directos: OK')

    modelo = {'directorioId': hijo['id'], 'equipoDestinoId': b}
    pedir('POST', '/compartidos', modelo, ta, 403)
    pedir('POST', '/compartidos', modelo, tua, 403)
    pedir('POST', '/compartidos', {'equipoDestinoId': b}, root, 400)
    pedir('POST', '/compartidos', {**modelo, 'archivoId': fa['id']}, root, 400)
    pedir('POST', '/compartidos', {**modelo, 'equipoDestinoId': a}, root, 400)
    compartir = pedir('POST', '/compartidos', modelo, root, 201)
    pedir('POST', '/compartidos', modelo, root, 409)
    for token in [tub, tb]:
        pedir('GET', f'/directorios/{hijo["id"]}', token=token)
        pedir('GET', f'/directorios/{nieto["id"]}', token=token)
        assert pedir('GET', f'/directorios?padreId={hijo["id"]}', token=token)['elementos'][0]['id'] == nieto['id']
        pedir('GET', f'/archivos/{fa["id"]}/descargar', token=token)
        pedir('GET', f'/archivos/{profundo["id"]}/descargar', token=token)
        pedir('GET', f'/directorios/{da["id"]}', token=token, esperado=404)
        pedir('GET', f'/archivos/{privado["id"]}/descargar', token=token, esperado=404)
        pedir('PATCH', f'/archivos/{fa["id"]}', {'nombre': 'Mal'}, token, 404)
        pedir('DELETE', f'/directorios/{hijo["id"]}', token=token, esperado=404)
        subir(token, hijo['id'], 400)
        pedir('DELETE', f'/compartidos/{compartir["id"]}', token=token, esperado=403)
    futuro = subir(tua, nieto['id'])
    pedir('GET', f'/archivos/{futuro["id"]}/descargar', token=tub)
    assert len(pedir('GET', '/compartidos', token=tub)) == 1
    pedir('GET', f'/compartidos?equipoId={a}', token=tub, esperado=403)
    pedir('DELETE', f'/compartidos/{compartir["id"]}', token=root, esperado=204)
    pedir('GET', f'/archivos/{fa["id"]}/descargar', token=tub, esperado=404)
    compartir = pedir('POST', '/compartidos', {'archivoId': fa['id'], 'equipoDestinoId': b}, root, 201)
    pedir('GET', f'/archivos/{fa["id"]}/descargar', token=tub)
    pedir('GET', f'/directorios/{hijo["id"]}', token=tub, esperado=404)
    pedir('GET', f'/archivos/{profundo["id"]}/descargar', token=tub, esperado=404)
    pedir('GET', f'/archivos?directorioId={hijo["id"]}', token=tub, esperado=404)
    pedir('DELETE', f'/archivos/{fa["id"]}', token=ta, esperado=204)
    assert pedir('GET', '/compartidos', token=tub) == []
    print('Compartir archivos y subárboles, lectura exclusiva y revocación: OK')

    # El rol y equipo se releen de la base en cada solicitud, incluso con JWT anteriores.
    pedir('PUT', f'/usuarios/{global_a["id"]}', {**mg, 'nivelAdmin': 1}, root, 204)
    pedir('POST', '/equipos', {'nombre': 'No permitido'}, tg, 403)
    pedir('GET', f'/directorios/{db["id"]}', token=tg, esperado=404)
    pedir('PUT', f'/usuarios/{ua["id"]}', {**mua, 'equipoId': c}, root, 204)
    assert pedir('GET', '/autenticacion/yo', token=tua)['equipoId'] == c
    pedir('GET', f'/archivos/{profundo["id"]}/descargar', token=tua, esperado=404)
    # El contenido de A permanece en A aunque su creador cambie de equipo.
    pedir('GET', f'/archivos/{profundo["id"]}/descargar', token=ta)
    pedir('DELETE', f'/usuarios/{ua["id"]}', token=root, esperado=409)
    pedir('PATCH', f'/usuarios/{ub["id"]}/estado', {'estaActivo': False}, tb, 204)
    pedir('GET', '/directorios', token=tub, esperado=401)
    pedir('PATCH', f'/usuarios/{ub["id"]}/estado', {'estaActivo': True}, tb, 204)

    # Autoregistro: sin equipo, inactivo y usuario común hasta que un administrador lo active.
    registro = {'nombre': 'Pendiente', 'correo': f'pendiente-{SUFIJO}@prueba.local', 'contrasena': PASSWORD,
                'confirmarContrasena': PASSWORD, 'fechaNacimiento': '1998-02-03'}
    pendiente = pedir('POST', '/autenticacion/registro', registro, esperado=201)
    info = pedir('GET', f'/usuarios/{pendiente["id"]}', token=ta)
    assert info['equipoId'] is None and not info['esAdmin'] and info['nivelAdmin'] == 0 and not info['estaActivo']
    pedir('POST', '/autenticacion/iniciar-sesion', {'correo': registro['correo'], 'contrasena': PASSWORD}, esperado=401)
    # No puede activarse sin equipo y un administrador ajeno no puede quedárselo.
    pedir('PATCH', f'/usuarios/{pendiente["id"]}/estado', {'estaActivo': True}, ta, 400)
    pedir('PATCH', f'/usuarios/{pendiente["id"]}/estado', {'estaActivo': True, 'equipoId': b}, ta, 403)
    # El administrador de A la activa y la incorpora a su equipo.
    pedir('PATCH', f'/usuarios/{pendiente["id"]}/estado', {'estaActivo': True, 'equipoId': a}, ta, 204)
    assert pedir('GET', f'/usuarios/{pendiente["id"]}', token=ta)['equipoId'] == a
    tokens = login(registro['correo'])
    pedir('GET', '/directorios', token=tokens['tokenRefresco'], esperado=401)
    nuevos = pedir('POST', '/autenticacion/renovar', {'tokenRefresco': tokens['tokenRefresco']})
    pedir('POST', '/autenticacion/renovar', {'tokenRefresco': tokens['tokenRefresco']}, esperado=401)
    pedir('POST', '/autenticacion/cerrar-sesion', token=nuevos['tokenAcceso'], esperado=204)
    pedir('POST', '/autenticacion/renovar', {'tokenRefresco': nuevos['tokenRefresco']}, esperado=401)
    print('Cambio de equipo, degradación inmediata, registro y renovación: OK')
    # Borrado recursivo elimina también concesiones de acceso.
    pedir('POST', '/compartidos', modelo, root, 201)
    pedir('DELETE', f'/directorios/{da["id"]}', token=ta, esperado=204)
    pedir('GET', f'/archivos/{profundo["id"]}', token=root, esperado=404)
    assert pedir('GET', '/compartidos', token=tb) == []
    pedir('DELETE', f'/usuarios/{yo["id"]}', token=root, esperado=400)
    print(f'OK: {comprobaciones} respuestas HTTP verificadas; aislamiento, roles y compartidos correctos.')


if __name__ == '__main__':
    main()
