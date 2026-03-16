using Locacao.PaymentService.Infrastructure.Adapters;
using Locacao.PaymentService.Infrastructure.Repositories;
using System.Text.Json.Serialization;

namespace Locacao.PaymentService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers()
                .AddJsonOptions(o =>
                    o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddSingleton<IPaymentRepository, InMemoryPaymentRepository>();
            builder.Services.AddSingleton<IPaymentGatewayAdapter, FakePaymentGatewayAdapter>();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseAuthorization();
            app.MapControllers();
            app.Run();
        }
    }
}
