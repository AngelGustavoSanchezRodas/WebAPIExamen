using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAPI.Data;
using WebAPI.Models;

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
        public async Task<ActionResult<Paciente>> RegistrarPaciente(Paciente paciente)
        {
            // --- PARTE B.1: Validación de Autorización ---
            if (!MedicosAutorizados.Contains(paciente.MedicoResponsable))
            {
                return Unauthorized(new { error = "Médico no autorizado. El carnet no coincide." });
            }

            // --- PARTE B.2: Validación de Capacidad Crítica ---
            if (paciente.NivelGravedad == 5)
            {
                int criticosEnEspera = await _context.Pacientes
                    .CountAsync(p => p.NivelGravedad == 5 && p.Estado == "En espera");

                if (criticosEnEspera >= 5)
                {
                    return BadRequest(new { error = "Capacidad máxima alcanzada. Redirección inmediata a otro hospital sugerida" });
                }
            }

            // --- REQUERIMIENTO A: Generar ID de Paciente (PAC-2026-XXX) ---
            int cantidadPacientes = await _context.Pacientes.CountAsync();
            string nuevoId = $"PAC-2026-{(cantidadPacientes + 1).ToString("D3")}";
            paciente.IdPaciente = nuevoId;
            paciente.FechaIngreso = DateTime.Now; // Aseguramos el timestamp para el ordenamiento

            _context.Pacientes.Add(paciente);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPacientes), new { id = paciente.IdPaciente }, paciente);
        }

        // GET: api/Pacientes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Paciente>>> GetPacientes()
        {
            // --- PARTE C: El Algoritmo de Ordenamiento ---

            // 1. Extraer todos sin usar ORDER BY de SQL
            var pacientesList = await _context.Pacientes.ToListAsync();
            var pacientes = pacientesList.ToArray();

            // 2. Implementación de Algoritmo de Burbuja (Bubble Sort)
            int n = pacientes.Length;
            for (int i = 0; i < n - 1; i++)
            {
                for (int j = 0; j < n - i - 1; j++)
                {
                    bool debeIntercambiar = false;
                    var p1 = pacientes[j];
                    var p2 = pacientes[j + 1];

                    // Criterio 1: Primero los de Gravedad 5 descendiendo hasta 1
                    if (p1.NivelGravedad < p2.NivelGravedad)
                    {
                        debeIntercambiar = true;
                    }
                    // Criterio 2: A igual gravedad, el más antiguo tiene prioridad
                    else if (p1.NivelGravedad == p2.NivelGravedad)
                    {
                        // Si p1 ingresó DESPUÉS que p2, debe ir abajo de p2
                        if (p1.FechaIngreso > p2.FechaIngreso)
                        {
                            debeIntercambiar = true;
                        }
                    }

                    if (debeIntercambiar)
                    {
                        // Realizar intercambio
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