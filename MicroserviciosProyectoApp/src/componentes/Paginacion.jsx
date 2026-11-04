import Button from 'react-bootstrap/Button';
import Form from 'react-bootstrap/Form';
import Row from 'react-bootstrap/Row';
import Col from 'react-bootstrap/Col';

/**
 * Controles de paginación reutilizables.
 *
 * Los requerimientos 2.4 y 3.5 del PDF piden que las listas estén paginadas y que
 * el usuario pueda elegir el tamaño de página. Este componente se usa en las tres
 * listas del sistema (usuarios, carpetas y archivos) para no repetir la misma
 * lógica en cada pantalla.
 *
 * La paginación se calcula siempre sobre "totalRegistros", que es el dato que
 * devuelve el backend dentro de { elementos, pagina, tamanoPagina, totalRegistros }.
 *
 * @param {number}   pagina          Página actual, empieza en 1.
 * @param {number}   totalRegistros  Total de elementos que cumplen los filtros.
 * @param {number}   tamanoPagina    Cuántos elementos se muestran por página.
 * @param {Function} onCambiarPagina Recibe el número de la página nueva.
 * @param {Function} onCambiarTamano Recibe el tamaño de página nuevo.
 */
function Paginacion({
  pagina,
  totalRegistros,
  tamanoPagina,
  onCambiarPagina,
  onCambiarTamano,
}) {
  /**
   * Opciones de tamaño de página. El backend acepta hasta 100 por petición,
   * así que no se ofrece ningún valor mayor.
   */
  const opcionesTamano = [5, 10, 25, 50, 100];

  // Al menos hay una página, aunque no haya registros, para que la barra no desaparezca.
  const totalPaginas = Math.max(1, Math.ceil(totalRegistros / tamanoPagina));

  // Rango de elementos que se están viendo, para el texto "Mostrando X a Y de Z".
  const desde = totalRegistros === 0 ? 0 : (pagina - 1) * tamanoPagina + 1;
  const hasta = Math.min(pagina * tamanoPagina, totalRegistros);

  /**
   * Números de página que se van a dibujar.
   *
   * Mostrar todos los números no es viable cuando hay cientos de páginas, así que
   * se calcula una ventana de cinco páginas alrededor de la actual.
   *
   * El arreglo se construye con Array.from en una sola expresión en lugar de con un
   * ciclo que rellena una lista: así ningún valor se modifica después de usarse.
   */
  const inicio = Math.max(1, Math.min(pagina - 2, totalPaginas - 4));
  const fin = Math.min(totalPaginas, inicio + 4);
  const numeros = Array.from({ length: fin - inicio + 1 }, (_, posicion) => inicio + posicion);

  return (
    <Row className="align-items-center g-2 mt-2">
      <Col xs="auto">
        <Form.Select
          size="sm"
          value={tamanoPagina}
          // Al cambiar el tamaño de página se vuelve a la primera, porque el número
          // de página anterior puede quedar fuera de rango.
          onChange={(evento) => {
            onCambiarTamano(Number(evento.target.value));
            onCambiarPagina(1);
          }}
        >
          {opcionesTamano.map((opcion) => (
            <option key={opcion} value={opcion}>
              {opcion} por página
            </option>
          ))}
        </Form.Select>
      </Col>

      <Col className="text-muted small">
        Mostrando {desde} a {hasta} de {totalRegistros} registros
      </Col>

      <Col xs="auto" className="ms-auto">
        <Button
          size="sm"
          variant="outline-secondary"
          className="me-1"
          disabled={pagina <= 1}
          onClick={() => onCambiarPagina(pagina - 1)}
        >
          Anterior
        </Button>
        {numeros.map((numero) => (
          <Button
            key={numero}
            size="sm"
            variant={numero === pagina ? 'primary' : 'outline-secondary'}
            className="me-1"
            onClick={() => onCambiarPagina(numero)}
          >
            {numero}
          </Button>
        ))}
        <Button
          size="sm"
          variant="outline-secondary"
          className="ms-1"
          disabled={pagina >= totalPaginas}
          onClick={() => onCambiarPagina(pagina + 1)}
        >
          Siguiente
        </Button>
      </Col>
    </Row>
  );
}

export default Paginacion;
