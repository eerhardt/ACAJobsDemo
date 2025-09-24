using ACAJobsDemo.ProcessorJob;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddAzureQueue("myqueue");

// Register the background service
builder.Services.AddHostedService<QueueProcessorService>();

using var host = builder.Build();

// Run the host (which will start the background service)
await host.RunAsync();
