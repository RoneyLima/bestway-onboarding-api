

using Carter;
using Festpay.Onboarding.Api.Middlewares;
using Festpay.Onboarding.Application.Modules;
using Festpay.Onboarding.Infra;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        name: "AllowAllOrigins",
        configurePolicy: policy =>
        {
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        }
    );
});

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(1);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddDistributedMemoryCache();
builder.Services.AddProblemDetails();
builder.Services.AddCarterModules(typeof(AppModules).Assembly);

AppModules.AddApplication(builder.Services);
builder.Services.AddDatabase();
builder.Services.AddSwagger(builder.Configuration);

var app = builder.Build();

app.UseCors("AllowAllOrigins");
app.UseSession();

app.UseSwagger();
app.UseSwaggerUI();

app.MapCarter();
app.MapControllers();

app.MapOpenApi();

app.UseHttpsRedirection();

app.UseMiddleware<ExceptionMiddleware>();

await app.RunAsync();
