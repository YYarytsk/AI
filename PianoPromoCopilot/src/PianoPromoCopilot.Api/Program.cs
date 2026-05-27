var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var app = builder.Build();
app.UseSwagger(); app.UseSwaggerUI();
app.MapGet("/api/health",()=>Results.Ok(new{status="ok"}));
app.Run();
