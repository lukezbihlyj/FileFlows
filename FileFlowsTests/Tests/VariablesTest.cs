using System.Text.Json;

namespace FileFlowsTests.Tests;

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
    
    
    /// <summary>
    /// Tests dictionary variables with nested keys and verifies proper replacement
    /// </summary>
    [TestMethod]
    public void Variables_Dictionary()
    {
        var variables = new Dictionary<string, object>();

        // Create a dictionary with nested keys
        var nestedDict = new Dictionary<string, object>
        {
            { "User", new Dictionary<string, object>
                {
                    { "Name", "Alice" },
                    { "Details", new Dictionary<string, object>
                        {
                            { "Age", 30 },
                            { "Country", "Canada" }
                        }
                    }
                }
            }
        };
        variables["user"] = nestedDict;

        // Test nested dictionary values
        Assert.AreEqual($"User Name: Alice", VariablesHelper.ReplaceVariables("User Name: {user.User.Name}", variables, stripMissing: true));
        Assert.AreEqual($"User Age: 30", VariablesHelper.ReplaceVariables("User Age: {user.User.Details.Age}", variables, stripMissing: true));
        Assert.AreEqual($"User Country: Canada", VariablesHelper.ReplaceVariables("User Country: {user.User.Details.Country}", variables, stripMissing: true));

        // Test if non-existent keys are handled properly with stripMissing
        Assert.AreEqual($"No Age Info:", VariablesHelper.ReplaceVariables("No Age Info: {user.User.Details.NonExistentKey}", variables, stripMissing: true));
        Assert.AreEqual($"Default:", VariablesHelper.ReplaceVariables("Default: {user.User.Details.NonExistentKey|size}", variables, stripMissing: true));
    }
    
    
    /// <summary>
    /// Tests dictionary variables with nested keys and verifies proper replacement
    /// </summary>
    [TestMethod]
    public void Variables_Dictionary_2()
    {
        var variables = new Dictionary<string, object>();

        // Create a nested dictionary
        var nestedDict = new Dictionary<string, object>
        {
            { "User", new Dictionary<string, object>
                {
                    { "Name", "Alice" },
                    { "Details", new Dictionary<string, object>
                        {
                            { "Age", 30 },
                            { "Country", "Canada" }
                        }
                    }
                }
            }
        };
        variables["user"] = nestedDict;

        var result = VariablesHelper.ReplaceVariables("Default: {user.User.Details.NonExistentKey|size}", variables, stripMissing: true);
        Assert.AreEqual("Default:", result, $"Expected 'Default:', but got '{result}'.");

        // Test nested dictionary values
        result = VariablesHelper.ReplaceVariables("User Name: {user.User.Name}", variables, stripMissing: true);
        Assert.AreEqual("User Name: Alice", result, $"Expected 'User Name: Alice', but got '{result}'.");

        result = VariablesHelper.ReplaceVariables("User Age: {user.User.Details.Age}", variables, stripMissing: true);
        Assert.AreEqual("User Age: 30", result, $"Expected 'User Age: 30', but got '{result}'.");

        result = VariablesHelper.ReplaceVariables("User Country: {user.User.Details.Country}", variables, stripMissing: true);
        Assert.AreEqual("User Country: Canada", result, $"Expected 'User Country: Canada', but got '{result}'.");

        // Test handling of non-existent keys with stripMissing
        result = VariablesHelper.ReplaceVariables("No Age Info: {user.User.Details.NonExistentKey}", variables, stripMissing: true);
        Assert.AreEqual("No Age Info:", result, $"Expected 'No Age Info:', but got '{result}'.");
    }
    
    
    /// <summary>
    /// Tests that empty brackets and parentheses are removed when the variable is missing or empty.
    /// </summary>
    [TestMethod]
    public void Variables_RemoveEmptyBracketsAndParentheses()
    {
        var variables = new Dictionary<string, object>();
        variables["existingVariable"] = "Alice";

        // Case 1: Brackets with missing variable
        string input1 = "text [{missingVariable}]";
        string expected1 = "text";
        string result1 = VariablesHelper.ReplaceVariables(input1, variables, stripMissing: true);
        Assert.AreEqual(expected1, result1, $"Expected '{expected1}', but got '{result1}'.");

        // Case 2: Parentheses with missing variable
        string input2 = "text ({missingVariable})";
        string expected2 = "text";
        string result2 = VariablesHelper.ReplaceVariables(input2, variables, stripMissing: true);
        Assert.AreEqual(expected2, result2, $"Expected '{expected2}', but got '{result2}'.");

        // Case 3: Brackets with missing variable and additional text
        string input3 = "text [{missingVariable}] end";
        string expected3 = "text end";
        string result3 = VariablesHelper.ReplaceVariables(input3, variables, stripMissing: true);
        Assert.AreEqual(expected3, result3, $"Expected '{expected3}', but got '{result3}'.");

        // Case 4: Parentheses with missing variable and additional text
        string input4 = "text ({missingVariable}) end";
        string expected4 = "text end";
        string result4 = VariablesHelper.ReplaceVariables(input4, variables, stripMissing: true);
        Assert.AreEqual(expected4, result4, $"Expected '{expected4}', but got '{result4}'.");

        // Case 5: Brackets with existing variable
        string input5 = "text [{existingVariable}]";
        string expected5 = "text [Alice]";
        string result5 = VariablesHelper.ReplaceVariables(input5, variables, stripMissing: true);
        Assert.AreEqual(expected5, result5, $"Expected '{expected5}', but got '{result5}'.");

        // Case 6: Parentheses with existing variable
        string input6 = "text ({existingVariable})";
        string expected6 = "text (Alice)";
        string result6 = VariablesHelper.ReplaceVariables(input6, variables, stripMissing: true);
        Assert.AreEqual(expected6, result6, $"Expected '{expected6}', but got '{result6}'.");

        // Case 7: Brackets with existing variable and additional text
        string input7 = "text [{existingVariable}] end";
        string expected7 = "text [Alice] end";
        string result7 = VariablesHelper.ReplaceVariables(input7, variables, stripMissing: true);
        Assert.AreEqual(expected7, result7, $"Expected '{expected7}', but got '{result7}'.");

        // Case 8: Parentheses with existing variable and additional text
        string input8 = "text ({existingVariable}) end";
        string expected8 = "text (Alice) end";
        string result8 = VariablesHelper.ReplaceVariables(input8, variables, stripMissing: true);
        Assert.AreEqual(expected8, result8, $"Expected '{expected8}', but got '{result8}'.");
    }
    
    [TestMethod]
    public void TestReplaceVariables_CleanSpecialCharacters_PathVariable()
    {
        var variables = new Dictionary<string, object>
        {
            { "pathVariable", "C:/Users/John/Files:Test" }
        };

        // Case 1: Clean special characters in a path variable
        string input = "Path: {pathVariable}";
        string expected = "Path: C-Users-John-Files - Test";
        string result = VariablesHelper.ReplaceVariables(input, variables, cleanSpecialCharacters: true);
        Assert.AreEqual(expected, result, $"Expected '{expected}', but got '{result}'.");
    }

    [TestMethod]
    public void TestReplaceVariables_FileName()
    {
        var variables = new Dictionary<string, object>
        {
            { "file.Name", @"C:\Users\John\Files - Test" }
        };
        string input = "Path: {file.Name}";
        string expected = @"Path: C:\Users\John\Files - Test";
        string result = VariablesHelper.ReplaceVariables(input, variables, cleanSpecialCharacters: true);
        Assert.AreEqual(expected, result, $"Expected '{expected}', but got '{result}'.");
    }
    [TestMethod]
    public void TestReplaceVariables_FolderName()
    {
        var variables = new Dictionary<string, object>
        {
            { "folder.Name", @"C:\Users\John\Files - Test" }
        };
        string input = "Path: {folder.Name}";
        string expected = @"Path: C:\Users\John\Files - Test";
        string result = VariablesHelper.ReplaceVariables(input, variables, cleanSpecialCharacters: true);
        Assert.AreEqual(expected, result, $"Expected '{expected}', but got '{result}'.");
    }

    [TestMethod]
    public void TestReplaceVariables_CleanSpecialCharacters_FileNameVariable()
    {
        var variables = new Dictionary<string, object>
        {
            { "file.Name", "file.txt" }
        };

        // Case 2: No cleaning needed for file name variable
        string input = "File: {file.Name}";
        string expected = "File: file.txt";
        string result = VariablesHelper.ReplaceVariables(input, variables, cleanSpecialCharacters: true);
        Assert.AreEqual(expected, result, $"Expected '{expected}', but got '{result}'.");
    }

    [TestMethod]
    public void TestReplaceVariables_CleanSpecialCharacters_FolderPathVariable()
    {
        var variables = new Dictionary<string, object>
        {
            { "folder.Path", "C:/Folder" }
        };

        // Case 3: No cleaning needed for folder path variable
        string input = "Folder: {folder.Path}";
        string expected = "Folder: C:/Folder";
        string result = VariablesHelper.ReplaceVariables(input, variables, cleanSpecialCharacters: true);
        Assert.AreEqual(expected, result, $"Expected '{expected}', but got '{result}'.");
    }

    [TestMethod]
    public void TestReplaceVariables_CleanSpecialCharacters_MissingVariable()
    {
        var variables = new Dictionary<string, object>();

        // Case 4: Clean special characters with missing variable (should replace with empty)
        string input = "Missing: {nonExistentVariable}";
        string expected = "Missing:";
        string result = VariablesHelper.ReplaceVariables(input, variables, stripMissing: true, cleanSpecialCharacters: true);
        Assert.AreEqual(expected, result, $"Expected '{expected}', but got '{result}'.");
    }

    [TestMethod]
    public void TestReplaceVariables_CleanSpecialCharacters_SpaceHandling()
    {
        var variables = new Dictionary<string, object>
        {
            { "pathVariable", "C:/Users/John/Files:Test" }
        };

        // Case 5: Clean special characters with space handling
        string input = "Path [{pathVariable}]";
        string expected = "Path [C-Users-John-Files - Test]";
        string result = VariablesHelper.ReplaceVariables(input, variables, stripMissing: true, cleanSpecialCharacters: true);
        Assert.AreEqual(expected, result, $"Expected '{expected}', but got '{result}'.");
    }

    [TestMethod]
    public void TestReplaceVariables_NoSpecialCharacterCleaning_PathVariable()
    {
        var variables = new Dictionary<string, object>
        {
            { "pathVariable", "C:/Users/John/Files:Test" }
        };

        // Case 6: No special characters cleaned
        string input = "Path: {pathVariable}";
        string expected = "Path: C:/Users/John/Files:Test";
        string result = VariablesHelper.ReplaceVariables(input, variables, cleanSpecialCharacters: false);
        Assert.AreEqual(expected, result, $"Expected '{expected}', but got '{result}'.");
    }

    [TestMethod]
    public void TestReplaceVariables_NoSpecialCharacterCleaning_FileNameVariable()
    {
        var variables = new Dictionary<string, object>
        {
            { "file.Name", "file.txt" }
        };

        // Case 7: No special characters cleaned for file name
        string input = "File: {file.Name}";
        string expected = "File: file.txt";
        string result = VariablesHelper.ReplaceVariables(input, variables, cleanSpecialCharacters: false);
        Assert.AreEqual(expected, result, $"Expected '{expected}', but got '{result}'.");
    }

    [TestMethod]
    public void TestReplaceVariables_NoSpecialCharacterCleaning_FolderPathVariable()
    {
        var variables = new Dictionary<string, object>
        {
            { "folder.Path", "C:/Folder" }
        };

        // Case 8: No special characters cleaned for folder path
        string input = "Folder: {folder.Path}";
        string expected = "Folder: C:/Folder";
        string result = VariablesHelper.ReplaceVariables(input, variables, cleanSpecialCharacters: false);
        Assert.AreEqual(expected, result, $"Expected '{expected}', but got '{result}'.");
    }

    [TestMethod]
    public void TestReplaceVariables_NoSpecialCharacterCleaning_MissingVariable()
    {
        var variables = new Dictionary<string, object>();

        // Case 9: Handling missing variable without special character cleaning
        string input = "Missing: {nonExistentVariable}";
        string expected = "Missing:";
        string result = VariablesHelper.ReplaceVariables(input, variables, stripMissing: true, cleanSpecialCharacters: false);
        Assert.AreEqual(expected, result, $"Expected '{expected}', but got '{result}'.");
    }

    [TestMethod]
    public void TestReplaceVariables_NoSpecialCharacterCleaning_SpaceHandling()
    {
        var variables = new Dictionary<string, object>
        {
            { "pathVariable", "C:/Users/John/Files:Test" }
        };

        // Case 10: No special characters cleaned with space handling
        string input = "Path [{pathVariable}]";
        string expected = "Path [C:/Users/John/Files:Test]";
        string result = VariablesHelper.ReplaceVariables(input, variables, stripMissing: true, cleanSpecialCharacters: false);
        Assert.AreEqual(expected, result, $"Expected '{expected}', but got '{result}'.");
    }
    
    [TestMethod]
    public void TestStripMissing()
    {
        var variables = new Dictionary<string, object>
        {
            { "pathVariable", "C:/Users/John/Files:Test" },
            { "null", null}
        };

        string input = "Path{missing}";
        string expected = "Path";
        string result = VariablesHelper.ReplaceVariables(input, variables, stripMissing: true);
        Assert.AreEqual(expected, result, $"Expected '{expected}', but got '{result}'.");
        
        string input2 = "Path{missing}";
        string expected2 = "Pathmissing";
        string result2 = VariablesHelper.ReplaceVariables(input2, variables, stripMissing: false);
        Assert.AreEqual(expected2, result2, $"Expected '{expected2}', but got '{result2}'.");
        
        string input3 = "Path{null}";
        string expected3 = "Path";
        string result3 = VariablesHelper.ReplaceVariables(input3, variables, stripMissing: true);
        Assert.AreEqual(expected3, result3, $"Expected '{expected3}', but got '{result3}'.");
        
        string input4 = "Path{null}";
        string expected4 = "Pathnull";
        string result4 = VariablesHelper.ReplaceVariables(input4, variables, stripMissing: false);
        Assert.AreEqual(expected4, result4, $"Expected '{expected4}', but got '{result4}'.");
    }
}