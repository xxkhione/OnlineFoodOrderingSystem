using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

// eureka
using Steeltoe.Discovery.Client;
using Steeltoe.Discovery.Eureka;

using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddNewtonsoftJson(options =>
    options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(o =>
{
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured."))),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
        ValidateIssuerSigningKey = true
    };
});
builder.Services.AddAuthorization();

// Eureka / Steeltoe Service Discovery (currently disabled)
// To re-enable: uncomment the line below.
// NOTE: Steeltoe 3.2.8 officially targets .NET 6/7/8. May still work on .NET 10 via .NET Standard
// compatibility, but if you hit errors upgrade to Steeltoe 4.x: https://docs.steeltoe.io
//builder.Services.AddDiscoveryClient(builder.Configuration);

builder.Services.AddSingleton<IOrderNotificationProducer, OrderNotificationProducer>();
builder.Services.AddHostedService<OrderNotificationConsumer>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();            
    app.MapScalarApiReference(); 
}

app.UseCors(options => options.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/security/getMessage", () => "Hello World!").RequireAuthorization();

// Required if Eureka is enabled (Steeltoe 3.x only — obsolete/removed in Steeltoe 4.x)
//app.UseDiscoveryClient();

app.Run();