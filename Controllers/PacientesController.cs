using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TuNombreDeProyecto.Models;
using WebAPIExamen.Data;
using WebAPIExamen.Models;

namespace WebAPIExamen.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PacientesController : ControllerBase
    {
        private readonly AppDbContext _context;

        // REQUERIMIENTO B: Arreglo estático de médicos autorizados
        private static readonly string[] MedicosAutorizados =
        {
            "MED-1010", "MED-2020", "MED-3030", "MED-4040", "MED-5050"
        };

        public PacientesController(AppDbContext context)
        {
            _context = context;
        }

        // POST: api/Pacientes
        [HttpPost]
        public async Task<ActionResult<Paciente>> RegistrarPaciente([FromBody] Paciente paciente)
        {
            // 1. Validación de Autorización
            if (!MedicosAutorizados.Contains(paciente.MedicoResponsable))
            {
                return Unauthorized(new { error = "Médico no autorizado. El carnet no coincide con los registros oficiales." });
            }

            // 2. Validación de Capacidad Crítica
            if (paciente.NivelGravedad == 5)
            {
                int criticosEnEspera = await _context.pacientes_13449
                    .CountAsync(p => p.NivelGravedad == 5 && p.Estado == "En espera");

                if (criticosEnEspera >= 5)
                {
                    return BadRequest(new { error = "Capacidad máxima alcanzada. Redirección inmediata a otro hospital sugerida" });
                }
            }

            // 3. Generación del ID de Paciente (PAC-2026-XXX)
            int totalPacientes = await _context.pacientes_13449.CountAsync();
            paciente.IdPaciente = $"PAC-2026-{(totalPacientes + 1).ToString("D3")}";

            paciente.FechaIngreso = DateTime.Now;

            _context.pacientes_13449.Add(paciente);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPacientes), new { id = paciente.IdPaciente }, paciente);
        }

        // GET: api/Pacientes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Paciente>>> GetPacientes()
        {
            // Extraer TODOS los pacientes sin usar ORDER BY de SQL
            var listaPacientes = await _context.pacientes_13449.ToListAsync();
            var pacientes = listaPacientes.ToArray();
            int n = pacientes.Length;

            // Algoritmo de Burbuja
            for (int i = 0; i < n - 1; i++)
            {
                for (int j = 0; j < n - i - 1; j++)
                {
                    bool debeIntercambiar = false;

                    // Gravedad descendente
                    if (pacientes[j].NivelGravedad < pacientes[j + 1].NivelGravedad)
                    {
                        debeIntercambiar = true;
                    }
                    // Desempate por fecha
                    else if (pacientes[j].NivelGravedad == pacientes[j + 1].NivelGravedad)
                    {
                        if (pacientes[j].FechaIngreso > pacientes[j + 1].FechaIngreso)
                        {
                            debeIntercambiar = true;
                        }
                    }

                    if (debeIntercambiar)
                    {
                        var temp = pacientes[j];
                        pacientes[j] = pacientes[j + 1];
                        pacientes[j + 1] = temp;
                    }
                }
            }

            return Ok(pacientes);
        }
    }
}