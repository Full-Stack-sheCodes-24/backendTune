namespace MoodzApi
{
    public class Program
    {
        public static void Main(string[] args) {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    // Check the environment and apply URL binding conditionally
                    var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

                    if (environment == "Production")
                    {
                        // In production, bind to the port provided by Heroku
                        var port = Environment.GetEnvironmentVariable("PORT");
                        if (!string.IsNullOrEmpty(port))
                        {
                            webBuilder.UseUrls($"http://0.0.0.0:{port}");
                        }
                    }
                })
                .ConfigureAppConfiguration((context, config) =>
                {
                    var env = context.HostingEnvironment;

                    if (env.IsDevelopment())
                    {
                        config.AddUserSecrets<Program>();
                    }
                });
    }
}