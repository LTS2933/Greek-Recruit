using Stripe;

namespace GreekRecruit.Services;

public class StripeService
{
    private readonly IConfiguration _config;

    public StripeService(IConfiguration config)
    {
        _config = config;
        StripeConfiguration.ApiKey = _config["Stripe:SecretKey"];
    }

    public async Task<string> CreateSubscriptionAsync(
        string orgName,
        string email,
        string paymentMethodId,
        string billingName,
        string billingAddress,
        string billingPostalCode,
        string billingPhone)
    {
        var customerOptions = new CustomerCreateOptions
        {
            Email = email,
            Name = billingName,
            Address = new AddressOptions
            {
                Line1 = billingAddress,
                PostalCode = billingPostalCode
            },
            Phone = billingPhone,
            PaymentMethod = paymentMethodId,
            InvoiceSettings = new CustomerInvoiceSettingsOptions
            {
                DefaultPaymentMethod = paymentMethodId
            }
        };

        var customerService = new CustomerService();
        var customer = await customerService.CreateAsync(customerOptions);

        Console.WriteLine($"Created Customer: {customer.Id}");

        var subscriptionOptions = new SubscriptionCreateOptions
        {
            Customer = customer.Id,
            Items = new List<SubscriptionItemOptions>
            {
                new SubscriptionItemOptions { Price = _config["Stripe:PriceId"] }
            },
            PaymentBehavior = "error_if_incomplete",
            CollectionMethod = "charge_automatically"
        };

        var subscriptionService = new SubscriptionService();
        var subscription = await subscriptionService.CreateAsync(subscriptionOptions);

        Console.WriteLine($"Created Subscription: {subscription.Id}, Status: {subscription.Status}");

        if (subscription.Status != "active")
        {
            throw new Exception($"Subscription status not active: {subscription.Status}");
        }

        return subscription.Id;
    }

    public async Task CancelSubscriptionAsync(string subscriptionId)
    {
        var service = new SubscriptionService();

        await service.UpdateAsync(subscriptionId, new SubscriptionUpdateOptions
        {
            CancelAtPeriodEnd = true
        });
    }
}
