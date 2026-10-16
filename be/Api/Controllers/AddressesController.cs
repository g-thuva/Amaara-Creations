using be.Application.Features.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace be.Api.Controllers;
[ApiController]
[Route("api/v1/addresses")]
[Authorize]
public class AddressesController(AddressBookService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<AddressResponse>>> GetAddresses(CancellationToken cancellationToken)
    {
        return Ok(await service.GetAddresses(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AddressResponse>> GetAddress(int id, CancellationToken cancellationToken)
    {
        return Ok(await service.GetAddress(id, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<AddressResponse>> CreateAddress([FromBody] SaveAddressRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAddress(request, cancellationToken);
        return CreatedAtAction(nameof(GetAddress), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AddressResponse>> UpdateAddress(int id, [FromBody] SaveAddressRequest request, CancellationToken cancellationToken)
    {
        return Ok(await service.UpdateAddress(id, request, cancellationToken));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAddress(int id, CancellationToken cancellationToken)
    {
        return Ok(await service.DeleteAddress(id, cancellationToken));
    }

    [HttpPut("{id:int}/default")]
    public async Task<ActionResult<AddressResponse>> SetDefault(int id, CancellationToken cancellationToken)
    {
        return Ok(await service.SetDefault(id, cancellationToken));
    }
}
