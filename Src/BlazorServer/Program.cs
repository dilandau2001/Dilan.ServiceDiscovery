using Dilan.GrpcServiceDiscovery.BlazorServer.Data;
using Dilan.GrpcServiceDiscovery.Grpc;

// Read configuration from appSettings.
var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var options = new ServiceConfigurationOptions();
config
    .GetSection(nameof(ServiceConfigurationOptions))
    .Bind(options);

var builder = WebApplication.CreateBuilder(args);

// Configure logger
//builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<ServiceDiscoveryServer>();
builder.Services.AddSingleton(options);
builder.Services.AddSingleton<ServerManagerLogic>();
builder.Services.AddSingleton<ServiceDiscoveryService>();
builder.Services.AddSingleton<MulticastClient>();

// Build container
var app = builder.Build();

// Start the server
var server = app.Services.GetRequiredService<ServiceDiscoveryServer>();
server.Start();

// Configure the HTTP request pipeline.
Console.WriteLine("Environment is Developement = " + app.Environment.IsDevelopment());

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// All other configurations.
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");
app.Run();
