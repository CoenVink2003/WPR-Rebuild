using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/Vehicle")]
public class VehicleController : ControllerBase
{
    private readonly DatabaseContext _context;

    public VehicleController(DatabaseContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }
    
    // GET (zonder filters)
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Vehicle>>> GetVehicles()
    {
        try
        {
            return await _context.Vehicles.ToListAsync();
        }
        catch (Exception e)
        {
            return StatusCode(500, new { message = "Internal Server Error", error = e.Message });
        }
    }
    
    // GET (op basis van ID)
    [HttpGet("{id}")]
    public async Task<ActionResult<Vehicle>> GetVehicle(int id)
    {
        Vehicle vehicle = await _context.Vehicles.FindAsync(id);

        if (vehicle == null)
        {
            return NotFound();
        }

        return vehicle;
    }
    
    // POST (geeft de ID van het nieuwe object terug)
    [HttpPost]
    public async Task<ActionResult<object>> PostEmployee([FromBody] Vehicle vehicle)
    {
        if (vehicle == null)
        {
            return BadRequest("Vehicle object is not set.");
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        _context.Vehicles.Add(vehicle);
        await _context.SaveChangesAsync();

        return Ok(new { ID = vehicle.Id });
    }
    
    // DELETE
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteVehicle(int id)
    {
        Vehicle vehicle = await _context.Vehicles.FindAsync(id);
        if (vehicle == null)
        {
            return NotFound();
        }

        _context.Remove(vehicle);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}