using WebApplication1.ServiceCollections;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddDatabaseContext(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddControllers();
builder.Services.AddSwaggerWithAuth();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddCorsPolicy();

var app = builder.Build();

// Configure the HTTP request pipeline
app.ConfigurePipeline();

app.Run();
