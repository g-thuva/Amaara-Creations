using System.Security.Claims;
using be.Data;
using be.DTOs.Address;
using be.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace be.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class AddressesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AddressesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<List<AddressResponse>>> GetAddresses(CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            var addresses = await _context.Addresses
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.IsDefault)
                .ThenByDescending(a => a.UpdatedAt)
                .Select(a => ToResponse(a))
                .ToListAsync(cancellationToken);

            return Ok(addresses);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<AddressResponse>> GetAddress(int id, CancellationToken cancellationToken)
        {
            var address = await FindOwnAddressAsync(id, cancellationToken);
            if (address == null)
            {
                return NotFound(new { message = "Address not found." });
            }

            return Ok(ToResponse(address));
        }

        [HttpPost]
        public async Task<ActionResult<AddressResponse>> CreateAddress([FromBody] SaveAddressRequest request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var userId = GetUserId();
            var hasExistingAddress = await _context.Addresses.AnyAsync(a => a.UserId == userId, cancellationToken);
            var shouldBeDefault = request.IsDefault || !hasExistingAddress;

            if (shouldBeDefault)
            {
                await ClearDefaultAddressesAsync(userId, cancellationToken);
            }

            var address = new Address
            {
                UserId = userId,
                Label = request.Label.Trim(),
                RecipientName = request.RecipientName.Trim(),
                Phone = request.Phone,
                AddressLine1 = request.AddressLine1.Trim(),
                AddressLine2 = request.AddressLine2,
                City = request.City.Trim(),
                DistrictOrProvince = request.DistrictOrProvince,
                PostalCode = request.PostalCode,
                Country = request.Country.Trim(),
                IsDefault = shouldBeDefault,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Addresses.Add(address);
            await _context.SaveChangesAsync(cancellationToken);

            return CreatedAtAction(nameof(GetAddress), new { id = address.Id }, ToResponse(address));
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<AddressResponse>> UpdateAddress(int id, [FromBody] SaveAddressRequest request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var address = await FindOwnAddressAsync(id, cancellationToken);
            if (address == null)
            {
                return NotFound(new { message = "Address not found." });
            }

            if (request.IsDefault)
            {
                await ClearDefaultAddressesAsync(address.UserId, cancellationToken);
            }

            address.Label = request.Label.Trim();
            address.RecipientName = request.RecipientName.Trim();
            address.Phone = request.Phone;
            address.AddressLine1 = request.AddressLine1.Trim();
            address.AddressLine2 = request.AddressLine2;
            address.City = request.City.Trim();
            address.DistrictOrProvince = request.DistrictOrProvince;
            address.PostalCode = request.PostalCode;
            address.Country = request.Country.Trim();
            address.IsDefault = request.IsDefault || address.IsDefault;
            address.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
            return Ok(ToResponse(address));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteAddress(int id, CancellationToken cancellationToken)
        {
            var address = await FindOwnAddressAsync(id, cancellationToken);
            if (address == null)
            {
                return NotFound(new { message = "Address not found." });
            }

            var wasDefault = address.IsDefault;
            var userId = address.UserId;
            _context.Addresses.Remove(address);
            await _context.SaveChangesAsync(cancellationToken);

            if (wasDefault)
            {
                var nextAddress = await _context.Addresses
                    .Where(a => a.UserId == userId)
                    .OrderByDescending(a => a.UpdatedAt)
                    .FirstOrDefaultAsync(cancellationToken);

                if (nextAddress != null)
                {
                    nextAddress.IsDefault = true;
                    nextAddress.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync(cancellationToken);
                }
            }

            return Ok(new { message = "Address deleted." });
        }

        [HttpPost("{id:int}/default")]
        [HttpPut("{id:int}/default")]
        public async Task<ActionResult<AddressResponse>> SetDefault(int id, CancellationToken cancellationToken)
        {
            var address = await FindOwnAddressAsync(id, cancellationToken);
            if (address == null)
            {
                return NotFound(new { message = "Address not found." });
            }

            await ClearDefaultAddressesAsync(address.UserId, cancellationToken);
            address.IsDefault = true;
            address.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            return Ok(ToResponse(address));
        }

        private string GetUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new InvalidOperationException("Authenticated user id is missing.");
        }

        private Task<Address?> FindOwnAddressAsync(int id, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            return _context.Addresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId, cancellationToken);
        }

        private async Task ClearDefaultAddressesAsync(string userId, CancellationToken cancellationToken)
        {
            var defaultAddresses = await _context.Addresses
                .Where(a => a.UserId == userId && a.IsDefault)
                .ToListAsync(cancellationToken);

            foreach (var defaultAddress in defaultAddresses)
            {
                defaultAddress.IsDefault = false;
                defaultAddress.UpdatedAt = DateTime.UtcNow;
            }
        }

        private static AddressResponse ToResponse(Address address)
        {
            return new AddressResponse
            {
                Id = address.Id,
                Label = address.Label,
                RecipientName = address.RecipientName,
                Phone = address.Phone,
                AddressLine1 = address.AddressLine1,
                AddressLine2 = address.AddressLine2,
                City = address.City,
                DistrictOrProvince = address.DistrictOrProvince,
                PostalCode = address.PostalCode,
                Country = address.Country,
                IsDefault = address.IsDefault,
                CreatedAt = address.CreatedAt,
                UpdatedAt = address.UpdatedAt
            };
        }
    }
}
