using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using namera_API.Common.Responses;
using namera_API.Data;
using namera_API.DTOs.Products;
using namera_API.Models.Products.Categories;
using namera_API.Models.Products.Enums;
using namera_API.Models.Products.Products;

namespace namera_API.Services.Products;

public sealed class ProductManagementService : IProductManagementService
{
    private readonly AppDbContext _dbContext;

    public ProductManagementService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ApiResponse<IReadOnlyList<AdminProductListItemDto>>> GetProductsAsync(AdminProductListQueryDto query)
    {
        var productsQuery = _dbContext.Products
            .AsNoTracking()
            .Include(product => product.Category)
            .Include(product => product.Images)
            .Include(product => product.OptionGroups)
            .Include(product => product.CustomizationFields)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            productsQuery = productsQuery.Where(product => product.Name.Contains(search) || product.Slug.Contains(search));
        }

        if (query.CategoryId.HasValue)
        {
            productsQuery = productsQuery.Where(product => product.CategoryId == query.CategoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Status) && TryParseStatus(query.Status, out var status))
        {
            productsQuery = productsQuery.Where(product => product.Status == status);
        }

        if (query.Customized.HasValue)
        {
            productsQuery = query.Customized.Value
                ? productsQuery.Where(product => product.HasVariants || product.CustomizationFields.Any())
                : productsQuery.Where(product => !product.HasVariants && !product.CustomizationFields.Any());
        }

        if (query.LowStockOnly)
        {
            productsQuery = productsQuery.Where(product =>
                product.InventoryTrackingEnabled &&
                !product.MadeToOrder &&
                product.Quantity != null &&
                product.Quantity <= product.LowStockThreshold);
        }

        var products = await productsQuery
            .OrderBy(product => product.DisplayOrder)
            .ThenByDescending(product => product.UpdatedAt ?? product.CreatedAt)
            .ToListAsync();

