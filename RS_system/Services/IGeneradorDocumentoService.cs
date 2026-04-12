namespace RS_system.Services
{
    public interface IGeneradorDocumentoService
    {
        /// <summary>
        /// Obtiene un diccionario de datos dinámicos resolviendo las consultas de las columnas mapeadas en los detalles del Documento.
        /// </summary>
        /// <param name="idDocumento">El ID del Documento configurado.</param>
        /// <param name="idFiltro">El ID para filtrar (por ejemplo el ID del diezmo, o miembro).</param>
        /// <returns>Diccionario con Alias y su respectivo Valor rescatado de la base de datos.</returns>
        Task<Dictionary<string, string>> ObtenerDatosDinamicosAsync(int idDocumento, int idFiltro);

        /// <summary>
        /// Genera un arreglo de bytes correspondiente al documento Word rellenado.
        /// Reemplaza las etiquetas @@@Key encontradas en el documento por el contenido del diccionario.
        /// Emplea OpenXML respetando etiquetas cortadas (Split Runs) y validando la existencia de la plantilla.
        /// </summary>
        /// <param name="rutaPlantilla">Ruta absoluta hacia el archivo de Microsoft Word (.docx).</param>
        /// <param name="datos">Diccionario que contiene los valores de reemplazo.</param>
        /// <returns>Buffer de bytes del nuevo documento generado.</returns>
        byte[] GenerarWordDesdePlantilla(string rutaPlantilla, Dictionary<string, string> datos);
    }
}
