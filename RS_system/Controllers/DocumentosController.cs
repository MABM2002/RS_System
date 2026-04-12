using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rs_system.Data;
using RS_system.Services;

namespace RS_system.Controllers
{
    [Authorize]
    public class DocumentosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IGeneradorDocumentoService _generadorService;
        private readonly ILogger<DocumentosController> _logger;
        private readonly IWebHostEnvironment _env;

        public DocumentosController(
            ApplicationDbContext context,
            IGeneradorDocumentoService generadorService,
            ILogger<DocumentosController> logger,
            IWebHostEnvironment env)
        {
            _context = context;
            _generadorService = generadorService;
            _logger = logger;
            _env = env;
        }

        [HttpGet]
        public async Task<IActionResult> Descargar(int idDocumento, int idFiltro)
        {
            try
            {
                // Buscamos el documento para obtener su nombre comun y la ruta de la plantilla fisica
                var documento = await _context.Documentos.FirstOrDefaultAsync(d => d.IdDocumento == idDocumento);
                
                if (documento == null)
                {
                    return NotFound("El documento configurado no existe en el sistema.");
                }

                // Delegar al servicio la creacion del bloque dinamico SQL y llenado de variables
                var datosDinamicos = await _generadorService.ObtenerDatosDinamicosAsync(idDocumento, idFiltro);

                // Obtener la ubicacion fisica del archivo de word. 
                // Suponemos que Documento.RutaPlantilla contiene el nombre del archivo (ej "recibo.docx") 
                // y que se ubica fisicamente en wwwroot/Plantillas/
                var rutaPlantillaFisica = Path.Combine(_env.WebRootPath, "Plantillas", documento.RutaPlantilla);

                // Llamar al metodo inyectado para operar el OpenXML sin afectar el disco
                var fileBytes = _generadorService.GenerarWordDesdePlantilla(rutaPlantillaFisica, datosDinamicos);

                string fileNameSalida = $"{documento.NombreComun}_{idFiltro}_{DateTime.Now:yyyyMMdd_HHmmss}.docx";
                
                // Retornar archivo File con MIME correspondiente a word
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", fileNameSalida);
            }
            catch (FileNotFoundException ex)
            {
                _logger.LogError(ex, "Plantilla de Word no hallada en la ruta estipulada.");
                return NotFound("La plantilla base conectada a este documento no se encuentra en el servidor.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crítico durante la generación dinámica del documento de Word.");
                return BadRequest($"Ocurrió un error general procesando la creación del documento: {ex.Message}");
            }
        }
    }
}
