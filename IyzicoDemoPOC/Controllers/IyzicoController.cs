using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using IyzicoDemoPOC.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace IyzicoDemoPOC.Controllers
{
    [ApiController]
    [Route("iyzico")]
    public class IyzicoController : ControllerBase
    {
        private readonly IyzicoSettings _settings;
        private readonly IHttpClientFactory _factory;

        public IyzicoController(IOptions<IyzicoSettings> settings, IHttpClientFactory factory)
        {
            _settings = settings.Value;
            _factory = factory;
        }

        [HttpPost("create-checkout-form")]
        public async Task<IActionResult> CreateCheckoutForm()
        {
            var httpClient = _factory.CreateClient();
            var uri = $"{_settings.BaseUrl}/payment/iyzipos/checkout/form/initialize";

            var request = new
            {
                locale = "tr",
                conversationId = "123456789",
                price = "100.00",
                paidPrice = "100.00",
                currency = "TRY",
                basketId = "B67832",
                paymentGroup = "PRODUCT",
                callbackUrl = _settings.CallbackUrl,
                buyer = new
                {
                    id = "BY789",
                    name = "Emirhan",
                    surname = "Dagdelen",
                    gsmNumber = "+905350000000",
                    email = "emir@example.com",
                    identityNumber = "11111111111",
                    registrationAddress = "Istanbul Kadikoy",
                    city = "Istanbul",
                    country = "Turkey",
                    zipCode = "34710"
                },
                shippingAddress = new
                {
                    contactName = "Emirhan Dagdelen",
                    city = "Istanbul",
                    country = "Turkey",
                    address = "Test mahallesi No:1",
                    zipCode = "34710"
                },
                billingAddress = new
                {
                    contactName = "Emirhan Dagdelen",
                    city = "Istanbul",
                    country = "Turkey",
                    address = "Fatura mahallesi No:2",
                    zipCode = "34710"
                },
                basketItems = new[]
                {
                    new {
                        id = "BI101",
                        name = "USB Kablo",
                        category1 = "Elektronik",
                        itemType = "PHYSICAL",
                        price = "100.00"
                    }
                }
            };

            var json = JsonSerializer.Serialize(request);
            var base64Data = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
            var hashStr = _settings.ApiKey + json + _settings.SecretKey;
            var hash = Convert.ToBase64String(SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(hashStr)));

            var message = new HttpRequestMessage(HttpMethod.Post, uri);
            message.Headers.Add("Accept", "application/json");
            message.Headers.Add("Authorization", $"IYZWS {_settings.ApiKey}:{hash}");

            message.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.SendAsync(message);
            var responseContent = await response.Content.ReadAsStringAsync();

            return Content(responseContent, "application/json");
        }

        [HttpPost("callback")]
        public IActionResult Callback()
        {
            // iyzico success/failed yönlendirme sonrası döner
            return Ok("✅ Callback received.");
        }
    }
}
