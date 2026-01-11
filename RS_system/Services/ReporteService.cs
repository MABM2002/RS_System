using System;
using System.Data;
using System.Threading.Tasks;
using Rs_system.Data;
using Microsoft.EntityFrameworkCore;

namespace Rs_system.Services
{
    public class ReporteService : IReporteService
    {
        private readonly ApplicationDbContext _context;

        public ReporteService(ApplicationDbContext context)
        {
            _context = context;
        }

        private void AddDateParams(System.Data.Common.DbCommand command, DateOnly inicio, DateOnly fin)
        {
            var p1 = command.CreateParameter();
            p1.ParameterName = "@p_inicio";
            p1.Value = inicio.ToDateTime(TimeOnly.MinValue);
            p1.DbType = DbType.Date;
            command.Parameters.Add(p1);

            var p2 = command.CreateParameter();
            p2.ParameterName = "@p_fin";
            p2.Value = fin.ToDateTime(TimeOnly.MaxValue);
            p2.DbType = DbType.Date;
            command.Parameters.Add(p2);
        }
    }
}