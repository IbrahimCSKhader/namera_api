using Microsoft.EntityFrameworkCore;
using namera_API.Models.Products.Categories;
using namera_API.Models.Products.Enums;
using namera_API.Models.Products.Products;

namespace namera_API.Data.Seed;

public static class ProductSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var dbContext = serviceProvider.GetRequiredService<AppDbContext>();

        var categories = await UpsertCategoriesAsync(dbContext);
        await UpsertProductsAsync(dbContext, categories);
    }

    private static async Task<List<ProductCategory>> UpsertCategoriesAsync(AppDbContext dbContext)
    {
        var seededCategories = CreateCategories();
        var categories = new List<ProductCategory>();

        foreach (var seededCategory in seededCategories)
        {
            var category = await dbContext.ProductCategories
                .FirstOrDefaultAsync(item => item.Slug == seededCategory.Slug);

            if (category is null)
            {
                dbContext.ProductCategories.Add(seededCategory);
                categories.Add(seededCategory);
                continue;
            }

            category.Name = seededCategory.Name;
            category.Description = seededCategory.Description;
            category.ImageUrl = seededCategory.ImageUrl;
            category.DisplayOrder = seededCategory.DisplayOrder;
            category.IsActive = true;
            category.UpdatedAt = DateTime.UtcNow;
            categories.Add(category);
        }

        await dbContext.SaveChangesAsync();
        return categories;
    }

    private static async Task UpsertProductsAsync(AppDbContext dbContext, IReadOnlyList<ProductCategory> categories)
    {
        var seededProducts = CreateProducts(categories);

        foreach (var seededProduct in seededProducts)
        {
            var product = await dbContext.Products
                .Include(item => item.Images)
                .Include(item => item.OptionGroups)
                    .ThenInclude(group => group.Values)
                .Include(item => item.CustomizationFields)
                    .ThenInclude(field => field.Choices)
                .FirstOrDefaultAsync(item => item.Slug == seededProduct.Slug);

            if (product is null)
            {
                dbContext.Products.Add(seededProduct);
                continue;
            }

            product.CategoryId = seededProduct.Category.Id;
            product.Name = seededProduct.Name;
            product.ShortDescription = seededProduct.ShortDescription;
            product.Description = seededProduct.Description;
            product.BasePrice = seededProduct.BasePrice;
            product.Status = seededProduct.Status;
            product.IsFeatured = seededProduct.IsFeatured;
            product.IsCustomizable = seededProduct.IsCustomizable;
            product.MinimumQuantity = seededProduct.MinimumQuantity;
            product.MaximumQuantity = seededProduct.MaximumQuantity;
            product.PreparationTimeInDays = seededProduct.PreparationTimeInDays;
            product.DisplayOrder = seededProduct.DisplayOrder;
            product.UpdatedAt = DateTime.UtcNow;

            dbContext.ProductImages.RemoveRange(product.Images);
            product.Images.Clear();

            foreach (var image in seededProduct.Images)
            {
                product.Images.Add(new ProductImage
                {
                    ImageUrl = image.ImageUrl,
                    AltText = image.AltText,
                    IsPrimary = image.IsPrimary,
                    DisplayOrder = image.DisplayOrder
                });
            }

            if (seededProduct.OptionGroups.Count > 0 || seededProduct.CustomizationFields.Count > 0)
            {
                dbContext.ProductOptionGroups.RemoveRange(product.OptionGroups);
                dbContext.ProductCustomizationFields.RemoveRange(product.CustomizationFields);
                product.OptionGroups.Clear();
                product.CustomizationFields.Clear();

                CopyCustomizations(product, seededProduct);
            }
        }

        await dbContext.SaveChangesAsync();
    }

    private static List<ProductCategory> CreateCategories()
    {
        return
        [
            CreateCategory("\u062d\u0641\u0638 \u0627\u0644\u0632\u0647\u0648\u0631", "preserved-flowers", 0, 1),
            CreateCategory("\u0645\u0631\u0627\u064a\u0627 \u0627\u0644\u0633\u064a\u0627\u0631\u0627\u062a", "car-mirror-charms", 1, 2),
            CreateCategory("\u0645\u064a\u062f\u0627\u0644\u064a\u0627\u062a", "keychains", 2, 3),
            CreateCategory("\u062a\u0637\u0631\u064a\u0632", "embroidery", 3, 4),
            CreateCategory("\u0633\u0627\u0639\u0627\u062a", "watches", 4, 5)
        ];
    }

    private static ProductCategory CreateCategory(string name, string slug, int imageIndex, int displayOrder)
    {
        return new ProductCategory
        {
            Name = name,
            Slug = slug,
            Description = CategoryDescription,
            ImageUrl = ImagePool[imageIndex],
            DisplayOrder = displayOrder,
            IsActive = true
        };
    }

    private static List<Product> CreateProducts(IReadOnlyList<ProductCategory> categories)
    {
        var flower = categories.Single(category => category.Slug == "preserved-flowers");
        var mirror = categories.Single(category => category.Slug == "car-mirror-charms");
        var keychain = categories.Single(category => category.Slug == "keychains");
        var embroidery = categories.Single(category => category.Slug == "embroidery");
        var watches = categories.Single(category => category.Slug == "watches");

        return
        [
            CreateProduct(flower, "\u0628\u0644\u0648\u0643 \u062d\u0641\u0638 \u0628\u0627\u0642\u0629 \u0627\u0644\u0639\u0631\u0648\u0633", "bridal-bouquet-resin-block", 180, true, 1, 7),
            CreateProduct(flower, "\u0642\u0627\u0644\u0628 \u0648\u0631\u062f \u062a\u0630\u0643\u0627\u0631\u064a", "memory-flower-cast", 120, true, 2, 6),
            CreateProduct(mirror, "\u062a\u0639\u0644\u064a\u0642\u0629 \u0645\u0631\u0622\u0629 \u0628\u0627\u0633\u0645", "custom-name-car-charm", 45, true, 3, 4),
            CreateProduct(mirror, "\u062a\u0639\u0644\u064a\u0642\u0629 \u0633\u064a\u0627\u0631\u0629 \u062f\u0627\u0626\u0631\u064a\u0629", "round-floral-car-charm", 39, false, 4, 3),
            CreateProduct(keychain, "\u0645\u064a\u062f\u0627\u0644\u064a\u0629 \u062d\u0631\u0641 \u0634\u0641\u0627\u0641\u0629", "clear-letter-keychain", 25, true, 5, 2),
            CreateProduct(keychain, "\u0645\u064a\u062f\u0627\u0644\u064a\u0629 \u0627\u0633\u0645 \u0645\u0632\u062f\u0648\u062c\u0629", "double-name-keychain", 32, false, 6, 2),
            CreateProduct(embroidery, "\u0637\u0627\u0631\u0629 \u062a\u0637\u0631\u064a\u0632 \u0648\u0631\u062f\u064a\u0629", "floral-embroidery-hoop", 95, true, 7, 5),
            CreateProduct(embroidery, "\u0647\u062f\u064a\u0629 \u062a\u0637\u0631\u064a\u0632 \u0648\u0631\u064a\u0632\u0646", "embroidery-resin-gift", 110, false, 8, 6),
            CreateProduct(watches, "\u0633\u0627\u0639\u0629 \u0631\u064a\u0632\u0646 \u0628\u062d\u0631\u064a\u0629", "ocean-resin-clock", 160, true, 9, 7),
            CreateProduct(watches, "\u0633\u0627\u0639\u0629 \u064a\u062f \u0628\u062a\u0635\u0645\u064a\u0645 \u0631\u064a\u0632\u0646", "resin-watch-face", 140, false, 10, 5),
            CreatePersonalizedTrayProduct(flower)
        ];
    }

    private static Product CreateProduct(
        ProductCategory category,
        string name,
        string slug,
        decimal price,
        bool isFeatured,
        int displayOrder,
        int preparationTime)
    {
        var product = new Product
        {
            Category = category,
            CategoryId = category.Id,
            Name = name,
            Slug = slug,
            ShortDescription = ProductDescription,
            Description = $"{ProductDescription} {ProductCustomizationDetails}",
            BasePrice = price,
            Status = ProductStatus.Active,
            IsFeatured = isFeatured,
            IsCustomizable = true,
            MinimumQuantity = 1,
            MaximumQuantity = 20,
            PreparationTimeInDays = preparationTime,
            DisplayOrder = displayOrder
        };

        foreach (var image in PickImages(displayOrder))
        {
            product.Images.Add(new ProductImage
            {
                ImageUrl = image.Url,
                AltText = $"{name} - {ImageLabel} {image.Order}",
                IsPrimary = image.Order == 1,
                DisplayOrder = image.Order
            });
        }

        return product;
    }

    private static Product CreatePersonalizedTrayProduct(ProductCategory category)
    {
        var product = CreateProduct(
            category,
            "\u0635\u064a\u0646\u064a\u0629 \u0631\u064a\u0632\u0646 \u0634\u062e\u0635\u064a\u0629 \u0628\u0627\u0644\u0627\u0633\u0645",
            "personalized-name-resin-tray",
            135,
            true,
            11,
            6);

        product.ShortDescription = "\u0635\u064a\u0646\u064a\u0629 \u0631\u064a\u0632\u0646 \u0645\u0635\u0646\u0648\u0639\u0629 \u062d\u0633\u0628 \u0627\u0644\u0637\u0644\u0628 \u0645\u0639 \u0627\u0633\u0645 \u0623\u0648 \u0639\u0628\u0627\u0631\u0629 \u062e\u0627\u0635\u0629.";
        product.Description = "\u0635\u064a\u0646\u064a\u0629 \u0631\u064a\u0632\u0646 \u0623\u0646\u064a\u0642\u0629 \u0644\u0644\u0647\u062f\u0627\u064a\u0627 \u0648\u0627\u0644\u0645\u0646\u0627\u0633\u0628\u0627\u062a\u060c \u064a\u0645\u0643\u0646 \u062a\u062e\u0635\u064a\u0635 \u0644\u0648\u0646\u0647\u0627\u060c \u0627\u0644\u0627\u0633\u0645 \u0627\u0644\u0645\u0643\u062a\u0648\u0628 \u0639\u0644\u064a\u0647\u0627\u060c \u0648\u0637\u0631\u064a\u0642\u0629 \u0627\u0644\u062a\u063a\u0644\u064a\u0641.";
        product.MadeToOrder = true;
        product.InventoryTrackingEnabled = false;
        product.Quantity = null;
        product.LowStockThreshold = 0;
        product.HasVariants = true;
        product.ShowOnHomepage = true;
        product.ShowInSuggestions = true;
        product.IsNew = true;
        product.MaxPreparationDays = 7;
        product.MinPreparationDays = 4;
        product.PreparationNote = "\u064a\u062a\u0645 \u062a\u0646\u0641\u064a\u0630 \u0627\u0644\u0637\u0644\u0628 \u064a\u062f\u0648\u064a\u0627 \u062d\u0633\u0628 \u0627\u0644\u062a\u062e\u0635\u064a\u0635\u0627\u062a \u0627\u0644\u0645\u062e\u062a\u0627\u0631\u0629.";

        product.OptionGroups.Add(new ProductOptionGroup
        {
            Name = "\u0644\u0648\u0646 \u0627\u0644\u062a\u0635\u0645\u064a\u0645",
            Description = "\u0627\u062e\u062a\u0627\u0631\u064a \u0627\u0644\u0644\u0648\u0646 \u0627\u0644\u0623\u0642\u0631\u0628 \u0644\u0630\u0648\u0642\u0643.",
            IsRequired = true,
            IsActive = true,
            DisplayOrder = 1,
            Values =
            {
                new ProductOptionValue { Label = "\u0648\u0631\u062f\u064a \u0646\u0627\u0639\u0645", ExtraPrice = 0, DisplayOrder = 1, IsActive = true, IsDefault = true },
                new ProductOptionValue { Label = "\u0628\u0646\u0641\u0633\u062c\u064a \u0644\u0627\u0645\u0639", ExtraPrice = 10, DisplayOrder = 2, IsActive = true },
                new ProductOptionValue { Label = "\u0630\u0647\u0628\u064a \u0648\u0634\u0641\u0627\u0641", ExtraPrice = 15, DisplayOrder = 3, IsActive = true }
            }
        });

        product.CustomizationFields.Add(new ProductCustomizationField
        {
            Label = "\u0627\u0644\u0627\u0633\u0645 \u0623\u0648 \u0627\u0644\u0639\u0628\u0627\u0631\u0629",
            FieldType = ProductCustomizationFieldType.ShortText,
            Description = "\u0627\u0644\u0646\u0635 \u0627\u0644\u0630\u064a \u0633\u064a\u0638\u0647\u0631 \u0639\u0644\u0649 \u0627\u0644\u0635\u064a\u0646\u064a\u0629.",
            Placeholder = "\u0645\u062b\u0627\u0644: Namera",
            IsRequired = true,
            DisplayOrder = 2,
            AdditionalPrice = 0,
            MinLength = 2,
            MaxLength = 40,
            IsActive = true
        });

        product.CustomizationFields.Add(new ProductCustomizationField
        {
            Label = "\u0627\u0644\u062a\u063a\u0644\u064a\u0641",
            FieldType = ProductCustomizationFieldType.SingleSelect,
            Description = "\u0627\u062e\u062a\u0627\u0631\u064a \u0637\u0631\u064a\u0642\u0629 \u062a\u062c\u0647\u064a\u0632 \u0627\u0644\u0647\u062f\u064a\u0629.",
            IsRequired = true,
            DisplayOrder = 3,
            AdditionalPrice = 0,
            IsActive = true,
            Choices =
            {
                new ProductCustomizationChoice { Label = "\u0628\u062f\u0648\u0646 \u062a\u063a\u0644\u064a\u0641", AdditionalPrice = 0, DisplayOrder = 1, IsActive = true },
                new ProductCustomizationChoice { Label = "\u0643\u0631\u062a \u0647\u062f\u064a\u0629", AdditionalPrice = 8, DisplayOrder = 2, IsActive = true },
                new ProductCustomizationChoice { Label = "\u0635\u0646\u062f\u0648\u0642 \u0641\u0627\u062e\u0631", AdditionalPrice = 20, DisplayOrder = 3, IsActive = true }
            }
        });

        return product;
    }

    private static void CopyCustomizations(Product product, Product seededProduct)
    {
        foreach (var group in seededProduct.OptionGroups)
        {
            product.OptionGroups.Add(new ProductOptionGroup
            {
                Name = group.Name,
                Description = group.Description,
                IsRequired = group.IsRequired,
                IsActive = group.IsActive,
                DisplayOrder = group.DisplayOrder,
                Values = group.Values.Select(value => new ProductOptionValue
                {
                    Label = value.Label,
                    ExtraPrice = value.ExtraPrice,
                    DisplayOrder = value.DisplayOrder,
                    IsActive = value.IsActive,
                    IsDefault = value.IsDefault,
                    StockQuantity = value.StockQuantity,
                    Sku = value.Sku,
                    ImageUrl = value.ImageUrl
                }).ToList()
            });
        }

        foreach (var field in seededProduct.CustomizationFields)
        {
            product.CustomizationFields.Add(new ProductCustomizationField
            {
                Label = field.Label,
                FieldType = field.FieldType,
                Description = field.Description,
                Placeholder = field.Placeholder,
                IsRequired = field.IsRequired,
                DisplayOrder = field.DisplayOrder,
                AdditionalPrice = field.AdditionalPrice,
                MinLength = field.MinLength,
                MaxLength = field.MaxLength,
                MinValue = field.MinValue,
                MaxValue = field.MaxValue,
                AllowedFilesCsv = field.AllowedFilesCsv,
                IsActive = field.IsActive,
                Choices = field.Choices.Select(choice => new ProductCustomizationChoice
                {
                    Label = choice.Label,
                    AdditionalPrice = choice.AdditionalPrice,
                    DisplayOrder = choice.DisplayOrder,
                    IsActive = choice.IsActive
                }).ToList()
            });
        }
    }

    private static IEnumerable<(string Url, int Order)> PickImages(int seed)
    {
        for (var index = 0; index < 6; index++)
        {
            var imageIndex = (seed + index - 1) % ImagePool.Length;
            yield return (ImagePool[imageIndex], index + 1);
        }
    }

    private const string CategoryDescription = "\u062a\u0635\u0627\u0645\u064a\u0645 \u0631\u064a\u0632\u0646 \u064a\u062f\u0648\u064a\u0629 \u0642\u0627\u0628\u0644\u0629 \u0644\u0644\u062a\u062e\u0635\u064a\u0635.";
    private const string ProductDescription = "\u0642\u0637\u0639\u0629 \u064a\u062f\u0648\u064a\u0629 \u0645\u0635\u0646\u0648\u0639\u0629 \u0628\u0639\u0646\u0627\u064a\u0629 \u0648\u064a\u0645\u0643\u0646 \u062a\u062e\u0635\u064a\u0635\u0647\u0627 \u062d\u0633\u0628 \u0627\u0644\u0645\u0646\u0627\u0633\u0628\u0629.";
    private const string ProductCustomizationDetails = "\u064a\u062a\u0645 \u062a\u0646\u0641\u064a\u0630 \u0627\u0644\u0642\u0637\u0639\u0629 \u064a\u062f\u0648\u064a\u0627 \u062d\u0633\u0628 \u0627\u0644\u0637\u0644\u0628 \u0645\u0639 \u0625\u0645\u0643\u0627\u0646\u064a\u0629 \u0627\u062e\u062a\u064a\u0627\u0631 \u0627\u0644\u0644\u0648\u0646\u060c \u0627\u0644\u0627\u0633\u0645\u060c \u0648\u0627\u0644\u062a\u063a\u0644\u064a\u0641 \u0627\u0644\u0645\u0646\u0627\u0633\u0628 \u0644\u0644\u0645\u0646\u0627\u0633\u0628\u0629.";
    private const string ImageLabel = "\u0635\u0648\u0631\u0629";

    private static readonly string[] ImagePool =
    [
        "https://lh3.googleusercontent.com/aida-public/AB6AXuCAEDmBxNRxfV61iTuF-WG5G_gj7_0HQgImM4HwwKz7MJkg3U9yHsR6lAgv28sX4HVMiwQYxlDAB5HakfzBgKaOuoDdPGth9Gs0z9qm6hToedBAGOCOT80a8cqA5iBwj0-Z9Frf9U3_FfvT6lQLB5BxdOZeIyPIE6MNHPWUiRS1eB1B_sMQzov9sK1ZXJ3iAMhk-yYTf3QUbWHgD6fiTsfMEScx3Inl59_euRfQYvQwIQ8fQKc3LzXa8YGKqtqdWkk-B6RN99l9mOet",
        "https://lh3.googleusercontent.com/aida-public/AB6AXuDrVKUiEuarb4CDFW-hp3CUfOxh-qVWFYiGncBB-cE-sSX1gTgefKpHqc-OeEoCCT2hiYmqv6_yaoc6_XJhUKI3j7dT8BvYHCE9zj_zPfdTXP9tT8T7zPe9O80hEg6Emlg12jU4g6qAF1oVTrUPkOFIu7u-aLqxOC3ISY7vD-h6TqyDTWKrmzY83JuRqZj1SPdoiAZYx5w1jOSmt3YJv0uzDvP6_VFkCJXLHW-qORIwOggwl1R0zyJmxmPwSrJGAcfaVukTeQzbnfJg",
        "https://lh3.googleusercontent.com/aida-public/AB6AXuAaYwZmNAPCn3EXr0JwfyvRChm4hei5YEvNrJW_b5lJwLgSyKTTfUGdKotcml2-9BQrN8iZ8hos4sf0zQDTIjM03aEF4dlC0pJBfZ2HDU-knUz0g8hRXOcL1xPjPPPPZyUZsvawwQxGncpxMB8zA6T5qnBTujZJCTtayaaLDt5UfIdEcWIjxp8ZzQELKmUQ-ZTDquiHs92LBi8dhlqhGPtIpLNLW92ViB5gl1kOtNNNEY4XydJGozgufuztYj-0q_eW0i_ZepiEoJLr",
        "https://lh3.googleusercontent.com/aida-public/AB6AXuCMn9LDPULXdHGKgsKdO-2ZI5_1DGwQ_VjH-VimcdzxhN7o2fl8BMXazjTUQk28VdcHJgNefCAK_-6ZoXdqIqvbgdK5hPthEAbRO-ei3leEr4fh_UGryJw_arZNHGnHYANneHPFm9wHsPbDNGQHCXBtRdAxNR32EYsXB9ztAP9ejtlDanND9DBZvGZW1R8PTdgj0xH32yf7ZAl_wtZhZwxq4Wun3_Wqd6ZliTqTITOdMhI9Cg0IVrubJRCHhAIzHywnvmhoygDMEwYN",
        "https://lh3.googleusercontent.com/aida-public/AB6AXuC4kiQy17LYMqdN4q_w7MHEl7rRk3F8jvbwKIgI3DjjbbmqIp-l6gr9lKVSZ1aSOdU8haIuZ3scO95UTem45pGh42V2-9aMwWlgk9FTefgqJhK0r6INB8WcxmN5g3w-nVbP-VprtCQx4qLyq8w3wwfgUL1OaF94SDunO61c9frPw95jSpK97ZNOJ2aKlfM-T1H2-z0FBI7wXhM0i-n08ABWM1BM937AA7hSq0UrleXm2YGDBZ_zJaYlheMN3kUD3uttUn8SPa9v-rrY",
        "https://lh3.googleusercontent.com/aida-public/AB6AXuA0QYp5oMW2aNdFeRUrybX3hLumIuI99OtwxYYMDi51tNAInaKgvg-kPmpYSLMil_hGPEW3Z_GgZrJJGoeikEs3jsoJVDzxCFSmf3hRcgr4-_lIYk6lKiuj01m6rRy9myfgIvSE1TmerJMljeK-FbSra8eecuN9-4HOw9gILkl-mPUO-FdhX-leDdIv78gKMiHePhanxst58h_BxwqdMphUHdtqm025qI7KqUNvsadu0YOx8-UG_SmTOOg2K_xq86v5IDuWWh5Tt3MO",
        "https://lh3.googleusercontent.com/aida-public/AB6AXuCD8cy3-oTUISlX0PtEXKgOOb51M-074qPwE3h-_MSdVV9jJvmo2R1czy78EBSdVfxotC_5ku3XB2vK9F-qScElRUhmGufanU1-jle92LdlzxvjRdCLE43y60olgBhzR1vYtCs0jw-98PHPZ7LnG6l2htO31KLC8WqENKOZBwHNAS-HPAko-G9PD3WrVW-XcAkw8HsEYfJJhNT1gLmrpQ6bHCjc1fq6h00T9xPmn48Iw_lGldke1WRxUk6U0-fjYnzdCyq_CvfvYtXo",
        "https://lh3.googleusercontent.com/aida-public/AB6AXuBgAXR2GHU0Rw3M4KX8wNRS5WuMaelqNmt-TQKQfS17nvEqbPQjRKk_HT-OfOsMTDuKRCtQrdn-R0vtYfROwesmEN-VDp8V00DthVKFjksNL_-YIc7oxW84_aB4XP5yMkPSKx7ORxzK3aYH5aXwdUaC-pIWCVpBMiVUpQPr1kXto7Tn6w_aPYZaWmJhs2gT_Z4dH30r_NMpUFzfkiKNh9tbYfffgP4mCeMOcokiZJUMmQ8qRHH8op-MJrUTnMaNwyvu81YD7o2sR8Z7"
    ];
}
