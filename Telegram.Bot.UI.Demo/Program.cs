using Localization;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Extensions.Logging;
using Telegram.Bot.UI;
using Telegram.Bot.UI.BotWorker;
using Telegram.Bot.UI.Demo.Database;
using Telegram.Bot.UI.Demo.TelegramBot;
using Telegram.Bot.UI.Demo.ViewModels;
using Telegram.Bot.UI.Loader;


// Load config
var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

string botToken = config["BotToken"] ?? throw new InvalidOperationException("BotToken not found in appsettings.json");
string? imgbbToken = config["ImgbbToken"];


// Logging
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .MinimumLevel.Debug()
    .CreateLogger();


// Database
Log.Information("Init Database...");
var databaseFactory = new DatabaseFactory();
using (var context = databaseFactory.Context()) {
    context.EnsureCreated();
}


// Localization
Log.Information("Init Localization...");
var localizationPack = LocalizationPack.FromLPack(new FileInfo(Path.Combine("Resources", "Lang.lpack")));


// Load Pages
Log.Information("Loading pages...");
var pagesPath = Path.Combine("Resources", "Pages");
// Pass assembly containing ViewModels for vmodel resolution
var vmodelAssembly = typeof(BotUser).Assembly;
var pageManager = new PageManager(pagesPath, vmodelAssembly);
pageManager.LoadAll();
Log.Information($"Loaded {pageManager.pageCount} pages: {string.Join(", ", pageManager.GetPageIds())}");

// Configure Services
Log.Information("Configuring services...");
var resourceLoader = new ResourceLoader("Resources");
PhotoEditorViewModel.Configure(imgbbToken, resourceLoader);

// Start Bot
Log.Information("Start bot...");
var loggerFactory = new SerilogLoggerFactory(Log.Logger);
var bot = new BotWorkerPulling<BotUser>((worker, chatId, client, token) => {
    return new BotUser(databaseFactory, pageManager, worker, chatId, client, token);
}) {
    botToken = botToken,
    resourceLoader = resourceLoader,
    localizationPack = localizationPack,
    logger = loggerFactory.CreateLogger("BotWorker")
};

await bot.StartAsync();


// Wait for exit
Console.ReadKey();

Log.Information("Stop bot...");
await bot.StopAsync();

Log.Information("Done");
