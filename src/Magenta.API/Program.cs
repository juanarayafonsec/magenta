using Magenta.API.Extensions;
using Magenta.Infrastructure.Data;
using Magenta.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddServices();

// Add infrastructure services
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Magenta API v1");
        c.RoutePrefix = "swagger"; // Set Swagger UI at /swagger
    });
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.EnsureCreated();
}

app.Run();
