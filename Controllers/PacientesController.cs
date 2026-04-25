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

        // Arreglo estático de médicos autorizados (Requerimiento B)
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

            // 2. Validación de Capacidad Crítica (Gravedad 5)
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
            // Extraer pacientes sin usar ORDER BY de SQL (Requerimiento C)
            var listaPacientes = await _context.pacientes_13449.ToListAsync();
            var pacientes = listaPacientes.ToArray();
            int n = pacientes.Length;

            // Algoritmo de Burbuja manual
            for (int i = 0; i < n - 1; i++)
            {
                for (int j = 0; j < n - i - 1; j++)
                {
                    bool debeIntercambiar = false;

                    // Criterio 1: Gravedad descendente (5 a 1)
                    if (pacientes[j].NivelGravedad < pacientes[j + 1].NivelGravedad)
                    {
                        debeIntercambiar = true;
                    }
                    // Criterio 2: A igual gravedad, el más antiguo primero
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

        // PUT: api/Pacientes/{id} - Actualizar Estado o Datos
        [HttpPut("{id}")]
        public async Task<IActionResult> ActualizarPaciente(string id, [FromBody] Paciente pacienteActualizado)
        {
            if (id != pacienteActualizado.IdPaciente)
            {
                return BadRequest(new { error = "El ID de la URL no coincide con el ID del cuerpo de la petición." });
            }

            var pacienteExistente = await _context.pacientes_13449.FindAsync(id);
            if (pacienteExistente == null)
            {
                return NotFound(new { error = "Paciente no encontrado." });
            }

            // Validación de valores permitidos para el Estado (En espera, Atendido, Derivado)
            var estadosValidos = new[] { "En espera", "Atendido", "Derivado" };
            if (!estadosValidos.Contains(pacienteActualizado.Estado))
            {
                return BadRequest(new { error = "Estado inválido. Solo se permite: En espera, Atendido o Derivado." });
            }

            // Actualización de campos
            pacienteExistente.NombreCompleto = pacienteActualizado.NombreCompleto;
            pacienteExistente.Sintomas = pacienteActualizado.Sintomas;
            pacienteExistente.NivelGravedad = pacienteActualizado.NivelGravedad;
            pacienteExistente.Estado = pacienteActualizado.Estado;
            pacienteExistente.MedicoResponsable = pacienteActualizado.MedicoResponsable;

            await _context.SaveChangesAsync();
            return Ok(pacienteExistente);
        }

        // DELETE: api/Pacientes/{id} - Eliminar registro
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

            return NoContent(); // Código 204
        }
    }
}