using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

Console.WriteLine("=== Week 3 Laboratory: Data Manipulation, Collections, LINQ ===\n");

var products = new List<Product>();
var inputProducts = new List<Product>
{
    new("SKU001", "Laptop", "Electronics", 48000m, 5),
    new("SKU002", "Mouse", "Electronics", 750m, 20),
    new("SKU003", "Keyboard", "Electronics", 1500m, 0),
    new("SKU004", "Rice", "Grocery", 60m, 3),
    new("SKU005", "Coffee", "Grocery", 250m, 15),
    
    new("SKU006", "Monitor", "Electronics", 8500m, 7),
    new("SKU007", "Bread", "Grocery", 70m, 12),
    
    new("SKU008", "", "Miscellaneous", -50m, 3),
    
    new("SKU005", "Invalid Product", "Miscellaneous", -10m, -2),
    new("SKU001", "Duplicate Laptop", "Electronics", 48000m, 5)
};

foreach (var product in inputProducts)
{
    var errors = ProductValidator.Validate(product, products);
    if (errors.Count == 0)
    {
        products.Add(product);
        Console.WriteLine($"Added: {product.Name}");
    }
    else
    {
        Console.WriteLine($"Rejected: {(string.IsNullOrEmpty(product.Name) ? "(no name)" : product.Name)}");
        foreach (var e in errors) Console.WriteLine($"  - {e}");
    }
}

var productLookup = products.ToDictionary(p => p.Sku);
string searchSku = "SKU002";
Console.WriteLine($"\nDictionary search for {searchSku}:");
if (productLookup.TryGetValue(searchSku, out var found))
    Console.WriteLine($"Found: {found.Name}, Price: {found.Price:C}");
else Console.WriteLine("Not found.");

Console.WriteLine("\nLINQ search for Coffee:");
var coffee = products.FirstOrDefault(p => p.Name.Equals("Coffee", StringComparison.OrdinalIgnoreCase));
Console.WriteLine(coffee is not null ? $"Found: {coffee.Name}" : "Not found.");

var electronics = products.Where(p => p.Category == "Electronics").OrderBy(p => p.Name).ToList();
Console.WriteLine("\nElectronics Products:");
foreach (var p in electronics) Console.WriteLine($"{p.Name,-15} — {p.Price:C}");

var highValue = products.Where(p => p.Price >= 1000m).OrderByDescending(p => p.Price).Select(p => new {p.Sku, p.Name, p.Price}).ToList();
Console.WriteLine("\nHigh‑Value Products:");
foreach (var x in highValue) Console.WriteLine($"{x.Sku} | {x.Name,-15} | {x.Price,10:C}");

var outOfStock = products.Where(p => p.Stock == 0).Select(p => p.Name).ToList();
Console.WriteLine("\nOut of Stock:");
foreach (var n in outOfStock) Console.WriteLine(n);

var inStockOver10 = products.Where(p => p.Stock > 10).OrderBy(p => p.Name).ToList();
Console.WriteLine("\nProducts with Stock Greater Than 10:");
foreach (var p in inStockOver10) Console.WriteLine($"{p.Name} — Stock: {p.Stock}");

var grouped = products.GroupBy(p => p.Category).OrderBy(g => g.Key).ToList();
Console.WriteLine("\nProducts by Category:");
foreach (var g in grouped)
{
    Console.WriteLine($"{g.Key,-15} ({g.Count()})");
    foreach (var p in g) Console.WriteLine($"  - {p.Name}");
}

decimal totalValue = products.Sum(p => p.Price * p.Stock);
Console.WriteLine($"\nTotal Inventory Value: {totalValue:C}");

string jsonFile = "products.json";
FileService.SaveAsJson(products, jsonFile);
Console.WriteLine($"\nProducts saved to {jsonFile}");

var loaded = FileService.LoadFromJson(jsonFile);
Console.WriteLine($"Loaded {loaded.Count} products from {jsonFile}");

var missing = FileService.LoadFromJson("missing-products.json");
Console.WriteLine($"Loading missing file returned {missing.Count} products.");

string reportFile = "report.txt";
FileService.ExportTextReport(products, reportFile);
Console.WriteLine($"\nReport exported to {reportFile}");

if (File.Exists(reportFile))
{
    Console.WriteLine("\n--- Report Content ---");
    foreach (var line in File.ReadAllLines(reportFile)) Console.WriteLine(line);
}

Console.WriteLine("\n--- Exception Handling Demo ---");
try
{
    _ = decimal.Parse("abc");
}
catch (FormatException ex)
{
    Console.WriteLine($"Format error caught: {ex.Message}");
}
finally
{
    Console.WriteLine("Finally block executed.");
}

Console.WriteLine("\nActivity completed.");

public record Product(string Sku, string Name, string Category, decimal Price, int Stock);

public static class ProductValidator
{
    public static List<string> Validate(Product p, List<Product> existing)
    {
        var err = new List<string>();
        if (string.IsNullOrWhiteSpace(p.Sku)) err.Add("SKU is required.");
        if (string.IsNullOrWhiteSpace(p.Name)) err.Add("Name is required.");
        if (string.IsNullOrWhiteSpace(p.Category)) err.Add("Category is required.");
        if (p.Price < 0) err.Add("Price must be zero or higher.");
        if (p.Stock < 0) err.Add("Stock must be zero or higher.");
        if (!string.IsNullOrWhiteSpace(p.Sku) && existing.Any(x => x.Sku.Equals(p.Sku, StringComparison.OrdinalIgnoreCase)))
            err.Add("SKU already exists.");
        return err;
    }
}

public static class FileService
{
    private static readonly JsonSerializerOptions Opt = new() {WriteIndented = true};

    public static void SaveAsJson(List<Product> prods, string path)
    {
        try { File.WriteAllText(path, JsonSerializer.Serialize(prods, Opt)); }
        catch (Exception ex) { Console.WriteLine($"Error saving: {ex.Message}"); }
    }

    public static List<Product> LoadFromJson(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                Console.WriteLine("JSON file not found. Returning empty list.");
                return new();
            }
            var json = File.ReadAllText(path);
            return string.IsNullOrWhiteSpace(json) ? new() : JsonSerializer.Deserialize<List<Product>>(json) ?? new();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading: {ex.Message}");
            return new();
        }
    }

    public static void ExportTextReport(List<Product> prods, string path)
    {
        var lines = new List<string>
        {
            "Product Inventory Report",
            $"Generated: {DateTime.Now}",
            new string('-', 40)
        };
        foreach (var p in prods)
            lines.Add($"{p.Sku} | {p.Name,-15} | {p.Category,-12} | {p.Price,10:C} | Stock: {p.Stock,3}");
        lines.Add(new string('-', 40));
        lines.Add($"Total products: {prods.Count}");
        lines.Add($"Total inventory value: {prods.Sum(p => p.Price * p.Stock):C}");
        File.WriteAllLines(path, lines);
    }
}