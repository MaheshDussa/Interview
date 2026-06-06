using WebApplication1.ServiceCollections;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddDatabaseContext(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddControllers();
builder.Services.AddSwaggerWithAuth();
// If Azure AD is configured (ClientId present), register Azure AD auth, otherwise fall back to local JWT
var azureClientId = builder.Configuration.GetSection("AzureAd").GetValue<string>("ClientId");
if (!string.IsNullOrWhiteSpace(azureClientId))
{
    builder.Services.AddAzureAdAuthentication(builder.Configuration);
}
else
{
    builder.Services.AddJwtAuthentication(builder.Configuration);
}
builder.Services.AddCorsPolicy();

var app = builder.Build();

// Configure the HTTP request pipeline
app.ConfigurePipeline();

app.Run();
