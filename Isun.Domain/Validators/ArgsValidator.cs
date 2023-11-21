using FluentValidation;

namespace Isun.Domain.Validators;
public sealed record ArgsValidator(string Cities);

public sealed class ArgsValidatorValidator : AbstractValidator<ArgsValidator>
{
    public ArgsValidatorValidator()
    {
        RuleFor(x => x.Cities).NotEmpty().WithMessage("Cities can't be empty.");
        RuleFor(x => x.Cities).Matches(@"^[a-zA-ZąčęėįšųūžĄČĘĖĮŠŲŪŽ]+(?:\s*,\s*[a-zA-ąčęėįšųūžĄČĘĖĮŠŲŪŽ]+)*$")
            .WithMessage("Cities must be a comma separated list of strings.");
    }
}

