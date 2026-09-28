using IoT.API.Configuration;
using IoT.API.Middleware;
using IoT.Application.Common;
using IoT.Application.Devices;
using IoT.Application.Devices.Events;
using IoT.Application.Devices.Services;
using IoT.Application.Identity;
using IoT.Infrastructure.Background;
using IoT.Infrastructure.Database;
using IoT.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

// Configuration bootstrap. Both calls must happen BEFORE CreateBuilder, because that is
// where AddEnvironmentVariables() reads the environment. See EnvironmentConfiguration.
EnvironmentConfiguration.LoadDotEnvFile();
EnvironmentConfiguration.ApplyAliases();

var builder = WebApplication.CreateBuilder(args);

// Infrastructure values come from the environment only (repo-root .env locally,
// compose environment: block in Docker) and are never present in appsettings.json.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "No connection string. Set CONNECTION_STRING (container) or " +
        "ConnectionStrings__DefaultConnection (local) in the repo-root .env file.");

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "No JWT signing key. Set JWT_KEY in the repo-root .env file.");

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Stamp a "Z" on every timestamp. Without this, values read back from SQL carry
        // DateTimeKind.Unspecified, serialize without a suffix, and the browser parses
        // them as LOCAL time -- every event displayed hours off. See UtcDateTimeConverter.
        options.JsonSerializerOptions.Converters.Add(new UtcDateTimeConverter());
        options.JsonSerializerOptions.Converters.Add(new NullableUtcDateTimeConverter());
    });

builder.Services.AddEndpointsApiExplorer();

// Exception handlers run as a chain in registration order: the domain handler claims the
// expected EntrioException types, and the global one catches everything else. Registered
// before AddControllers' pipeline via UseExceptionHandler below.
builder.Services.AddExceptionHandler<EntrioExceptionHandler>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Allowed origins are application behaviour, not infrastructure, so they live in
// appsettings.json: 8080 is the Vite dev server, 4300 is the nginx container.
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials());
});

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "IoT Garage API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter: Bearer {your JWT token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(CreateDeviceEventCommand).Assembly));

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(connectionString);
});

// IMPORTANT: expose it as IAppDbContext
builder.Services.AddScoped<IAppDbContext>(provider =>
    provider.GetRequiredService<AppDbContext>());

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<IDeviceAuthenticator, DeviceAuthenticator>();
builder.Services.AddScoped<AutoCloseService>();

builder.Services.AddHostedService<ScheduleWorker>();

var app = builder.Build();

// Migrate and seed before listening, so an open port also means the database is ready.
await DatabaseStartup.MigrateAsync(app);
await DatabaseSeeder.SeedAsync(app);

// Must come first in the pipeline so it can catch anything thrown further down.
app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Only meaningful when the https launch profile is used. The container listens on
    // plain HTTP only, and the ESP32 speaks plain HTTP, so redirecting there would
    // break the device without ever having a port to redirect to.
    app.UseHttpsRedirection();
}

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
