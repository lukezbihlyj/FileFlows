using System.Text.Json;

namespace FileFlowTests.Tests;

/// <summary>
/// Tests for variable replacements
/// </summary>
[TestClass]
public class VariablesTest
{
    /// <summary>
    /// Tests plex variables in folder names are not escaped
    /// </summary>
    [TestMethod]
    public void PlexTests()
    {
        var variables = new Dictionary<string, object>();
        Assert.AreEqual("ShowName (2020) {tmdb-123456}", VariablesHelper.ReplaceVariables("ShowName (2020) {tmdb-123456}", variables, stripMissing: true));
        Assert.AreEqual("ShowName (2020) {tvdb-123456}", VariablesHelper.ReplaceVariables("ShowName (2020) {tvdb-123456}", variables, stripMissing: true));
        Assert.AreEqual("ShowName (2020) {imdb-123456}", VariablesHelper.ReplaceVariables("ShowName (2020) {imdb-123456}", variables, stripMissing: true));
        
        Assert.AreEqual("ShowName (2020) {tmdb-123456} missing", VariablesHelper.ReplaceVariables("ShowName (2020) {tmdb-123456} {missing}", variables, stripMissing: false));
        Assert.AreEqual("ShowName (2020) {tvdb-123456} missing", VariablesHelper.ReplaceVariables("ShowName (2020) {tvdb-123456} {missing}", variables, stripMissing: false));
        Assert.AreEqual("ShowName (2020) {imdb-123456} missing", VariablesHelper.ReplaceVariables("ShowName (2020) {imdb-123456} {missing}", variables, stripMissing: false));
        variables.Add("tmdb-123456", "bobby");
        variables.Add("tvdb-123456", "drake");
        variables.Add("imdb-123456", "iceman");
        Assert.AreEqual("ShowName (2020) bobby", VariablesHelper.ReplaceVariables("ShowName (2020) {tmdb-123456}", variables, stripMissing: true));
        Assert.AreEqual("ShowName (2020) drake", VariablesHelper.ReplaceVariables("ShowName (2020) {tvdb-123456}", variables, stripMissing: true));
        Assert.AreEqual("ShowName (2020) iceman", VariablesHelper.ReplaceVariables("ShowName (2020) {imdb-123456}", variables, stripMissing: true));
    }

    /// <summary>
    /// Tests a variable with odd characters in it
    /// </summary>
    [TestMethod]
    public void NotValidVariable()
    {
        var variables = new Dictionary<string, object>();
        const string testString = "Test {thi$ is n0t a valid-variable{na-me}!}";
        Assert.AreEqual(testString, VariablesHelper.ReplaceVariables(testString, variables, stripMissing: true));
        variables.Add("thi$ is n0t a valid-variable{na-me}!", "odd{repl@ce}ment");
        Assert.AreEqual("Test odd{repl@ce}ment", VariablesHelper.ReplaceVariables(testString, variables, stripMissing: true));
    }
    

    /// <summary>
    /// Tests a variable with odd characters in it
    /// </summary>
    [TestMethod]
    public void Formatters()
    {
        var variables = new Dictionary<string, object>();
        const string name = "This is mixed Casing!";
        var date = new DateTime(2022, 10, 29, 11, 41, 32, 532);
        variables["value"] = date;
        Assert.AreEqual("Test 29/10/2022", VariablesHelper.ReplaceVariables("Test {value|dd/MM/yyyy}", variables, stripMissing: true));
        Assert.AreEqual("Test 29-10-2022", VariablesHelper.ReplaceVariables("Test {value|dd-MM-yyyy}", variables, stripMissing: true));
        Assert.AreEqual("Test 29-10-2022 11:41:32.532 AM", VariablesHelper.ReplaceVariables("Test {value|dd-MM-yyyy hh:mm:ss.fff tt}", variables, stripMissing: true));
        Assert.AreEqual($"Test {date.ToShortTimeString()}", VariablesHelper.ReplaceVariables("Test {value|time}", variables, stripMissing: true));
        variables["value"] = name;
        Assert.AreEqual("Test " + name.ToUpper(), VariablesHelper.ReplaceVariables("Test {value!}", variables, stripMissing: true));
        variables["value"] = 12;
        Assert.AreEqual("Test 0012", VariablesHelper.ReplaceVariables("Test {value|0000}", variables, stripMissing: true));
        variables["value"] = 645645654;
        Assert.AreEqual("Test 645.65 MB", VariablesHelper.ReplaceVariables("Test {value|size}", variables, stripMissing: true));
        variables["value"] = "this !:\\/ is #%^&*!~@?%$ not a safe name!..";
        Assert.AreEqual("Test this ! is #%^&!~@%$ not a safe name!", VariablesHelper.ReplaceVariables("Test {value|file}", variables, stripMissing: true));
    }

    [TestMethod]
    public void MissingVariables()
    {
        var variables = new Dictionary<string, object>();
        variables["movie.Name"] = "Batman";
        Assert.AreEqual("/movies/Batman .mkv", VariablesHelper.ReplaceVariables("/movies/{movie.Name} {movie.Year}.mkv", variables, stripMissing: true));
    }

