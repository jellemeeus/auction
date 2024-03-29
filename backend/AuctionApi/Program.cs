using AuctionApi.Models;
using AuctionApi.Services;

// https://learn.microsoft.com/en-us/aspnet/core/security/cors?view=aspnetcore-8.0&preserve-view=true
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
//
builder.Services.Configure<AuctionDatabaseSettings>(
    builder.Configuration.GetSection("AuctionDatabase"));
builder.Services.AddSingleton<RoomsService>();

builder.Services.Configure<WarcraftDatabaseSettings>(
    builder.Configuration.GetSection("WarcraftDatabase"));

builder.Services.Configure<BlizzardAPISecrets>(
    builder.Configuration);
builder.Services.AddSingleton<BlizzardAPISecrets>();

builder.Services.AddSingleton<WarcraftService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy  =>
                      {
                          policy.WithOrigins("http://localhost:9000").AllowAnyMethod().AllowAnyHeader();
                      });
});

builder.Services.AddControllers();

// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-6.0
// Limit Cors to endpoints. See endpoint routing
// https://stackoverflow.com/questions/57530680/enable-cors-for-any-port-on-localhost


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors(MyAllowSpecificOrigins);

//app.UseAuthorization(); // TODO: look into adding this

app.MapControllers();


//
////string clientId = "MY-CLIENT-ID-GOES-HERE";
////string clientSecret = "MY-CLIENT-SECRET-GOES-HERE";
//
//var warcraftClient = new WarcraftClient(clientId, clientSecret, Region.Europe, Locale.en_GB);
//
//// Retrieve the character profile for Drinian of realm Norgannon.
//RequestResult<ArgentPonyWarcraftClient.Item> result =
//    await warcraftClient.GetItemAsync(19019, "static-eu");
//
//// If we got it, display the level.
//if (result.Success)
//{
//    ArgentPonyWarcraftClient.Item item = result.Value;
//    Console.WriteLine($"Level for {item.Name}: {item.Level}");
//}
//else {
//    Console.WriteLine($"Level for {result}");
//}

app.Run();