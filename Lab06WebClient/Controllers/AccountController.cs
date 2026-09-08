using System.Text;
using System.Text.Json;
using Lab06WebClient.Models;
using Microsoft.AspNetCore.Mvc;

namespace Lab06WebClient.Controllers
{
    public class AccountController : Controller
    {
        private readonly string url = "http://localhost:5062/api/auth/login";
        private readonly HttpClient httpClient;

        public AccountController(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }


        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var response = await httpClient.PostAsJsonAsync(url, model);
            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Invalid account or password";
                return View(model);
            }
            var result = await response.Content.ReadFromJsonAsync<LoginResult>();
            if (result == null || string.IsNullOrEmpty(result.Token))
            {
                ViewBag.Error = "Login failed: unable to process server response.";
                return View(model);
            }
            HttpContext.Session.SetString("Token", result.Token);
            HttpContext.Session.SetString("RefreshToken", result.RefreshToken);
            HttpContext.Session.SetString("Account", result.AccountNumber);
            HttpContext.Session.SetString("FullName", result.FullName);
            HttpContext.Session.SetString("Balance", result.Balance.ToString());
            return RedirectToAction("Index", "Transfer");

        }


        public async Task<IActionResult> Logout()
        {
            var token = HttpContext.Session.GetString("Token");
            if (!string.IsNullOrEmpty(token))
            {
                try
                {
                    using var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost:5062/api/auth/revoke");
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                    await httpClient.SendAsync(request);
                }
                catch
                {
                    // Ignore revoke network errors on logout
                }
            }

            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}
