using System.ComponentModel.DataAnnotations;

var product = new Product
{
    Id = 1,
    Name = "Test product",
    UnitPrice = 10
};

// Valid case
var validationResults = product.Validate();
Console.WriteLine($"Product should be valid, is valid?: {!validationResults.Any()}");

// Invalid case
product.Id = -1;
product.Name = null;
product.UnitPrice = -5;
validationResults = product.Validate();
Console.WriteLine($"Product should be invalid, is valid?: {!validationResults.Any()}");
var resultTable = validationResults.Select(r => new { Name = string.Join(", ", r.MemberNames), Message = r.ErrorMessage });
var nameFieldLength = resultTable.Max(row => row.Name.Length);
foreach (var result in resultTable)
{
    Console.WriteLine($"  {result.Name.PadLeft(nameFieldLength)}: {result.Message}");
}

public partial class Product
{
    [Range(0, int.MaxValue, ErrorMessage = "**CUSTOM ERROR MESSAGE** Id must be greater than 0.")]
    public int Id { get; set; }

    [Required, StringLength(1000, MinimumLength = 1)]
    public string? Name { get; set; }

    [Range(0, double.MaxValue)]
    public double UnitPrice { get; set; }
}

// This would get source generated
public partial class Product : IValidate
{
    private static readonly RangeAttribute _idAttr01 = new(0, int.MaxValue) { ErrorMessage = "Id must be greater than 0" };

    private static readonly RequiredAttribute _nameAttr01 = new();
    private static readonly StringLengthAttribute _nameAttr02 = new(1000) { MinimumLength = 1 };

    private static readonly RangeAttribute _unitPriceAttr01 = new(0, double.MaxValue);

    private static readonly string[] _idMemberNames = new[] { nameof(Id) };
    private static readonly string[] _nameMemberNames = new[] { nameof(Name) };
    private static readonly string[] _unitPriceMemberNames = new[] { nameof(UnitPrice) };

    private static void ValidateValue(ValidationAttribute attribute, object? value, List<ValidationResult> results, string[] memberNames)
    {
        if (!attribute.IsValid(value))
        {
            results.Add(new(attribute.FormatErrorMessage(memberNames[0]), memberNames));
        }
    }

    public IEnumerable<ValidationResult> Validate()
    {
        var results = new List<ValidationResult>();

        // Id
        ValidateValue(_idAttr01, Id, results, _idMemberNames);

        // Name
        ValidateValue(_nameAttr01, Name, results, _nameMemberNames);
        ValidateValue(_nameAttr02, Name, results, _nameMemberNames);

        // UnitPrice
        ValidateValue(_unitPriceAttr01, UnitPrice, results, _unitPriceMemberNames);

        return results;
    }
}

public interface IValidate
{
    IEnumerable<ValidationResult> Validate();
}