        return ApiResponse<IReadOnlyList<AdminProductListItemDto>>.Ok(
            products.Select(ToAdminListItem).ToList(),
            "تم تحميل منتجات لوحة الإدارة بنجاح");
    }

    public async Task<ApiResponse<ProductDetailsDto>> GetProductAsync(Guid id)
    {
        var product = await GetProductForDetailsAsync(id);
        if (product is null)
        {
            return ApiResponse<ProductDetailsDto>.Fail("المنتج غير موجود");
        }

        return ApiResponse<ProductDetailsDto>.Ok(ToDetails(product), "تم تحميل المنتج بنجاح");
    }

    public async Task<ApiResponse<ProductDetailsDto>> CreateProductAsync(CreateProductRequestDto request)
    {
        var errors = await ValidateProductRequestAsync(request);
        if (errors.Count > 0)
        {
            return ApiResponse<ProductDetailsDto>.Fail("راجع حقول المنتج قبل الحفظ", errors);
        }

        var product = new Product
        {
            Id = request.ClientId ?? Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };

        ApplyRequest(product, request);
        product.Slug = await CreateUniqueSlugAsync(request.Slug, request.Name);

        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var created = await GetProductForDetailsAsync(product.Id);
        return ApiResponse<ProductDetailsDto>.Ok(ToDetails(created!), "تم إنشاء المنتج بنجاح");
    }

    public async Task<ApiResponse<ProductDetailsDto>> UpdateProductAsync(Guid id, UpdateProductRequestDto request)
    {
        var product = await _dbContext.Products
            .Include(item => item.Images)
            .Include(item => item.OptionGroups)
                .ThenInclude(group => group.Values)
            .Include(item => item.CustomizationFields)
                .ThenInclude(field => field.Choices)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (product is null)
        {
            return ApiResponse<ProductDetailsDto>.Fail("المنتج غير موجود");
        }

        var errors = await ValidateProductRequestAsync(request, id);
        if (errors.Count > 0)
        {
            return ApiResponse<ProductDetailsDto>.Fail("راجع حقول المنتج قبل الحفظ", errors);
        }

        product.UpdatedAt = DateTime.UtcNow;
        ApplyRequest(product, request);
        product.Slug = await CreateUniqueSlugAsync(request.Slug, request.Name, id);

        await _dbContext.SaveChangesAsync();

        var updated = await GetProductForDetailsAsync(product.Id);
        return ApiResponse<ProductDetailsDto>.Ok(ToDetails(updated!), "تم تحديث المنتج بنجاح");
    }

    public async Task<ApiResponse<ProductDetailsDto>> ArchiveProductAsync(Guid id)
    {
        var product = await _dbContext.Products.FirstOrDefaultAsync(item => item.Id == id);
        if (product is null)
        {
            return ApiResponse<ProductDetailsDto>.Fail("المنتج غير موجود");
        }

        product.Status = ProductStatus.Archived;
        product.AllowOrdering = false;
        product.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        return await GetProductAsync(id);
    }

    public async Task<ApiResponse<ProductDetailsDto>> PublishProductAsync(Guid id, bool publish)
    {
        var product = await _dbContext.Products.FirstOrDefaultAsync(item => item.Id == id);
        if (product is null)
        {
            return ApiResponse<ProductDetailsDto>.Fail("المنتج غير موجود");
        }

        product.Status = publish ? ProductStatus.Published : ProductStatus.Hidden;
        product.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        return await GetProductAsync(id);
    }

    public async Task<ApiResponse<ProductCategoryResponseDto>> CreateCategoryAsync(CreateProductCategoryRequestDto request)
    {
        var name = NormalizeText(request.Name);
        if (string.IsNullOrWhiteSpace(name))
        {
            return ApiResponse<ProductCategoryResponseDto>.Fail("اسم التصنيف مطلوب");
        }

        var slug = await CreateUniqueCategorySlugAsync(request.Slug, name);
        var category = new ProductCategory
        {
            Id = request.ClientId ?? Guid.NewGuid(),
            Name = name,
            Slug = slug,
            Description = NormalizeNullableText(request.Description),
            ImageUrl = NormalizeNullableText(request.ImageUrl),
            IsActive = true,
            DisplayOrder = await _dbContext.ProductCategories.CountAsync() + 1,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.ProductCategories.Add(category);
        await _dbContext.SaveChangesAsync();

        return ApiResponse<ProductCategoryResponseDto>.Ok(ToCategoryDto(category), "تم إنشاء التصنيف بنجاح");
    }

    private async Task<Product?> GetProductForDetailsAsync(Guid id)
    {
        return await _dbContext.Products
            .AsNoTracking()
            .Include(product => product.Category)
            .Include(product => product.Images)
            .Include(product => product.OptionGroups)
                .ThenInclude(group => group.Values)
            .Include(product => product.CustomizationFields)
                .ThenInclude(field => field.Choices)
            .FirstOrDefaultAsync(product => product.Id == id);
    }

    private void ApplyRequest(Product product, CreateProductRequestDto request)
    {
        product.CategoryId = request.CategoryId;
        product.Name = NormalizeText(request.Name);
        product.ShortDescription = NormalizeNullableText(request.ShortDescription);
        product.Description = NormalizeNullableText(request.Description);
        product.PricingType = ParsePricingType(request.PricingType);
        product.BasePrice = request.BasePrice ?? 0;
        product.IsPriceVisible = request.IsPriceVisible;
        product.PriceLabel = NormalizeNullableText(request.PriceLabel);
        product.Currency = string.IsNullOrWhiteSpace(request.Currency) ? "ILS" : NormalizeText(request.Currency).ToUpperInvariant();
        product.Status = ParseStatus(request.Status);
        product.HasVariants = request.HasVariants || request.OptionGroups.Count > 0;
        product.IsCustomizable = request.CustomizationFields.Count > 0 || product.HasVariants;
        product.InventoryTrackingEnabled = request.InventoryTrackingEnabled;
        product.Quantity = request.InventoryTrackingEnabled ? request.Quantity : null;
        product.LowStockThreshold = request.LowStockThreshold;
        product.MadeToOrder = request.MadeToOrder;
        product.AllowBackorder = request.AllowBackorder;
        product.MinimumQuantity = request.MinimumQuantity;
        product.MaximumQuantity = request.MaximumQuantity;
        product.PreparationTimeInDays = request.MaxPreparationDays ?? request.MinPreparationDays ?? 0;
        product.MinPreparationDays = request.MinPreparationDays;
        product.MaxPreparationDays = request.MaxPreparationDays;
        product.PreparationUnit = ParsePreparationUnit(request.PreparationUnit);
        product.PreparationNote = NormalizeNullableText(request.PreparationNote);
        product.ShowOnHomepage = request.ShowOnHomepage;
        product.IsFeatured = request.IsFeatured;
        product.IsNew = request.IsNew;
        product.ShowInSuggestions = request.ShowInSuggestions;
        product.DirectAccessOnly = request.DirectAccessOnly;
        product.AllowRatings = request.AllowRatings;
        product.AllowOrdering = request.AllowOrdering;
        product.DisplayOrder = request.DisplayOrder;
        product.VisibleFrom = request.VisibleFrom;
        product.VisibleTo = request.VisibleTo;

        product.Images.Clear();
        foreach (var image in request.Images.OrderBy(item => item.DisplayOrder))
        {
            product.Images.Add(new ProductImage
            {
                Id = Guid.NewGuid(),
                ImageUrl = NormalizeText(image.ImageUrl),
                AltText = NormalizeNullableText(image.AltText),
                IsPrimary = image.IsPrimary,
                DisplayOrder = image.DisplayOrder,
                CreatedAt = DateTime.UtcNow
            });
        }

        product.OptionGroups.Clear();
        foreach (var groupRequest in request.OptionGroups.OrderBy(item => item.DisplayOrder))
        {
            var group = new ProductOptionGroup
            {
                Id = Guid.NewGuid(),
                Name = NormalizeText(groupRequest.Name),
                Description = NormalizeNullableText(groupRequest.Description),
                IsRequired = groupRequest.IsRequired,
                IsActive = groupRequest.IsActive,
                DisplayOrder = groupRequest.DisplayOrder,
                CreatedAt = DateTime.UtcNow
            };

            foreach (var valueRequest in groupRequest.Values.OrderBy(item => item.DisplayOrder))
            {
                group.Values.Add(new ProductOptionValue
                {
                    Id = Guid.NewGuid(),
                    Label = NormalizeText(valueRequest.Label),
                    ExtraPrice = valueRequest.ExtraPrice,
                    DisplayOrder = valueRequest.DisplayOrder,
                    IsActive = valueRequest.IsActive,
                    IsDefault = valueRequest.IsDefault,
                    StockQuantity = valueRequest.StockQuantity,
                    Sku = NormalizeNullableText(valueRequest.Sku),
                    ImageUrl = NormalizeNullableText(valueRequest.ImageUrl),
                    CreatedAt = DateTime.UtcNow
                });
            }

            product.OptionGroups.Add(group);
        }

        product.CustomizationFields.Clear();
        foreach (var fieldRequest in request.CustomizationFields.OrderBy(item => item.DisplayOrder))
        {
            var field = new ProductCustomizationField
            {
                Id = Guid.NewGuid(),
                Label = NormalizeText(fieldRequest.Label),
                FieldType = ParseFieldType(fieldRequest.Type),
                Description = NormalizeNullableText(fieldRequest.Description),
                Placeholder = NormalizeNullableText(fieldRequest.Placeholder),
                IsRequired = fieldRequest.IsRequired,
                DisplayOrder = fieldRequest.DisplayOrder,
                AdditionalPrice = fieldRequest.AdditionalPrice,
                MinLength = fieldRequest.MinLength,
                MaxLength = fieldRequest.MaxLength,
                MinValue = fieldRequest.MinValue,
                MaxValue = fieldRequest.MaxValue,
                AllowedFilesCsv = string.Join(',', fieldRequest.AllowedFiles.Select(NormalizeText).Where(item => item.Length > 0)),
                IsActive = fieldRequest.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            foreach (var choiceRequest in fieldRequest.Choices.OrderBy(item => item.DisplayOrder))
            {
                field.Choices.Add(new ProductCustomizationChoice
                {
                    Id = Guid.NewGuid(),
                    Label = NormalizeText(choiceRequest.Label),
                    AdditionalPrice = choiceRequest.AdditionalPrice,
                    DisplayOrder = choiceRequest.DisplayOrder,
                    IsActive = choiceRequest.IsActive,
                    CreatedAt = DateTime.UtcNow
                });
            }

            product.CustomizationFields.Add(field);
        }
    }

    private async Task<List<string>> ValidateProductRequestAsync(CreateProductRequestDto request, Guid? existingProductId = null)
    {
        var errors = new List<string>();
        var name = NormalizeText(request.Name);

        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add("اسم المنتج مطلوب.");
        }

        if (name.Length > 200)
        {
            errors.Add("اسم المنتج يجب ألا يتجاوز 200 حرف.");
        }

        if (!await _dbContext.ProductCategories.AnyAsync(category => category.Id == request.CategoryId && category.IsActive))
        {
            errors.Add("يجب اختيار تصنيف موجود وفعال.");
        }

        var primaryImages = request.Images.Count(image => image.IsPrimary && !string.IsNullOrWhiteSpace(image.ImageUrl));
        if (primaryImages != 1)
        {
            errors.Add("يجب تحديد صورة رئيسية واحدة فقط للمنتج.");
        }

        foreach (var image in request.Images)
        {
            if (string.IsNullOrWhiteSpace(image.ImageUrl))
            {
                errors.Add("كل صورة يجب أن تحتوي على رابط أو مسار صالح.");
            }
        }

        var pricingType = ParsePricingType(request.PricingType);
        if (pricingType != ProductPricingType.Quote && request.BasePrice is null)
        {
            errors.Add("السعر مطلوب لطريقة التسعير المختارة.");
        }

        if (request.BasePrice < 0)
        {
            errors.Add("السعر لا يمكن أن يكون سالبًا.");
        }

        if (request.InventoryTrackingEnabled && !request.MadeToOrder && request.Quantity is null)
        {
            errors.Add("الكمية مطلوبة عند تفعيل تتبع المخزون.");
        }

        if (request.Quantity < 0 || request.LowStockThreshold < 0)
        {
            errors.Add("قيم المخزون يجب أن تكون موجبة.");
        }

        if (request.MinimumQuantity < 1)
        {
            errors.Add("أقل كمية للطلب يجب أن تكون 1 على الأقل.");
        }

        if (request.MaximumQuantity.HasValue && request.MaximumQuantity < request.MinimumQuantity)
        {
            errors.Add("أعلى كمية لا يمكن أن تكون أقل من أقل كمية.");
        }

        if (request.MinPreparationDays < 0 || request.MaxPreparationDays < 0)
        {
            errors.Add("مدة التجهيز لا يمكن أن تكون سالبة.");
        }

        if (request.MinPreparationDays.HasValue && request.MaxPreparationDays.HasValue && request.MaxPreparationDays < request.MinPreparationDays)
        {
            errors.Add("أعلى مدة تجهيز يجب أن تكون أكبر من أو تساوي أقل مدة.");
        }

        if (request.VisibleFrom.HasValue && request.VisibleTo.HasValue && request.VisibleTo < request.VisibleFrom)
        {
            errors.Add("تاريخ نهاية الظهور يجب أن يكون بعد تاريخ البداية.");
        }

        foreach (var (group, index) in request.OptionGroups.Select((item, index) => (item, index)))
        {
            if (string.IsNullOrWhiteSpace(group.Name))
            {
                errors.Add($"اسم مجموعة الخيارات رقم {index + 1} مطلوب.");
            }

            var activeLabels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var value in group.Values)
            {
                var label = NormalizeText(value.Label);
                if (string.IsNullOrWhiteSpace(label))
                {
                    errors.Add($"قيمة داخل مجموعة {group.Name} بدون اسم.");
                }

                if (value.ExtraPrice < 0 || value.StockQuantity < 0)
                {
                    errors.Add($"قيم الأسعار أو المخزون داخل مجموعة {group.Name} يجب ألا تكون سالبة.");
                }

                if (value.IsActive && !activeLabels.Add(label))
                {
                    errors.Add($"لا يمكن تكرار قيمة الخيار {label} داخل المجموعة نفسها.");
                }
            }

            if (group.IsRequired && group.Values.All(value => !value.IsActive))
            {
                errors.Add($"مجموعة الخيارات {group.Name} إجبارية وتحتاج إلى قيمة متاحة واحدة على الأقل.");
            }
        }

        foreach (var (field, index) in request.CustomizationFields.Select((item, index) => (item, index)))
        {
            if (string.IsNullOrWhiteSpace(field.Label))
            {
                errors.Add($"اسم حقل التخصيص رقم {index + 1} مطلوب.");
            }

            if (field.AdditionalPrice < 0)
            {
                errors.Add($"تكلفة حقل التخصيص {field.Label} لا يمكن أن تكون سالبة.");
            }

            if (field.MaxLength.HasValue && field.MinLength.HasValue && field.MaxLength < field.MinLength)
            {
                errors.Add($"حدود الطول في حقل {field.Label} غير صحيحة.");
            }

            if (field.MaxValue.HasValue && field.MinValue.HasValue && field.MaxValue < field.MinValue)
            {
                errors.Add($"حدود الرقم في حقل {field.Label} غير صحيحة.");
            }

            var fieldType = ParseFieldType(field.Type);
            if ((fieldType == ProductCustomizationFieldType.SingleSelect || fieldType == ProductCustomizationFieldType.MultiSelect) && field.Choices.Count == 0)
            {
                errors.Add($"حقل {field.Label} يحتاج إلى خيارات.");
            }

            if (fieldType == ProductCustomizationFieldType.ImageUpload && field.AllowedFiles.Count == 0)
            {
                errors.Add($"حقل رفع الصور {field.Label} يحتاج إلى أنواع ملفات مسموحة.");
            }
        }

        var slug = Slugify(string.IsNullOrWhiteSpace(request.Slug) ? request.Name : request.Slug);
        var slugExists = await _dbContext.Products.AnyAsync(product => product.Slug == slug && product.Id != existingProductId);
        if (slugExists)
        {
            errors.Add("رابط المنتج مستخدم مسبقًا.");
        }

        return errors;
    }

    private async Task<string> CreateUniqueSlugAsync(string? requestedSlug, string name, Guid? existingProductId = null)
    {
        var baseSlug = Slugify(string.IsNullOrWhiteSpace(requestedSlug) ? name : requestedSlug);
        var slug = baseSlug;
        var counter = 2;

        while (await _dbContext.Products.AnyAsync(product => product.Slug == slug && product.Id != existingProductId))
        {
            slug = $"{baseSlug}-{counter++}";
        }

        return slug;
    }

    private async Task<string> CreateUniqueCategorySlugAsync(string? requestedSlug, string name)
    {
        var baseSlug = Slugify(string.IsNullOrWhiteSpace(requestedSlug) ? name : requestedSlug);
        var slug = baseSlug;
        var counter = 2;

        while (await _dbContext.ProductCategories.AnyAsync(category => category.Slug == slug))
        {
            slug = $"{baseSlug}-{counter++}";
        }

        return slug;
    }

    private static AdminProductListItemDto ToAdminListItem(Product product)
    {
        var primaryImage = product.Images.OrderByDescending(image => image.IsPrimary).ThenBy(image => image.DisplayOrder).FirstOrDefault();

        return new AdminProductListItemDto
        {
            Id = product.Id,
            Name = product.Name,
            Slug = product.Slug,
            CategoryName = product.Category.Name,
            Status = ToStatusKey(product.Status),
            PricingType = ToPricingTypeKey(product.PricingType),
            BasePrice = product.PricingType == ProductPricingType.Quote ? null : product.BasePrice,
            PriceLabel = BuildPriceLabel(product),
            InventoryLabel = BuildInventoryLabel(product),
            IsLowStock = product.InventoryTrackingEnabled && !product.MadeToOrder && product.Quantity <= product.LowStockThreshold,
            HasCustomizations = product.HasVariants || product.CustomizationFields.Count > 0,
            IsFeatured = product.IsFeatured,
            IsVisible = IsCustomerVisible(product.Status),
            DisplayOrder = product.DisplayOrder,
            PrimaryImageUrl = primaryImage?.ImageUrl ?? string.Empty,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        };
    }

    private static ProductDetailsDto ToDetails(Product product)
    {
        return new ProductDetailsDto
        {
            Id = product.Id,
            Name = product.Name,
            Slug = product.Slug,
            ShortDescription = product.ShortDescription ?? string.Empty,
            Description = product.Description ?? string.Empty,
            CategoryId = product.CategoryId,
            Category = ToCategoryDto(product.Category),
            Status = ToStatusKey(product.Status),
            PricingType = ToPricingTypeKey(product.PricingType),
            BasePrice = product.PricingType == ProductPricingType.Quote ? null : product.BasePrice,
            IsPriceVisible = product.IsPriceVisible,
            PriceLabel = BuildPriceLabel(product),
            Currency = product.Currency,
            HasVariants = product.HasVariants,
            InventoryTrackingEnabled = product.InventoryTrackingEnabled,
            Quantity = product.Quantity,
            LowStockThreshold = product.LowStockThreshold,
            MadeToOrder = product.MadeToOrder,
            AllowBackorder = product.AllowBackorder,
            MinimumQuantity = product.MinimumQuantity,
            MaximumQuantity = product.MaximumQuantity,
            MinPreparationDays = product.MinPreparationDays,
            MaxPreparationDays = product.MaxPreparationDays,
            PreparationUnit = ToPreparationUnitKey(product.PreparationUnit),
            PreparationNote = product.PreparationNote ?? string.Empty,
            ShowOnHomepage = product.ShowOnHomepage,
            IsFeatured = product.IsFeatured,
            IsNew = product.IsNew,
            ShowInSuggestions = product.ShowInSuggestions,
            DirectAccessOnly = product.DirectAccessOnly,
            AllowRatings = product.AllowRatings,
            AllowOrdering = product.AllowOrdering,
            DisplayOrder = product.DisplayOrder,
            VisibleFrom = product.VisibleFrom,
            VisibleTo = product.VisibleTo,
            Images = product.Images.OrderBy(image => image.DisplayOrder).Select(image => new ProductImageResponseDto
            {
                Id = image.Id,
                ImageUrl = image.ImageUrl,
                AltText = image.AltText ?? product.Name,
                IsPrimary = image.IsPrimary,
                DisplayOrder = image.DisplayOrder
            }).ToList(),
            OptionGroups = product.OptionGroups.OrderBy(group => group.DisplayOrder).Select(group => new ProductOptionGroupDto
            {
                Id = group.Id,
                Name = group.Name,
                Description = group.Description ?? string.Empty,
                IsRequired = group.IsRequired,
                IsActive = group.IsActive,
                DisplayOrder = group.DisplayOrder,
                Values = group.Values.OrderBy(value => value.DisplayOrder).Select(value => new ProductOptionValueDto
                {
                    Id = value.Id,
                    Label = value.Label,
                    ExtraPrice = value.ExtraPrice,
                    DisplayOrder = value.DisplayOrder,
                    IsActive = value.IsActive,
                    IsDefault = value.IsDefault,
                    StockQuantity = value.StockQuantity,
                    Sku = value.Sku ?? string.Empty,
                    ImageUrl = value.ImageUrl ?? string.Empty
                }).ToList()
            }).ToList(),
            CustomizationFields = product.CustomizationFields.OrderBy(field => field.DisplayOrder).Select(field => new ProductCustomizationFieldDto
            {
                Id = field.Id,
                Label = field.Label,
                Type = ToFieldTypeKey(field.FieldType),
                Description = field.Description ?? string.Empty,
                Placeholder = field.Placeholder ?? string.Empty,
                IsRequired = field.IsRequired,
                DisplayOrder = field.DisplayOrder,
                AdditionalPrice = field.AdditionalPrice,
                MinLength = field.MinLength,
                MaxLength = field.MaxLength,
                MinValue = field.MinValue,
                MaxValue = field.MaxValue,
                AllowedFiles = SplitCsv(field.AllowedFilesCsv),
                IsActive = field.IsActive,
                Choices = field.Choices.OrderBy(choice => choice.DisplayOrder).Select(choice => new ProductCustomizationChoiceDto
                {
                    Id = choice.Id,
                    Label = choice.Label,
                    AdditionalPrice = choice.AdditionalPrice,
                    DisplayOrder = choice.DisplayOrder,
                    IsActive = choice.IsActive
                }).ToList()
            }).ToList()
        };
    }

    private static ProductCategoryResponseDto ToCategoryDto(ProductCategory category)
    {
        return new ProductCategoryResponseDto
        {
            Id = category.Id,
            Name = category.Name,
            Slug = category.Slug,
            Description = category.Description ?? string.Empty,
            ImageUrl = category.ImageUrl ?? string.Empty
        };
    }

    private static ProductStatus ParseStatus(string status)
    {
        return status.Trim().ToLowerInvariant() switch
        {
            "draft" => ProductStatus.Draft,
            "published" => ProductStatus.Published,
            "active" => ProductStatus.Active,
            "hidden" => ProductStatus.Hidden,
            "inactive" => ProductStatus.Inactive,
            "unavailable" => ProductStatus.Unavailable,
            "outofstock" or "out_of_stock" => ProductStatus.OutOfStock,
            "archived" => ProductStatus.Archived,
            _ => ProductStatus.Draft
        };
    }

    private static bool TryParseStatus(string status, out ProductStatus productStatus)
    {
        productStatus = ParseStatus(status);
        return true;
    }

    private static ProductPricingType ParsePricingType(string pricingType)
    {
        return pricingType.Trim().ToLowerInvariant() switch
        {
            "startingfrom" or "starting_from" => ProductPricingType.StartingFrom,
            "optionsbased" or "options_based" => ProductPricingType.OptionsBased,
            "quote" => ProductPricingType.Quote,
            _ => ProductPricingType.Fixed
        };
    }

    private static ProductCustomizationFieldType ParseFieldType(string fieldType)
    {
        return fieldType.Trim().ToLowerInvariant() switch
        {
            "longtext" or "long_text" => ProductCustomizationFieldType.LongText,
            "imageupload" or "image_upload" => ProductCustomizationFieldType.ImageUpload,
            "singleselect" or "single_select" => ProductCustomizationFieldType.SingleSelect,
            "multiselect" or "multi_select" => ProductCustomizationFieldType.MultiSelect,
            "checkbox" => ProductCustomizationFieldType.Checkbox,
            "date" => ProductCustomizationFieldType.Date,
            "number" => ProductCustomizationFieldType.Number,
            _ => ProductCustomizationFieldType.ShortText
        };
    }

    private static ProductPreparationUnit ParsePreparationUnit(string preparationUnit)
    {
        return preparationUnit.Trim().ToLowerInvariant() switch
        {
            "weeks" => ProductPreparationUnit.Weeks,
            "custom" => ProductPreparationUnit.Custom,
            _ => ProductPreparationUnit.Days
        };
    }

    private static string ToStatusKey(ProductStatus status)
    {
        return status switch
        {
            ProductStatus.Published or ProductStatus.Active => "published",
            ProductStatus.Hidden or ProductStatus.Inactive => "hidden",
            ProductStatus.Unavailable or ProductStatus.OutOfStock => "unavailable",
            ProductStatus.Archived => "archived",
            _ => "draft"
        };
    }

    private static string ToPricingTypeKey(ProductPricingType pricingType)
    {
        return pricingType switch
        {
            ProductPricingType.StartingFrom => "startingFrom",
            ProductPricingType.OptionsBased => "optionsBased",
            ProductPricingType.Quote => "quote",
            _ => "fixed"
        };
    }

    private static string ToFieldTypeKey(ProductCustomizationFieldType fieldType)
    {
        return fieldType switch
        {
            ProductCustomizationFieldType.LongText => "longText",
            ProductCustomizationFieldType.ImageUpload => "imageUpload",
            ProductCustomizationFieldType.SingleSelect => "singleSelect",
            ProductCustomizationFieldType.MultiSelect => "multiSelect",
            ProductCustomizationFieldType.Checkbox => "checkbox",
            ProductCustomizationFieldType.Date => "date",
            ProductCustomizationFieldType.Number => "number",
            _ => "shortText"
        };
    }

    private static string ToPreparationUnitKey(ProductPreparationUnit preparationUnit)
    {
        return preparationUnit switch
        {
            ProductPreparationUnit.Weeks => "weeks",
            ProductPreparationUnit.Custom => "custom",
            _ => "days"
        };
    }

    private static bool IsCustomerVisible(ProductStatus status)
    {
        return status is ProductStatus.Published or ProductStatus.Active or ProductStatus.OutOfStock or ProductStatus.Unavailable;
    }

    private static string BuildPriceLabel(Product product)
    {
        if (!product.IsPriceVisible || product.PricingType == ProductPricingType.Quote)
        {
            return string.IsNullOrWhiteSpace(product.PriceLabel) ? "السعر عند الطلب" : product.PriceLabel;
        }

        var formatted = product.BasePrice.ToString("0.##", CultureInfo.InvariantCulture);
        return product.PricingType switch
        {
            ProductPricingType.StartingFrom => $"يبدأ من {formatted} شيكل",
            ProductPricingType.OptionsBased => $"حسب الخيارات من {formatted} شيكل",
            _ => $"{formatted} شيكل"
        };
    }

    private static string BuildInventoryLabel(Product product)
    {
        if (product.MadeToOrder)
        {
            return "مصنوع حسب الطلب";
        }

        if (!product.InventoryTrackingEnabled)
        {
            return "بدون تتبع مخزون";
        }

        return $"الكمية {product.Quantity ?? 0}";
    }

    private static IReadOnlyList<string> SplitCsv(string? csv)
    {
        return string.IsNullOrWhiteSpace(csv)
            ? []
            : csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static string NormalizeText(string? value)
    {
        return string.Join(' ', (value ?? string.Empty).Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static string? NormalizeNullableText(string? value)
    {
        var normalized = NormalizeText(value);
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static string Slugify(string? value)
    {
        var normalized = NormalizeText(value).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            normalized = Guid.NewGuid().ToString("N")[..8];
        }

        var builder = new StringBuilder();
        foreach (var character in normalized)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
            }
            else if (char.IsWhiteSpace(character) || character is '-' or '_')
            {
                builder.Append('-');
            }
        }

        var slug = builder.ToString().Trim('-');
        while (slug.Contains("--", StringComparison.Ordinal))
        {
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        }

        return string.IsNullOrWhiteSpace(slug) ? Guid.NewGuid().ToString("N")[..8] : slug;
    }
}
