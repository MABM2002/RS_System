using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.EntityFrameworkCore;
using Rs_system.Data;
using System.Data;
using System.Text;

namespace RS_system.Services
{
    public class GeneradorDocumentoService : IGeneradorDocumentoService
    {
        private readonly ApplicationDbContext _context;

        public GeneradorDocumentoService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Dictionary<string, string>> ObtenerDatosDinamicosAsync(int idDocumento, int idFiltro)
        {
            var configDoc = await _context.Documentos
                .Include(d => d.Detalles)
                .FirstOrDefaultAsync(d => d.IdDocumento == idDocumento);

            if (configDoc == null)
            {
                throw new InvalidOperationException($"No se encontró la configuración del documento con ID {idDocumento}.");
            }

            if (!configDoc.Detalles.Any())
            {
                throw new InvalidOperationException("El documento no tiene detalles configurados para mapear columnas.");
            }

            // Construir la consulta SQL dinámica
            var selectColumns = string.Join(", ", configDoc.Detalles.Select(d => $"{d.ColumnaSql} AS {d.AliasMarcador}"));
            var whereClause = string.IsNullOrWhiteSpace(configDoc.CondicionWhere) ? string.Empty : $"WHERE {configDoc.CondicionWhere}";
            
            var sqlQuery = $"SELECT {selectColumns} FROM {configDoc.TablaFrom} {whereClause}";

            var diccionarioDatos = new Dictionary<string, string>();

            // Ejecutar la consulta con ADO.NET inyectando el parámetro seguro
            using (var connection = _context.Database.GetDbConnection())
            {
                await connection.OpenAsync();
                
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sqlQuery;
                    command.CommandType = CommandType.Text;

                    // Inyección de parámetro anti SQL-Injection
                    var idParam = command.CreateParameter();
                    idParam.ParameterName = "@id";
                    idParam.Value = idFiltro;
                    command.Parameters.Add(idParam);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            foreach (var detalle in configDoc.Detalles)
                            {
                                int colOrdinal;
                                try
                                {
                                    colOrdinal = reader.GetOrdinal(detalle.AliasMarcador);
                                }
                                catch (IndexOutOfRangeException)
                                {
                                    throw new InvalidOperationException($"La columna SQL generada para '{detalle.AliasMarcador}' no se encontró en los resultados de la DB.");
                                }

                                var dbValue = reader.IsDBNull(colOrdinal) ? string.Empty : reader.GetValue(colOrdinal)?.ToString();
                                diccionarioDatos[detalle.AliasMarcador] = dbValue ?? string.Empty;
                            }
                        }
                    }
                }
            }

            return diccionarioDatos;
        }

        public byte[] GenerarWordDesdePlantilla(string rutaPlantilla, Dictionary<string, string> datos)
        {
            // Verificación temprana según requerimiento
            if (!File.Exists(rutaPlantilla))
            {
                throw new FileNotFoundException($"No se encontró la plantilla en la ruta: {rutaPlantilla}", rutaPlantilla);
            }

            using var memoryStream = new MemoryStream();
            
            // Cargar en el MemoryStream para no modificar el disco
            using (var fileStream = new FileStream(rutaPlantilla, FileMode.Open, FileAccess.Read))
            {
                fileStream.CopyTo(memoryStream);
            }
            
            // Necesario para manipular con OpenXML en un MemoryStream
            memoryStream.Position = 0;

            using (var wordDoc = WordprocessingDocument.Open(memoryStream, true))
            {
                var body = wordDoc.MainDocumentPart?.Document.Body;
                if (body == null) return memoryStream.ToArray();

                // Para mitigar con éxito los "Split Runs" operamos a nivel de cada párrafo
                foreach (var paragraph in body.Descendants<Paragraph>())
                {
                    // Obtención de todos los w:t (nodos de texto) dentro del párrafo
                    var textNodes = paragraph.Descendants<Text>().ToList();
                    if (!textNodes.Any()) continue;

                    foreach (var kvp in datos)
                    {
                        string marcador = $"@@@{kvp.Key}";
                        
                        // Iterar hasta que no queden más instancias de este marcador en el párrafo
                        while (true)
                        {
                            // Reconstruimos el texto concatenado para comprobar si existe la cadena cortada
                            var currentTextBuilder = new StringBuilder();
                            foreach (var node in textNodes)
                            {
                                currentTextBuilder.Append(node.Text);
                            }
                            
                            string fullText = currentTextBuilder.ToString();
                            int matchIndex = fullText.IndexOf(marcador, StringComparison.Ordinal);

                            if (matchIndex == -1)
                            {
                                break; // Ya no hay más ocurrencias de este marcador en el párrafo actual
                            }

                            int traversedChars = 0;
                            bool startedReplacing = false;
                            int remainingMatchLength = marcador.Length;

                            foreach (var tNode in textNodes)
                            {
                                int originalNodeLen = tNode.Text?.Length ?? 0;
                                if (originalNodeLen == 0) continue;

                                if (!startedReplacing)
                                {
                                    // Comprobar si el inicio del marcador cae dentro de este nodo (w:t)
                                    if (matchIndex < traversedChars + originalNodeLen)
                                    {
                                        startedReplacing = true;
                                        int replaceStartIndex = matchIndex - traversedChars;
                                        int matchedCharsInThisNode = Math.Min(remainingMatchLength, originalNodeLen - replaceStartIndex);

                                        string textBeforeMatch = tNode.Text!.Substring(0, replaceStartIndex);
                                        string textAfterMatch = tNode.Text.Substring(replaceStartIndex + matchedCharsInThisNode);

                                        // Inyectamos el valor a reemplazar SOLAMENTE en este primer nodo implicado
                                        tNode.Text = textBeforeMatch + kvp.Value + textAfterMatch;
                                        
                                        remainingMatchLength -= matchedCharsInThisNode;
                                    }
                                }
                                else if (remainingMatchLength > 0)
                                {
                                    // Este nodo es una secuela corrompida ("Split Run") del marcador.
                                    int charsToClearInThisNode = Math.Min(remainingMatchLength, originalNodeLen);
                                    
                                    // "Vaciamos" la parte que correspondía a la etiqueta
                                    string textAfterMatch = tNode.Text!.Substring(charsToClearInThisNode);
                                    tNode.Text = textAfterMatch;
                                    
                                    remainingMatchLength -= charsToClearInThisNode;
                                }

                                traversedChars += originalNodeLen;
                            }
                        }
                    }
                }
                
                // Guardar los cambios estructurales realizados
                wordDoc.MainDocumentPart!.Document.Save();
            }

            return memoryStream.ToArray();
        }
    }
}
