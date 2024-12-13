using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Operations;
using Microsoft.AspNetCore.Mvc.Formatters;
using System.Linq.Expressions;
using System.Reflection;


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

    // GET, verkapt als POST i.v.m. data (op basis van filter)
    [HttpPost("filter")]
    public async Task<ActionResult<IEnumerable<Vehicle>>> FilterVehicle([FromBody] JsonElement rawPatchData)
    {
        var filters = rawPatchData;

        IQueryable<Vehicle> filteredVehicles = _context.Vehicles;

        foreach (var property in filters.EnumerateObject())
        {
            var key = property.Name;
            var value = property.Value;

            if (value.ValueKind != JsonValueKind.Null)
            {
                var propertyType = typeof(Vehicle).GetProperty(key)?.PropertyType;

                if (propertyType != null)
                {
                    var convertedValue = Convert.ChangeType(value.ToString(), propertyType);

                    var param = Expression.Parameter(typeof(Vehicle), "v");
                    var propertyExpression = Expression.Property(param, key);
                    var valueExpression = Expression.Constant(convertedValue);

                    var equalsExpression = Expression.Equal(propertyExpression, valueExpression);
                    var lambda = Expression.Lambda<Func<Vehicle, bool>>(equalsExpression, param);

                    filteredVehicles = filteredVehicles.Where(lambda);
                }
            }
        }

        var result = await filteredVehicles.ToListAsync();
        return result;
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

    [HttpPatch("{id}")]
    public async Task<IActionResult> PatchVehicle(int id, [FromBody] JsonElement rawPatchData)
    {
        // Zoek het voertuig op basis van de id
        var vehicle = await _context.Vehicles.FindAsync(id);
        if (vehicle == null)
        {
            return NotFound("Vehicle not found.");
        }

        // Itereer over de JSON eigenschappen (filters)
        foreach (var property in rawPatchData.EnumerateObject())
        {
            // Verkrijg de propertynaam en waarde uit de JSON
            var propertyName = property.Name;
            var newValue = property.Value;

            // Zoek de corresponderende property in het vehicle object
            var vehicleProperty =
                vehicle.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);

            if (vehicleProperty != null && vehicleProperty.CanWrite)
            {
                // Zet de waarde als de nieuwe waarde niet null is
                if (newValue.ValueKind != JsonValueKind.Null)
                {
                    // Voor niet-strings: converteer naar het juiste type
                    var targetType = vehicleProperty.PropertyType;
                    var convertedValue = Convert.ChangeType(newValue.ToString(), targetType);

                    // Zet de nieuwe waarde op het voertuig
                    vehicleProperty.SetValue(vehicle, convertedValue);
                }
            }
        }

        try
        {
            // Sla de veranderingen op in de database
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.Vehicles.Any(c => c.Id == id))
            {
                return NotFound("Vehicle not found.");
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }
}
