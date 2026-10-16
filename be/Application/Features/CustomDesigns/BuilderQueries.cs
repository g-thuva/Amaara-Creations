using be.Application.Features.Checkout;
using be.Domain.CustomDesigns;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using be.Application.Abstractions.Persistence;
using be.Application.Common;
using be.Domain.Common;

namespace be.Application.Features.CustomDesigns;
public class BuilderQueries
{
    private readonly IApplicationDbContext _context;
    private readonly CustomStickerPricingService _pricing;
    private readonly IOptions<CheckoutOptions> _checkoutOptions;
    public BuilderQueries(IApplicationDbContext context, CustomStickerPricingService pricing, IOptions<CheckoutOptions> checkoutOptions)
    {
        _context = context;
        _pricing = pricing;
        _checkoutOptions = checkoutOptions;
    }

    public async Task<CustomBuilderConfigurationDto> GetConfiguration(CancellationToken cancellationToken)
    {
        var configuration = await ActiveConfiguration().AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        return configuration == null ? throw new CommerceException("CUSTOM_DESIGNS_REQUEST_INVALID", "Custom builder is not configured", StatusCodes.Status503ServiceUnavailable) : configuration.ToDto(false);
    }

    public async Task<CustomQuoteResponse> Quote(CustomQuoteRequest request, CancellationToken cancellationToken)
    {
        var configuration = await ActiveConfiguration().AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        if (configuration == null)
        {
            throw new CommerceException("CUSTOM_DESIGNS_REQUEST_INVALID", "Custom builder is not configured", StatusCodes.Status503ServiceUnavailable);
        }

        try
        {
            return _pricing.Quote(configuration, request);
        }
        catch (CustomBuilderValidationException exception)
        {
            throw new RequestValidationException(new Dictionary<string, string[]> { ["configuration"] = exception.Errors.ToArray() });
        }
    }

    private IQueryable<CustomBuilderConfigurationVersion> ActiveConfiguration()
    {
        return _context.CustomBuilderConfigurationVersions.Include(version => version.Options)
            .Where(version => version.Status == BuilderVersionStatus.Published && version.Currency == _checkoutOptions.Value.Currency)
            .OrderByDescending(version => version.PublishedAt)
            .ThenByDescending(version => version.VersionNumber);
    }
}
