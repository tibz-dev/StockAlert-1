
using FluentValidation;
using StockAlert.Application.DTOs;

public class ProductValidator : AbstractValidator<ProductDto>
{
    public ProductValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Price).GreaterThan(0).WithMessage("Nothing in life is free!");
        RuleFor(x => x.StockQuantity).GreaterThanOrEqualTo(0);
    }
}