namespace Proyecto.Services;

public class ServicioAlmacenamiento
{
    private readonly string _rutaRaiz;

    public ServicioAlmacenamiento(IConfiguration configuracion, IWebHostEnvironment entorno)
    {
        var rutaConfigurada = configuracion["Almacenamiento:RutaRaiz"] ?? "storage";

        _rutaRaiz = Path.IsPathRooted(rutaConfigurada)
            ? rutaConfigurada
            : Path.Combine(entorno.ContentRootPath, rutaConfigurada);

        System.IO.Directory.CreateDirectory(_rutaRaiz);
    }

    public string RutaRaiz => _rutaRaiz;

    public async Task<string> GuardarAsync(Stream contenido, string extension, CancellationToken cancelacion)
    {
        var nombreAlmacenamiento = $"{Guid.NewGuid():N}{extension}";
        var rutaCompleta = ObtenerRutaCompleta(nombreAlmacenamiento);

        await using var destino = System.IO.File.Create(rutaCompleta);
        await contenido.CopyToAsync(destino, cancelacion);

        return nombreAlmacenamiento;
    }

    public string ObtenerRutaCompleta(string nombreAlmacenamiento) => Path.Combine(_rutaRaiz, nombreAlmacenamiento);

    public bool Existe(string nombreAlmacenamiento) => System.IO.File.Exists(ObtenerRutaCompleta(nombreAlmacenamiento));

    public void Eliminar(string nombreAlmacenamiento)
    {
        var rutaCompleta = ObtenerRutaCompleta(nombreAlmacenamiento);

        if (System.IO.File.Exists(rutaCompleta))
            System.IO.File.Delete(rutaCompleta);
    }
}
