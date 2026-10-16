using be.Domain.Identity;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using be.Application.Abstractions.Persistence;
using be.Application.Abstractions.Identity;
using be.Domain.Common;

namespace be.Application.Features.Identity;
public class AddressBookService
{
    private readonly IApplicationDbContext _context;
    public AddressBookService(IApplicationDbContext context, IRequestActor actor)
    {
        _context = context;
        _actor = actor;
    }

    public async Task<List<AddressResponse>> GetAddresses(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var addresses = await _context.Addresses.Where(a => a.UserId == userId)
            .OrderByDescending(a => a.IsDefault)
            .ThenByDescending(a => a.UpdatedAt)
            .Select(a => ToResponse(a))
            .ToListAsync(cancellationToken);
        return addresses;
    }

    public async Task<AddressResponse> GetAddress(int id, CancellationToken cancellationToken)
    {
        var address = await FindOwnAddressAsync(id, cancellationToken);
        if (address == null)
        {
            throw new CommerceException("IDENTITY_NOT_FOUND", "Address not found.", 404);
        }

        return ToResponse(address);
    }

    public async Task<AddressResponse> CreateAddress(SaveAddressRequest request, CancellationToken cancellationToken)
    {
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
        return ToResponse(address);
    }

    public async Task<AddressResponse> UpdateAddress(int id, SaveAddressRequest request, CancellationToken cancellationToken)
    {
        var address = await FindOwnAddressAsync(id, cancellationToken);
        if (address == null)
        {
            throw new CommerceException("IDENTITY_NOT_FOUND", "Address not found.", 404);
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
        return ToResponse(address);
    }

    public async Task<object?> DeleteAddress(int id, CancellationToken cancellationToken)
    {
        var address = await FindOwnAddressAsync(id, cancellationToken);
        if (address == null)
        {
            throw new CommerceException("IDENTITY_NOT_FOUND", "Address not found.", 404);
        }

        var wasDefault = address.IsDefault;
        var userId = address.UserId;
        _context.Addresses.Remove(address);
        await _context.SaveChangesAsync(cancellationToken);
        if (wasDefault)
        {
            var nextAddress = await _context.Addresses.Where(a => a.UserId == userId).OrderByDescending(a => a.UpdatedAt).FirstOrDefaultAsync(cancellationToken);
            if (nextAddress != null)
            {
                nextAddress.IsDefault = true;
                nextAddress.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        return new
        {
            message = "Address deleted."
        };
    }

    public async Task<AddressResponse> SetDefault(int id, CancellationToken cancellationToken)
    {
        var address = await FindOwnAddressAsync(id, cancellationToken);
        if (address == null)
        {
            throw new CommerceException("IDENTITY_NOT_FOUND", "Address not found.", 404);
        }

        await ClearDefaultAddressesAsync(address.UserId, cancellationToken);
        address.IsDefault = true;
        address.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return ToResponse(address);
    }

    private string GetUserId()
    {
        return _actor.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("Authenticated user id is missing.");
    }

    private Task<Address?> FindOwnAddressAsync(int id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        return _context.Addresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId, cancellationToken);
    }

    private async Task ClearDefaultAddressesAsync(string userId, CancellationToken cancellationToken)
    {
        var defaultAddresses = await _context.Addresses.Where(a => a.UserId == userId && a.IsDefault).ToListAsync(cancellationToken);
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

    private readonly IRequestActor _actor;
}
