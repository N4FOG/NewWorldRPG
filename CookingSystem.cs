using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace UrwReborn;

public class RecipeIngredientRequirement
{
    [JsonPropertyName("item_id")]
    public string ItemId { get; set; } = "";

    [JsonPropertyName("tag")]
    public string Tag { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("qty")]
    public int Qty { get; set; } = 1;

    [JsonPropertyName("consumed")]
    public bool Consumed { get; set; } = true;
}

public class RecipeProduct
{
    [JsonPropertyName("item_id")]
    public string ItemId { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("qty_min")]
    public int QtyMin { get; set; } = 1;

    [JsonPropertyName("qty_max")]
    public int QtyMax { get; set; } = 1;
}

public class RecipeSurvivalEffects
{
    [JsonPropertyName("warmth_bonus")]
    public float WarmthBonus { get; set; } = 0;

    [JsonPropertyName("nutrition")]
    public float Nutrition { get; set; } = 0;

    [JsonPropertyName("thirst")]
    public float Thirst { get; set; } = 0;

    [JsonPropertyName("energy")]
    public float Energy { get; set; } = 0;

    [JsonPropertyName("special_buff")]
    public string? SpecialBuff { get; set; }
}

public class CookingRecipe
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("category")]
    public string Category { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("cookware")]
    public string Cookware { get; set; } = "Nenhum";

    [JsonPropertyName("heat_required")]
    public string HeatRequired { get; set; } = "Nenhum";

    [JsonPropertyName("prep_minutes")]
    public int PrepMinutes { get; set; } = 5;

    [JsonPropertyName("cook_minutes")]
    public int CookMinutes { get; set; } = 15;

    [JsonPropertyName("ingredients")]
    public List<RecipeIngredientRequirement> Ingredients { get; set; } = new();

    [JsonPropertyName("products")]
    public List<RecipeProduct> Products { get; set; } = new();

    [JsonPropertyName("survival_effects")]
    public RecipeSurvivalEffects SurvivalEffects { get; set; } = new();
}

public static class CookingSystem
{
    public static List<CookingRecipe> Recipes { get; private set; } = new();
    public static List<string> Categories { get; private set; } = new();

    static CookingSystem()
    {
        LoadRecipes();
    }

    public static void LoadRecipes()
    {
        try
        {
            string basePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "data", "nordic_cooking_recipes.json"));
            if (!File.Exists(basePath))
            {
                basePath = @"k:\UnRealWorld\urw_reborn\data\nordic_cooking_recipes.json";
            }

