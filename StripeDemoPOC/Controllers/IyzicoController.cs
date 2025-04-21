using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using StripeDemoPOC.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace StripeDemoPOC.Controllers
{
    [ApiController]
    [Route("paypal")]
    public class PayPalController : ControllerBase
    {
        private readonly PayPalSettings _settings;
        private readonly IHttpClientFactory _httpClientFactory;

        public PayPalController(IOptions<PayPalSettings> settings, IHttpClientFactory httpClientFactory)
        {
            _settings = settings.Value;
            _httpClientFactory = httpClientFactory;
        }

        [HttpPost("create-order")]
        public async Task<IActionResult> CreateOrder()
        {
            var client = _httpClientFactory.CreateClient();
            var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_settings.ClientId}:{_settings.ClientSecret}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", auth);

            // 1. Access Token Al
            var tokenResponse = await client.PostAsync($"{_settings.BaseUrl}/v1/oauth2/token",
                new StringContent("grant_type=client_credentials", Encoding.UTF8, "application/x-www-form-urlencoded"));

            var tokenResult = JsonSerializer.Deserialize<JsonElement>(await tokenResponse.Content.ReadAsStringAsync());
            var accessToken = tokenResult.GetProperty("access_token").GetString();

            // 2. Yeni Order Oluştur
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var body = new
            {
                intent = "CAPTURE",
                purchase_units = new[]
                {
                    new {
                        amount = new {
                            currency_code = "USD",
                            value = "10.00"
                        }
                    }
                },
                application_context = new {
                    return_url = _settings.SuccessUrl,
                    cancel_url = _settings.CancelUrl
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{_settings.BaseUrl}/v2/checkout/orders", content);
            var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

            var approveLink = json.RootElement
                .GetProperty("links")
                .EnumerateArray()
                .First(x => x.GetProperty("rel").GetString() == "approve")
                .GetProperty("href").GetString();

            return Ok(new { redirect_url = approveLink });
        }

        [HttpGet("success")]
        public IActionResult Success() => Ok("✅ Payment successful!");

        [HttpGet("cancel")]
        public IActionResult Cancel() => Ok("❌ Payment cancelled.");
    }
}
