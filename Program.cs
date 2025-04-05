var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Global exception handling middleware
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = 500;
        await context.Response.WriteAsJsonAsync(new { error = "Internal server error." });
    }
});

// Token validation middleware
app.Use(async (context, next) =>
{
    var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

    if (string.IsNullOrEmpty(token) || !ValidateToken(token))
    {
        context.Response.StatusCode = 401;
        await context.Response.WriteAsJsonAsync(new { error = "Unauthorized" });
        return;
    }

    await next();
});

// Logging middleware
app.Use(async (context, next) =>
{
    // Log the HTTP request
    Console.WriteLine($"Request: {context.Request.Method} {context.Request.Path}");

    // Proceed to the next middleware
    await next();

    // Log the HTTP response
    Console.WriteLine($"Response: {context.Response.StatusCode}");
});

// In-memory user storage
var users = new List<User>
{
    new User { Id = 1, Name = "John Doe", Email = "john.doe@example.com" },
    new User { Id = 2, Name = "Jane Smith", Email = "jane.smith@example.com" }
};

// GET: Retrieve all users
app.MapGet("/users", () => Results.Ok(users.AsReadOnly()))
   .WithName("GetAllUsers");

// GET: Retrieve a specific user by ID
app.MapGet("/users/{id:int}", (int id) =>
{
    var user = users.Find(u => u.Id == id); // Optimized with Find
    if (user is null)
    {
        return Results.NotFound($"User with ID {id} was not found.");
    }
    return Results.Ok(user);
}).WithName("GetUserById");

// POST: Add a new user
app.MapPost("/users", (User newUser) =>
{
    var validationResult = ValidateUser(newUser);
    if (!string.IsNullOrEmpty(validationResult))
    {
        return Results.BadRequest(validationResult);
    }

    newUser.Id = users.Any() ? users.Max(u => u.Id) + 1 : 1;
    users.Add(newUser);
    return Results.Created($"/users/{newUser.Id}", newUser);
}).WithName("CreateUser");

// PUT: Update an existing user's details
app.MapPut("/users/{id:int}", (int id, User updatedUser) =>
{
    var user = users.Find(u => u.Id == id); // Optimized with Find
    if (user is null)
    {
        return Results.NotFound($"User with ID {id} was not found.");
    }

    var validationResult = ValidateUser(updatedUser);
    if (!string.IsNullOrEmpty(validationResult))
    {
        return Results.BadRequest(validationResult);
    }

    user.Name = updatedUser.Name;
    user.Email = updatedUser.Email;
    return Results.Ok(user);
}).WithName("UpdateUser");

// DELETE: Remove a user by ID
app.MapDelete("/users/{id:int}", (int id) =>
{
    var user = users.Find(u => u.Id == id); // Optimized with Find
    if (user is null)
    {
        return Results.NotFound($"User with ID {id} was not found.");
    }

    users.Remove(user);
    return Results.NoContent();
}).WithName("DeleteUser");

app.Run();

bool IsValidEmail(string email)
{
    return !string.IsNullOrWhiteSpace(email) && 
           email.Contains("@") && 
           email.Contains(".");
}

// Helper method to validate user data
string ValidateUser(User user)
{
    if (string.IsNullOrWhiteSpace(user.Name))
    {
        return "Name is required.";
    }

    if (!IsValidEmail(user.Email))
    {
        return "Invalid email format.";
    }

    return string.Empty;
}

// Helper method to validate token
bool ValidateToken(string token)
{
    // Replace this with your actual token validation logic
    // Example: Allow multiple valid tokens
    var validTokens = new List<string> { "valid-token", "another-valid-token" };
    return validTokens.Contains(token);
}

class User
{
    public int Id { get; set; }
    required public string Name { get; set; }
    required public string Email { get; set; }
}