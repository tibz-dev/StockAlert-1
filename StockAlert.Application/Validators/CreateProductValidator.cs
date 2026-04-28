using FluentValidation;
using StockAlert.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockAlert.Application.Validators;
public class ProductValidator : AbstractValidator<Product>
{
    public ProductValidator()
    {
        RuleFor(p => p.Name).NotEmpty().MaximumLength(100);
        RuleFor(p => p.Price).GreaterThan(0);
        RuleFor(p => p.StockQuantity).GreaterThanOrEqualTo(0);
    }
}
