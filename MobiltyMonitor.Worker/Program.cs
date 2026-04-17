namespace MobiltyMonitor.Worker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);
            builder.Services.AddHostedService<MobilityMonitor.Worker.Worker>();

            var host = builder.Build();
            host.Run();
        }
    }
}
