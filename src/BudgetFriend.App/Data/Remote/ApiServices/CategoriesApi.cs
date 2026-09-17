using BudgetFriend.App.Data.Remote.ApiClient;
using BudgetFriend.App.Models;

namespace BudgetFriend.App.Data.Remote.ApiServices;

/// <summary>Category endpoints of the BudgetFriend.API.</summary>
public interface ICategoriesApi {
    Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default);
    Task<CategoryDto> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken ct = default);
    Task<CategoryDto> GetCategoryAsync(Guid categoryId, CancellationToken ct = default);
    Task<CategoryDto> UpdateCategoryAsync(Guid categoryId, UpdateCategoryRequest request, CancellationToken ct = default);
    Task DeleteCategoryAsync(Guid categoryId, CancellationToken ct = default);
}

public sealed class CategoriesApi : ICategoriesApi {
    private readonly ApiHttpClient _client;

    public CategoriesApi(ApiHttpClient client) {
        _client = client;
    }

    public Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default)
        => _client.GetAsync<IReadOnlyList<CategoryDto>>(_client.Path("categories"), ct);

    public Task<CategoryDto> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken ct = default)
        => _client.PostAsync<CreateCategoryRequest, CategoryDto>(_client.Path("categories"), request, ct: ct);

    public Task<CategoryDto> GetCategoryAsync(Guid categoryId, CancellationToken ct = default)
        => _client.GetAsync<CategoryDto>($"{_client.Path("categories")}/{categoryId}", ct);

    public Task<CategoryDto> UpdateCategoryAsync(Guid categoryId, UpdateCategoryRequest request, CancellationToken ct = default)
        => _client.PutAsync<UpdateCategoryRequest, CategoryDto>($"{_client.Path("categories")}/{categoryId}", request, ct);

    public Task DeleteCategoryAsync(Guid categoryId, CancellationToken ct = default)
        => _client.DeleteAsync($"{_client.Path("categories")}/{categoryId}", ct);
}