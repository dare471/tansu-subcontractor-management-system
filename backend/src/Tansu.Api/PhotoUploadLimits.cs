using Microsoft.Extensions.Options;
using Tansu.Application.EmployeePhotoReviews;

namespace Tansu.Api;

internal static class PhotoUploadLimits
{
    public static int ResolveMaxBytes(IOptions<EmployeePhotoReviewOptions> options) =>
        options.Value.MaxPhotoBytes > 0
            ? options.Value.MaxPhotoBytes
            : 1024 * 1024;
}