    /// <summary>
    /// Tests a DateTime variable can have its properties access
    /// </summary>
    [TestMethod]
    public void Variables_Date()
    {
        var variables = new Dictionary<string, object>();
        var date = new DateTime(2020, 06, 29, 13, 45, 09);
        variables["img.DateTaken"] = date;
        Assert.AreEqual($"Date: {date:yyyy-MM-dd}", VariablesHelper.ReplaceVariables("Date: {img.DateTaken|yyyy-MM-dd}", variables, stripMissing: true));
        Assert.AreEqual($"Date: {date:yyyy-MM-dd}", VariablesHelper.ReplaceVariables("Date: {img.DateTaken:yyyy-MM-dd}", variables, stripMissing: true));
        
        Assert.AreEqual($"Year: {date.Year}", VariablesHelper.ReplaceVariables("Year: {img.DateTaken.Year}", variables, stripMissing: true));

        Assert.AreEqual($"Month: {date.Month}", VariablesHelper.ReplaceVariables("Month: {img.DateTaken.Month}", variables, stripMissing: true));

        Assert.AreEqual($"Day: {date.Day}", VariablesHelper.ReplaceVariables("Day: {img.DateTaken.Day}", variables, stripMissing: true));
    }
    
    
    /// <summary>
    /// Tests JsonElement variables can have their properties accessed
    /// </summary>
    [TestMethod]
    public void Variables_JsonElement()
    {
        var variables = new Dictionary<string, object>();

        // Create a JsonElement object
        var jsonString = @"{ ""Name"": ""John"", ""Age"": 30, ""Address"": { ""City"": ""New York"", ""Zip"": ""10001"" } }";
        var jsonDoc = JsonDocument.Parse(jsonString);
        var jsonElement = jsonDoc.RootElement;

        variables["person"] = jsonElement;

        // Access top-level properties
        Assert.AreEqual($"Name: John", VariablesHelper.ReplaceVariables("Name: {person.Name}", variables, stripMissing: true));
        Assert.AreEqual($"Age: 30", VariablesHelper.ReplaceVariables("Age: {person.Age}", variables, stripMissing: true));

        // Access nested properties
        Assert.AreEqual($"City: New York", VariablesHelper.ReplaceVariables("City: {person.Address.City}", variables, stripMissing: true));
        Assert.AreEqual($"Zip: 10001", VariablesHelper.ReplaceVariables("Zip: {person.Address.Zip}", variables, stripMissing: true));
    }

    /// <summary>
    /// Tests complex object properties and JsonElement within the variables
    /// </summary>
    [TestMethod]
    public void Variables_ComplexObjectsAndJsonElement()
    {
        var variables = new Dictionary<string, object>();

        // Complex object
        var complexObj = new
        {
            User = new { Name = "Alice", Details = new { Age = 25, Country = "USA" } }
        };
        variables["user"] = complexObj;

        // JsonElement object
        var jsonString = @"{ ""ProductName"": ""Laptop"", ""Price"": 1200.50, ""Specs"": { ""RAM"": ""16GB"", ""Storage"": ""512GB SSD"" } }";
        var jsonDoc = JsonDocument.Parse(jsonString);
        var productElement = jsonDoc.RootElement;
        variables["product"] = productElement;

        // Access complex object properties
        Assert.AreEqual($"User Name: Alice", VariablesHelper.ReplaceVariables("User Name: {user.User.Name}", variables, stripMissing: true));
        Assert.AreEqual($"User Age: 25", VariablesHelper.ReplaceVariables("User Age: {user.User.Details.Age}", variables, stripMissing: true));
        Assert.AreEqual($"Country: USA", VariablesHelper.ReplaceVariables("Country: {user.User.Details.Country}", variables, stripMissing: true));

        // Access JsonElement properties
        Assert.AreEqual($"Product: Laptop", VariablesHelper.ReplaceVariables("Product: {product.ProductName}", variables, stripMissing: true));
        Assert.AreEqual($"Price: 1200.50", VariablesHelper.ReplaceVariables("Price: {product.Price}", variables, stripMissing: true));

        // Access nested JsonElement properties
        Assert.AreEqual($"RAM: 16GB", VariablesHelper.ReplaceVariables("RAM: {product.Specs.RAM}", variables, stripMissing: true));
        Assert.AreEqual($"Storage: 512GB SSD", VariablesHelper.ReplaceVariables("Storage: {product.Specs.Storage}", variables, stripMissing: true));
    }
    
    
    /// <summary>
    /// Tests JsonElement variables with byte sizes and applies the size formatter
    /// </summary>
    [TestMethod]
    public void Variables_JsonElement_WithSizeFormatter()
    {
        var variables = new Dictionary<string, object>();

        // Create a JsonElement object with RAM and Storage in bytes
        var jsonString = @"{ ""Specs"": { ""RAM"": 17179869184, ""Storage"": 536870912000 } }"; // 16GB RAM, 500GB Storage
        var jsonDoc = JsonDocument.Parse(jsonString);
        var specsElement = jsonDoc.RootElement.GetProperty("Specs");
        variables["specs"] = specsElement;

        // Test SizeFormatter integration with the RAM and Storage
        Assert.AreEqual($"RAM: 17.18 GB", VariablesHelper.ReplaceVariables("RAM: {specs.RAM|size}", variables, stripMissing: true));
        Assert.AreEqual($"Storage: 536.87 GB", VariablesHelper.ReplaceVariables("Storage: {specs.Storage|size}", variables, stripMissing: true));
    }
}