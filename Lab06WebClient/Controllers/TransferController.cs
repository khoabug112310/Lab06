using Lab06WebClient.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Lab06WebClient.Controllers
{
    public class TransferController : Controller
    {
        private readonly string baseUrl = "http://localhost:5062/api/transfer";
        private readonly string authUrl = "http://localhost:5062/api/auth";
        private readonly HttpClient httpClient;

        public TransferController(HttpClient httpClient) => this.httpClient = httpClient;

        private async Task<HttpResponseMessage> SendAuthorizedAsync(Func<string, HttpRequestMessage> requestFactory)
        {
            var token = HttpContext.Session.GetString("Token");
            if (string.IsNullOrEmpty(token)) throw new UnauthorizedAccessException();

            var request = requestFactory(token);
            var response = await httpClient.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                var refreshToken = HttpContext.Session.GetString("RefreshToken");
                if (!string.IsNullOrEmpty(refreshToken))
                {
                    var refreshPayload = new { RefreshToken = refreshToken };
                    var refreshResponse = await httpClient.PostAsJsonAsync($"{authUrl}/refresh-token", refreshPayload);

                    if (refreshResponse.IsSuccessStatusCode)
                    {
                        var refreshData = await refreshResponse.Content.ReadFromJsonAsync<RefreshTokenResponse>();
                        if (refreshData != null && !string.IsNullOrEmpty(refreshData.Token))
                        {
                            HttpContext.Session.SetString("Token", refreshData.Token);
                            HttpContext.Session.SetString("RefreshToken", refreshData.RefreshToken);

                            // Retry with new token
                            var retryRequest = requestFactory(refreshData.Token);
                            return await httpClient.SendAsync(retryRequest);
                        }
                    }
                }

                // If refresh failed or no refresh token, clear session
                HttpContext.Session.Clear();
                throw new UnauthorizedAccessException();
            }

            return response;
        }

        private static async Task<string> ExtractErrorMessageAsync(HttpResponseMessage response)
        {
            try
            {
                var content = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                if (doc.RootElement.TryGetProperty("message", out var msgProp))
                {
                    return msgProp.GetString() ?? content;
                }
                return content;
            }
            catch
            {
                return await response.Content.ReadAsStringAsync();
            }
        }

        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("Token") == null)
            {
                return RedirectToAction("Login", "Account");
            }
            ViewBag.Account = HttpContext.Session.GetString("Account");
            ViewBag.FullName = HttpContext.Session.GetString("FullName");
            ViewBag.Balance = HttpContext.Session.GetString("Balance");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Transfer(TransferViewModel model)
        {
            if (HttpContext.Session.GetString("Token") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Account = HttpContext.Session.GetString("Account");
                ViewBag.FullName = HttpContext.Session.GetString("FullName");
                ViewBag.Balance = HttpContext.Session.GetString("Balance");
                return View("Index", model);
            }

            try
            {
                var response = await SendAuthorizedAsync(token =>
                {
                    var req = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/request-otp");
                    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    req.Content = JsonContent.Create(model);
                    return req;
                });

                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.Error = await ExtractErrorMessageAsync(response);
                    ViewBag.Account = HttpContext.Session.GetString("Account");
                    ViewBag.FullName = HttpContext.Session.GetString("FullName");
                    ViewBag.Balance = HttpContext.Session.GetString("Balance");
                    return View("Index", model);
                }

                using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                var root = doc.RootElement;
                var transactionId = root.GetProperty("transactionId").GetString() ?? "";
                var maskedEmail = root.TryGetProperty("maskedEmail", out var mProp) ? mProp.GetString() ?? "" : "";
                var toFullName = root.TryGetProperty("toFullName", out var fnProp) ? fnProp.GetString() ?? "" : "";

                var confirmModel = new ConfirmOtpViewModel
                {
                    TransactionId = transactionId,
                    ToAccount = model.ToAccount,
                    ToFullName = toFullName,
                    Amount = model.Amount,
                    MaskedEmail = maskedEmail
                };

                return View("ConfirmOtp", confirmModel);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account");
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                ViewBag.Account = HttpContext.Session.GetString("Account");
                ViewBag.FullName = HttpContext.Session.GetString("FullName");
                ViewBag.Balance = HttpContext.Session.GetString("Balance");
                return View("Index", model);
            }
        }

        [HttpGet]
        public IActionResult ConfirmOtp()
        {
            if (HttpContext.Session.GetString("Token") == null)
            {
                return RedirectToAction("Login", "Account");
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmOtp(ConfirmOtpViewModel model)
        {
            if (HttpContext.Session.GetString("Token") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrWhiteSpace(model.Otp) || model.Otp.Trim().Length != 6)
            {
                ViewBag.Error = "Please enter a valid 6-digit OTP code.";
                return View("ConfirmOtp", model);
            }

            try
            {
                var payload = new
                {
                    TransactionId = model.TransactionId,
                    Otp = model.Otp.Trim()
                };

                var response = await SendAuthorizedAsync(token =>
                {
                    var req = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/confirm-otp");
                    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    req.Content = JsonContent.Create(payload);
                    return req;
                });

                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.Error = await ExtractErrorMessageAsync(response);
                    return View("ConfirmOtp", model);
                }

                if (decimal.TryParse(HttpContext.Session.GetString("Balance"), out var currentBalance))
                {
                    HttpContext.Session.SetString("Balance", (currentBalance - model.Amount).ToString());
                }

                TempData["Message"] = "Transfer completed successfully!";
                return RedirectToAction("History");
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account");
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View("ConfirmOtp", model);
            }
        }

        public async Task<IActionResult> History()
        {
            try
            {
                var response = await SendAuthorizedAsync(token =>
                {
                    var req = new HttpRequestMessage(HttpMethod.Get, baseUrl);
                    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    return req;
                });

                if (!response.IsSuccessStatusCode) return RedirectToAction("Login", "Account");
                var data = await response.Content.ReadFromJsonAsync<List<TransactionViewModel>>();
                return View(data ?? new List<TransactionViewModel>());
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account");
            }
        }
    }
}
