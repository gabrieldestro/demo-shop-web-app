using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Entities;
using Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Stripe;

namespace Infrastructure.Services
{
    public class PaymentService(IConfiguration config, ICartService cartService,
    IGenericRepository<DeliveryMethod> dmRepo,
    IGenericRepository<Core.Entities.Product> productRepo) : IPaymentService
    {
        public async Task<ShoppingCart?> CreateOrUpdatePaymentIntent(string cartId)
        {
            var cart = await cartService.GetCartAsync(cartId);

            if (cart == null) return null;

            var shippingPrice = 0m;

            if (cart.DeliveryMethodId.HasValue)
            {
                var deliveryMethod = await dmRepo.GetByIdAsync((int)cart.DeliveryMethodId);

                if (deliveryMethod == null) return null;

                shippingPrice = deliveryMethod.Price;
            }

            foreach (var item in cart.Items)
            {
                var productItem = await productRepo.GetByIdAsync(item.ProductId);

                if (productItem == null) return null;

                if (item.Price != productItem.Price)
                {
                    item.Price = productItem.Price;
                }
            }

            await handleExternalPaymentService(cart, shippingPrice);

            await cartService.SetCartAsync(cart);

            return cart;
        }

        private async Task handleExternalPaymentService(ShoppingCart cart, decimal shippingPrice)
        {
            StripeConfiguration.ApiKey = config["StripeSettings:SecretKey"];

            var service = new PaymentIntentService();

            PaymentIntent? intent = null;

            if (string.IsNullOrEmpty(cart.PaymentIntentId))
            {
                var options = new PaymentIntentCreateOptions
                {
                    Amount = (long)cart.Items.Sum(i => i.Quantity * (i.Price * 100)) + (long)shippingPrice * 100,
                    Currency = "usd",
                    PaymentMethodTypes = ["card"]
                };
                intent = await service.CreateAsync(options);
                cart.PaymentIntentId = intent.Id;
                cart.ClientSecret = intent.ClientSecret;
            }
            else
            {
                var options = new PaymentIntentUpdateOptions
                {
                    Amount = (long)cart.Items.Sum(i => i.Quantity * (i.Price * 100)) + (long)shippingPrice * 100,
                };
                intent = await service.UpdateAsync(cart.PaymentIntentId, options);
            }
        }
    }
}
