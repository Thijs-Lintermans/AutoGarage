using AutoGarage.DAL;
using AutoGarage.DAL.Repositories;
using Microsoft.EntityFrameworkCore;
using Parkeerwachter.DAL;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Configure JSON serializer settings globally
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.MaxDepth = 64; // Optional: Set the maximum depth if needed
    });// Add services to the container.
var connectionString
    = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AutoGarageContext>(options =>
options.UseSqlServer(connectionString));

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();


using (var scope = app.Services.CreateScope())
{
    var myContext = scope.ServiceProvider.GetRequiredService<AutoGarageContext>();
    DBInitializer.Initialize(myContext);
}

app.Run();
