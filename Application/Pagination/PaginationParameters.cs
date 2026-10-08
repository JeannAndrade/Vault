using System.ComponentModel.DataAnnotations;

namespace Application.Pagination;

public record PaginationParameters
{
    public const int PageSizeDefault = 20;
    public const int PageSizeMaximum = 100;

    [Range(1, int.MaxValue, ErrorMessage = "A página deve ser maior ou igual a {1}")]
    public int Page { get; init; } = 1;

    [Range(1, PageSizeMaximum, ErrorMessage = "O tamanho da página deve estar entre {1} e {2}")]
    public int PageSize { get; init; } = PageSizeDefault;
}
