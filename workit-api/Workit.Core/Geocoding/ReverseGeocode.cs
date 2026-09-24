using FluentValidation;
using MediatR;
using Workit.Core.Shared.Exceptions;
using Workit.Core.Shared.Location;

namespace Workit.Core.Geocoding;

public static class ReverseGeocode
{
    public sealed record Request(double Latitude, double Longitude) : IRequest<AddressSuggestion>;

    public sealed class RequestValidator : AbstractValidator<Request>
    {
        public RequestValidator()
        {
            RuleFor(request => request.Latitude)
                .InclusiveBetween(-90, 90);

            RuleFor(request => request.Longitude)
                .InclusiveBetween(-180, 180);
        }
    }

    internal sealed class Handler(IAddressLookupService addressLookupService)
        : IRequestHandler<Request, AddressSuggestion>
    {
        public async Task<AddressSuggestion> Handle(Request request, CancellationToken cancellationToken)
        {
            return await addressLookupService.ReverseAsync(request.Latitude, request.Longitude, cancellationToken)
                ?? throw new NotFoundException("error.addressNotFound");
        }
    }
}
