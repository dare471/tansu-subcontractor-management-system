using Microsoft.Extensions.Options;
using Tansu.Application.Common.Interfaces;

namespace Tansu.Application.Common;

public sealed class AppBranding(IOptions<BrandingOptions> options) : IAppBranding
{
    private readonly BrandingOptions _options = options.Value;

    public string BrandName => string.IsNullOrWhiteSpace(_options.BrandName) ? "Tansu" : _options.BrandName.Trim();
    public string CompanyName => string.IsNullOrWhiteSpace(_options.CompanyName) ? "ТАНСУ" : _options.CompanyName.Trim();
}
