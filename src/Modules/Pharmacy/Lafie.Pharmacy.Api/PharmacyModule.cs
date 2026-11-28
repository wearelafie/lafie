using Lafie.Pharmacy.Application.Catalog;
using Lafie.Pharmacy.Application.Dispensing;
using Lafie.Pharmacy.Application.Purchasing;
using Lafie.Pharmacy.Application.Stock;
using Lafie.Pharmacy.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Lafie.Pharmacy.Api;

/// <summary>Point d'entrée DI du module Pharmacie.</summary>
public static class PharmacyModule
{
    public static IServiceCollection AddPharmacyModule(this IServiceCollection services)
    {
        services.AddScoped<IMedicationRepository, MedicationRepository>();
        services.AddScoped<IStockRepository, StockRepository>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<IPurchaseOrderRepository, PurchaseOrderRepository>();
        services.AddScoped<IPrescriptionRepository, PrescriptionRepository>();
        return services;
    }
}