            if (File.Exists(basePath))
            {
                string json = File.ReadAllText(basePath);
                var loaded = JsonSerializer.Deserialize<List<CookingRecipe>>(json);
                if (loaded != null)
                {
                    Recipes = loaded;
                    Categories = Recipes.Select(r => r.Category).Distinct().ToList();
                    Categories.Insert(0, "Todas as Receitas");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CookingSystem] Erro ao carregar receitas: {ex.Message}");
        }
    }

    // Retorna a contagem de um item ou tag no inventário do jogador
    // (Agora lê do inventário espacial único SpatialItems — sem mais mochila legada)
    public static int GetPlayerItemCount(RecipeIngredientRequirement req)
    {
        int count = 0;

        // Procura na mochila espacial em grid
        foreach (var item in InventoryHUD.SpatialItems)
        {
            if (item == null) continue;

            if (MatchesRequirement(item, req))
            {
                count += item.Count;
            }
        }

        // Procura também nos itens equipados (ex: faca, panela)
        for (int r = 0; r < 3; r++)
        {
            for (int c = 0; c < 3; c++)
            {
                var eq = InventoryHUD.Equipment[r, c];
                if (eq == null) continue;

                if (MatchesRequirement(eq, req))
                {
                    count += eq.Count;
                }
            }
        }

        return count;
    }

    public static bool MatchesRequirement(InventoryItem item, RecipeIngredientRequirement req)
    {
        // Comparação direta de ID
        if (!string.IsNullOrEmpty(req.ItemId) && item.Id.Equals(req.ItemId, StringComparison.OrdinalIgnoreCase))
            return true;

        // Comparação por nomes comuns / tags de ingredientes (Sistema BDO de Substituição de Categorias)
        string lowerName = item.Name.ToLowerInvariant();
        string lowerCategory = item.Category.ToLowerInvariant();

        return req.Tag switch
        {
            "Mushroom_Edible" => lowerName.Contains("cogumelo") && !lowerName.Contains("tóxico"),
            "Mushroom_Toxic" => lowerName.Contains("orelha-negra") || lowerName.Contains("tóxico"),
            "Leaf_Birch" => lowerName.Contains("bétula") || lowerName.Contains("folha"),
            "Liquid_Water" => lowerName.Contains("água") && !lowerName.Contains("salgada"),
            "Liquid_SaltWater" => lowerName.Contains("salgada") || lowerName.Contains("salmoura"),
            "Liquid_Milk" => lowerName.Contains("leite") || lowerName.Contains("nata"),
            "Liquid_Broth" => lowerName.Contains("caldo") || lowerName.Contains("tutano"),
            "Seasoning_Salt" => lowerName.Contains("sal"),
            "Fat_Butter" => lowerName.Contains("manteiga") || lowerName.Contains("gordura"),
            "Cereal_Grain" => lowerName.Contains("grão") || lowerName.Contains("centeio") || lowerName.Contains("trigo"),
            "Cereal_Flour" => lowerName.Contains("farinha"),
            "Meat_Quadruped" => lowerName.Contains("carne") || lowerName.Contains("alce") || lowerName.Contains("urso") || lowerName.Contains("rena"),
            "Fish_Freshwater" => lowerName.Contains("peixe") || lowerName.Contains("truta") || lowerName.Contains("salmão") || lowerName.Contains("lúcio"),
            "Berries_Wild" => lowerName.Contains("fruta") || lowerName.Contains("baga") || lowerName.Contains("mirtilo"),
            "Herb_Aromatic" => lowerName.Contains("erva") || lowerName.Contains("milefólio"),
            "Tool_Knife" => lowerName.Contains("faca") || lowerCategory.Contains("faca"),
            "Tool_Spit" => lowerName.Contains("graveto") || lowerName.Contains("espeto"),
            "Bread_Flatbread" => lowerName.Contains("pão") || lowerName.Contains("rieska"),
            "Dish_Stew" => lowerName.Contains("ensopado"),
            "Dish_RoastedFish" => lowerName.Contains("truta") && lowerName.Contains("brasa"),
            _ => false
        };
    }

    public static bool CanCraft(CookingRecipe recipe)
    {
        foreach (var req in recipe.Ingredients)
        {
            if (GetPlayerItemCount(req) < req.Qty)
                return false;
        }
        return true;
    }

    public static bool TryCook(CookingRecipe recipe, out string message, out int totalMinutesElapsed)
    {
        totalMinutesElapsed = recipe.PrepMinutes + recipe.CookMinutes;

        if (!CanCraft(recipe))
        {
            message = "Você não possui todos os ingredientes ou ferramentas necessárias!";
            return false;
        }

        // 1. Consome os ingredientes necessários da mochila espacial
        foreach (var req in recipe.Ingredients)
        {
            if (!req.Consumed) continue; // Ferramentas não são consumidas (ex: faca)

            int needed = req.Qty;
            for (int i = InventoryHUD.SpatialItems.Count - 1; i >= 0 && needed > 0; i--)
            {
                var item = InventoryHUD.SpatialItems[i];
                if (item != null && MatchesRequirement(item, req))
                {
                    int take = Math.Min(needed, item.Count);
                    item.Count -= take;
                    needed -= take;
                    if (item.Count <= 0)
                    {
                        InventoryHUD.SpatialItems.RemoveAt(i); // item esgotado sai do grid
                    }
                }
            }
        }

        // 2. Adiciona os produtos resultantes
        Random rnd = new Random();
        foreach (var prod in recipe.Products)
        {
            int yieldQty = rnd.Next(prod.QtyMin, prod.QtyMax + 1);
            AddItemToBackpack(prod.ItemId, prod.Name, yieldQty);
        }

        // 3. Aplica os efeitos fisiológicos imediatos (se provado/consumido) ou bônus de calor
        Program.ApplyCookingEffects(recipe.SurvivalEffects);

        message = $"Você preparou com sucesso: {recipe.Name}!";
        return true;
    }

    private static void AddItemToBackpack(string id, string name, int count)
    {
        // Adiciona ao inventário ESPACIAL em grid via API única
        // (empilha quando possível ou busca auto local no grid; marca state para persistência)
        InventoryHUD.AddItem(new InventoryItem
        {
            Id = id,
            Name = name,
            Count = count,
            Icon = GetIconForProduct(id),
            Category = "Comida / Processado",
            BaseWidth = 1,
            BaseHeight = 1,
            WeightKg = 0.3f,
            DurabilityPct = 100
        });
    }

    private static string GetIconForProduct(string id)
    {
        return id switch
        {
            "salt_pure" => "🧂",
            "water_salt" => "🍶",
            "water" => "💧",
            "flour_rye" => "🌾",
            "shroom_purified" => "🍄",
            "butter" => "🧈",
            "bone_broth" => "🥣",
            "herb_butter" => "🌿",
            "soup_mushroom" => "🍲",
            "stew_elk" => "🥘",
            "soup_salmon" => "🐟",
            "flatbread" => "🥖",
            "trout_roasted" => "🍢",
            "porridge_berry" => "🥣",
            "feast_hunter" => "👑",
            _ => "🍲"
        };
    }
}
