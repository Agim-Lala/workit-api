using FluentValidation;
using MediatR;
using Workit.Core.Businesses.Domain;
using Workit.Core.Shared.Location;

namespace Workit.Core.Geocoding;

public static class AutocompleteAddress
{
    public const int MinQueryLength = 3;

    public sealed record Request(string Query) : IRequest<IReadOnlyList<AddressSuggestion>>;

    public sealed class RequestValidator : AbstractValidator<Request>
    {
        public RequestValidator()
        {
            RuleFor(request => request.Query)
                .NotEmpty()
                .MinimumLength(MinQueryLength)
                .MaximumLength(BusinessProfile.MaxFullAddressLength);
        }
    }

    internal sealed class Handler(IAddressLookupService addressLookupService)
        : IRequestHandler<Request, IReadOnlyList<AddressSuggestion>>
    {
        public Task<IReadOnlyList<AddressSuggestion>> Handle(Request request, CancellationToken cancellationToken)
        {
            return addressLookupService.AutocompleteAsync(request.Query.Trim(), cancellationToken);
        }
    }
}
