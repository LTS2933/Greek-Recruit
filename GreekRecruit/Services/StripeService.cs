using Stripe;
using Stripe.Checkout;

namespace GreekRecruit.Services;

public class StripeService
{
    private readonly IConfiguration _config;

    public StripeService(IConfiguration config)
    {
        _config = config;
        StripeConfiguration.ApiKey = _config["Stripe:SecretKey"];
    }

    public async Task<Customer> CreateCustomerAsync(string email, string name)
    {
        var options = new CustomerCreateOptions
        {
            Email = email,
            Name = name,
        };

        var service = new CustomerService();
        return await service.CreateAsync(options);
    }

    public async Task<string> CreateSubscriptionAsync(string orgName, string email, string paymentMethodId)
    {
        var customerOptions = new CustomerCreateOptions
        {
            Email = email,
            Name = orgName,
            PaymentMethod = paymentMethodId,
            InvoiceSettings = new CustomerInvoiceSettingsOptions
            {
                DefaultPaymentMethod = paymentMethodId
            }
        };

        var customerService = new CustomerService();
        var customer = await customerService.CreateAsync(customerOptions);

        var subscriptionOptions = new SubscriptionCreateOptions
        {
            Customer = customer.Id,
            Items = new List<SubscriptionItemOptions>
        {
            new SubscriptionItemOptions
            {
                Price = _config["Stripe:PriceId"]
            }
        },
            Expand = new List<string> { "latest_invoice.payment_intent" }
        };

        var subscriptionService = new SubscriptionService();
        var subscription = await subscriptionService.CreateAsync(subscriptionOptions);

        return subscription.Id;
    }


    public async Task<PaymentMethod> AttachPaymentMethodAsync(string paymentMethodId, string customerId)
    {
        var service = new PaymentMethodService();
        return await service.AttachAsync(paymentMethodId, new PaymentMethodAttachOptions
        {
            Customer = customerId
        });
    }

    public async Task SetDefaultPaymentMethodAsync(string customerId, string paymentMethodId)
    {
        var service = new CustomerService();
        await service.UpdateAsync(customerId, new CustomerUpdateOptions
        {
            InvoiceSettings = new CustomerInvoiceSettingsOptions
            {
                DefaultPaymentMethod = paymentMethodId
            }
        });
    }


    public async Task CancelSubscriptionAsync(string subscriptionId)
    {
        var service = new SubscriptionService();

        // Cancel at period end
        await service.UpdateAsync(subscriptionId, new SubscriptionUpdateOptions
        {
            CancelAtPeriodEnd = true
        });

    }

}
