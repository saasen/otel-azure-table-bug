using System.Net;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddOpenTelemetry()
    .UseAzureMonitor()
    .WithTracing(traceBuilder =>
        traceBuilder
            .AddHttpClientInstrumentation(c =>
            {
                c.EnrichWithHttpResponseMessage = (ac, res) =>
                {
                    var statusCode = (int)res.StatusCode;
                    if (statusCode is >= (int)HttpStatusCode.BadRequest and < (int)HttpStatusCode.InternalServerError)
                    {
                        var body = res.Content.ReadAsStringAsync().Result;
                        if (!string.IsNullOrWhiteSpace(body))
                        {
                            ac.SetTag("http.response.body", body);
                        }
                    }
                };
            })
            .AddAspNetCoreInstrumentation(config =>
            {
                config.Filter = httpContext =>
                {
                    var unwantedSegments = new[] { "/v1/healthchecks", "/metrics" };
                    foreach (var unwantedSegment in unwantedSegments)
                    {
                        if (httpContext.Request.Path.Value.StartsWith(unwantedSegment))
                        {
                            return false;
                        }
                    }

                    return true;
                };
            })
    );

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

app.Run();