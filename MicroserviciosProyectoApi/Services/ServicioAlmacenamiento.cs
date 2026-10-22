namespace Proyecto.Services;

/// <summary>
/// Encapsula el acceso al almacenamiento físico de los archivos subidos por los usuarios.
/// Es la única pieza del backend que toca el sistema de archivos.
/// </summary>
/// <remarks>
/// La ruta raíz sale de la configuración (<c>Almacenamiento:RutaRaiz</c>). En desarrollo
/// apunta a una carpeta local del proyecto y en contenedor apunta a <c>/app/storage</c>,
/// que es donde docker-compose monta el volumen nombrado. Gracias a eso los archivos
/// sobreviven a la recreación del contenedor, tal como lo pide la sección de
/// requerimientos técnicos del PDF.
///
/// En la base de datos nunca se guarda la ruta del cliente ni el nombre original: se guarda
/// un nombre físico generado con un GUID, de modo que el nombre que propone el usuario no
/// puede influir en la ruta del disco (evita ataques de recorrido de rutas).
/// </remarks>
public class ServicioAlmacenamiento
{
    private readonly string _rutaRaiz;

    public ServicioAlmacenamiento(IConfiguration configuracion, IWebHostEnvironment entorno)
    {
        var rutaConfigurada = configuracion["Almacenamiento:RutaRaiz"] ?? "storage";

        // Si la configuración trae una ruta absoluta se respeta; si es relativa se resuelve
        // contra la raíz del contenido de la aplicación.
        _rutaRaiz = Path.IsPathRooted(rutaConfigurada)
            ? rutaConfigurada
            : Path.Combine(entorno.ContentRootPath, rutaConfigurada);

        // Se crea al arrancar para que la primera subida no falle si la carpeta no existe.
        System.IO.Directory.CreateDirectory(_rutaRaiz);
    }

    /// <summary>Ruta física donde se guardan los archivos. Útil para diagnóstico.</summary>
    public string RutaRaiz => _rutaRaiz;

    /// <summary>
    /// Guarda el contenido recibido y devuelve el nombre físico con el que quedó almacenado
    /// (un GUID más la extensión original).
    /// </summary>
    public async Task<string> GuardarAsync(Stream contenido, string extension, CancellationToken cancelacion)
    {
        var nombreAlmacenamiento = $"{Guid.NewGuid():N}{extension}";
        var rutaCompleta = ObtenerRutaCompleta(nombreAlmacenamiento);

        await using var destino = System.IO.File.Create(rutaCompleta);
        await contenido.CopyToAsync(destino, cancelacion);

        return nombreAlmacenamiento;
    }

    /// <summary>Traduce el nombre físico a la ruta absoluta dentro del almacenamiento.</summary>
    public string ObtenerRutaCompleta(string nombreAlmacenamiento) => Path.Combine(_rutaRaiz, nombreAlmacenamiento);

    /// <summary>Indica si el contenido físico de un archivo sigue existiendo.</summary>
    public bool Existe(string nombreAlmacenamiento) => System.IO.File.Exists(ObtenerRutaCompleta(nombreAlmacenamiento));

    /// <summary>
    /// Elimina el archivo físico. Es tolerante: si ya no existe simplemente no hace nada,
    /// para que un borrado repetido no rompa la operación lógica.
    /// </summary>
    public void Eliminar(string nombreAlmacenamiento)
    {
        var rutaCompleta = ObtenerRutaCompleta(nombreAlmacenamiento);

        if (System.IO.File.Exists(rutaCompleta))
            System.IO.File.Delete(rutaCompleta);
    }
}
