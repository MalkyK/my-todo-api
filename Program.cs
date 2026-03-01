


using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TodoApi;

var builder = WebApplication.CreateBuilder(args);

// 1. הגדרת מסד נתונים
var connectionString = builder.Configuration.GetConnectionString("ToDoDB");
builder.Services.AddDbContext<ToDoDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// 2. הגדרת JWT
// var key = Encoding.ASCII.GetBytes("YourSuperSecretKeyThatIsAtLeast32CharsLong!");

// במקום המחרוזת הקבועה, אנחנו מושכים מה-Configuration
var keyString = builder.Configuration["Jwt:Key"];
var key = Encoding.ASCII.GetBytes(keyString);
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

builder.Services.AddAuthorization();

// 3. הגדרת CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseAuthentication(); // חייב להופיע לפני Authorization
app.UseAuthorization();

// --- Endpoints של המערכת ---

app.MapGet("/", () => "Hello World!");

// הרשמה (Register)
app.MapPost("/register", async (ToDoDbContext db, User user) => {
    if (await db.Users.AnyAsync(u => u.Username == user.Username))
        return Results.BadRequest("User already exists");

    db.Users.Add(user);
    await db.SaveChangesAsync();
    return Results.Ok("User registered successfully");
});

// התחברות (Login) - מייצר טוקן
app.MapPost("/login", async (ToDoDbContext db, User loginUser) => {
    var user = await db.Users.FirstOrDefaultAsync(u => 
        u.Username == loginUser.Username && u.Password == loginUser.Password);

    if (user == null) return Results.Unauthorized();

    var tokenHandler = new JwtSecurityTokenHandler();
    var tokenDescriptor = new SecurityTokenDescriptor
    {


        Subject = new ClaimsIdentity(new[] { new Claim("id", user.Id.ToString()) }), 
        Expires = DateTime.UtcNow.AddDays(7),
        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
    };
    var token = tokenHandler.CreateToken(tokenDescriptor);
    return Results.Ok(new { token = tokenHandler.WriteToken(token) });
});


// שליפת משימות - הוספתי הגנה [Authorize]
app.MapGet("/items", async (ToDoDbContext db, System.Security.Claims.ClaimsPrincipal user) => {
    // שליפת ה-ID מתוך ה-Token
    var userId = int.Parse(user.FindFirst("id")?.Value ?? "0");
    // מחזיר רק משימות שה-UserId שלהן תואם למשתמש המחובר
    return await db.Items.Where(i => i.UserId == userId).ToListAsync();
}).RequireAuthorization();

//
app.MapPost("/items", async (ToDoDbContext db, Item newItem, System.Security.Claims.ClaimsPrincipal user) => {
    // מוצאים מי המשתמש ושומרים את ה-ID שלו בתוך המשימה החדשה
    var userId = int.Parse(user.FindFirst("id")?.Value ?? "0");
    newItem.UserId = userId; 

    db.Items.Add(newItem);
    await db.SaveChangesAsync();
    return Results.Created($"/items/{newItem.Id}", newItem);
}).RequireAuthorization();

// עדכון משימה - הוספתי הגנה
app.MapPut("/items/{id}", async (ToDoDbContext db, int id, Item inputItem) => {
    var item = await db.Items.FindAsync(id);
    if (item is null) return Results.NotFound();
    item.Name = inputItem.Name;
    item.IsComplete = inputItem.IsComplete;
    await db.SaveChangesAsync();
    return Results.NoContent();
}).RequireAuthorization();

// מחיקת משימה - הוספתי הגנה
app.MapDelete("/items/{id}", async (ToDoDbContext db, int id) => {
    if (await db.Items.FindAsync(id) is Item item) {
        db.Items.Remove(item);
        await db.SaveChangesAsync();
        return Results.Ok(item);
    }
    return Results.NotFound();
}).RequireAuthorization();

app.Run();


