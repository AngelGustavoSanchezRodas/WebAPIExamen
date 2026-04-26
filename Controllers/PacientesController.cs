using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebAPIExamen.Data;
using WebAPIExamen.Models;

namespace WebAPIExamen.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PacientesController : ControllerBase
    {
        private readonly AppDbContext _context;

        // Arreglo estático de médicos autorizados
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
            try
            {
                // 1. Validación de Autorización
                if (!MedicosAutorizados.Contains(paciente.MedicoResponsable))
                {
                    return Unauthorized(new { error = "Médico no autorizado. El carnet no coincide." });
                }

                // 2. Validación de Capacidad Crítica (Gravedad 5)
                if (paciente.NivelGravedad == 5)
                {
                    int criticosEnEspera = await _context.pacientes_13449
                        .CountAsync(p => p.NivelGravedad == 5 && p.Estado == "En espera");

                    if (criticosEnEspera >= 5)
                    {
                        return BadRequest(new { error = "Capacidad máxima alcanzada." });
                    }
                }

                // 3. Generación del ID a prueba de borrados (Busca el último ID en lugar de contar)
                var ultimoPaciente = await _context.pacientes_13449
                    .OrderByDescending(p => p.IdPaciente)
                    .FirstOrDefaultAsync();

                int nuevoNumero = 1;
                if (ultimoPaciente != null && ultimoPaciente.IdPaciente != null && ultimoPaciente.IdPaciente.StartsWith("PAC-2026-"))
                {
                    string numeroString = ultimoPaciente.IdPaciente.Substring(9); // Extrae lo que va después de "PAC-2026-"
                    if (int.TryParse(numeroString, out int num))
                    {
                        nuevoNumero = num + 1;
                    }
                }

                paciente.IdPaciente = $"PAC-2026-{nuevoNumero:D3}";
                paciente.FechaIngreso = DateTime.Now;

                _context.pacientes_13449.Add(paciente);
                await _context.SaveChangesAsync();

                // Usamos Ok() en lugar de CreatedAtAction para evitar errores de ruteo
                return Ok(paciente);
            }
            catch (Exception ex)
            {
                // Si la BD falla, te dirá exactamente por qué
                return StatusCode(500, new
                {
                    error = "Error interno",
                    mensaje = ex.Message,
                    detalle = ex.InnerException?.Message
                });
            }
        }

        // GET: api/Pacientes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Paciente>>> GetPacientes()
        {
            var listaPacientes = await _context.pacientes_13449.ToListAsync();
            var pacientes = listaPacientes.ToArray();
            int n = pacientes.Length;

            for (int i = 0; i < n - 1; i++)
            {
                for (int j = 0; j < n - i - 1; j++)
                {
                    bool debeIntercambiar = false;

                    if (pacientes[j].NivelGravedad < pacientes[j + 1].NivelGravedad)
                    {
                        debeIntercambiar = true;
                    }
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

        // PUT: api/Pacientes/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> ActualizarPaciente(string id, [FromBody] Paciente pacienteActualizado)
        {
            if (id != pacienteActualizado.IdPaciente)
            {
                return BadRequest(new { error = "El ID no coincide." });
            }

            var pacienteExistente = await _context.pacientes_13449.FindAsync(id);
            if (pacienteExistente == null)
            {
                return NotFound(new { error = "Paciente no encontrado." });
            }

            var estadosValidos = new[] { "En espera", "Atendido", "Derivado" };
            if (!estadosValidos.Contains(pacienteActualizado.Estado))
            {
                return BadRequest(new { error = "Estado inválido." });
            }

            pacienteExistente.NombreCompleto = pacienteActualizado.NombreCompleto;
            pacienteExistente.Sintomas = pacienteActualizado.Sintomas;
            pacienteExistente.NivelGravedad = pacienteActualizado.NivelGravedad;
            pacienteExistente.Estado = pacienteActualizado.Estado;
            pacienteExistente.MedicoResponsable = pacienteActualizado.MedicoResponsable;

            await _context.SaveChangesAsync();
            return Ok(pacienteExistente);
        }

        // DELETE: api/Pacientes/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> EliminarPaciente(string id)
        {
            var paciente = await _context.pacientes_13449.FindAsync(id);
            if (paciente == null)
            {
                return NotFound(new { error = "Paciente no encontrado." });
            }

            _context.pacientes_13449.Remove(paciente);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}