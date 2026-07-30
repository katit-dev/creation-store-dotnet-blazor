using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CreationStore.API.Data;
using CreationStore.API.DTOs.Auth;
using CreationStore.API.DTOs.Cart;
using CreationStore.API.DTOs.Order;
using CreationStore.API.DTOs.Payment;
using CreationStore.API.DTOs.ResponseTypes;
using CreationStore.API.Helpers.Constant;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CreationStore.Tests
{
    public class PaymentApiTests :
        IClassFixture<CustomWebApplicationFactory>,
        IAsyncLifetime
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        private static readonly JsonSerializerOptions JsonOptions = new(
            JsonSerializerDefaults.Web
        );

        public PaymentApiTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        public async Task InitializeAsync()
        {
            await CleanupPaymentTestDataAsync();
        }

        public async Task DisposeAsync()
        {
            _client.DefaultRequestHeaders.Authorization = null;
            await CleanupPaymentTestDataAsync();
        }

        [Fact]
        public async Task CreatePayment_WithoutToken_Returns401()
        {
            SetBearerToken(null);

            var response = await _client.PostAsync(
                "/api/payments/vnpay/create-payment/1",
                null
            );

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CreatePayment_OrderNotFound_Returns404()
        {
            var token = await RegisterAndLoginAsync("notfound");
            SetBearerToken(token);

            var response = await _client.PostAsync(
                "/api/payments/vnpay/create-payment/999999999",
                null
            );

            var result = await response.Content
                .ReadFromJsonAsync<ResponseTypeDTO<object>>(JsonOptions);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(404, result!.StatusCode);
        }

        [Fact]
        public async Task CreatePayment_UserCannotPayOtherUserOrder_Returns404()
        {
            var tokenA = await RegisterAndLoginAsync("usera");
            var tokenB = await RegisterAndLoginAsync("userb");

            var orderOfUserB = await CreateOrderAsync(tokenB);

            SetBearerToken(tokenA);

            var response = await _client.PostAsync(
                $"/api/payments/vnpay/create-payment/{orderOfUserB}",
                null
            );

            var result = await response.Content
                .ReadFromJsonAsync<ResponseTypeDTO<object>>(JsonOptions);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(404, result!.StatusCode);
        }

        [Fact]
        public async Task CreatePayment_CancelledOrder_Returns400()
        {
            var token = await RegisterAndLoginAsync("cancelled");
            var orderId = await CreateOrderAsync(token);

            SetBearerToken(token);

            var cancelResponse = await _client.PutAsJsonAsync(
                $"/api/orders/{orderId}/cancel",
                new CancelOrderDTO
                {
                    CancelReason = "Payment auto test cancel"
                }
            );

            Assert.Equal(HttpStatusCode.OK, cancelResponse.StatusCode);

            var response = await _client.PostAsync(
                $"/api/payments/vnpay/create-payment/{orderId}",
                null
            );

            var result = await response.Content
                .ReadFromJsonAsync<ResponseTypeDTO<object>>(JsonOptions);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(400, result!.StatusCode);
        }

        [Fact]
        public async Task CreatePayment_ValidOrder_ReturnsPaymentUrl()
        {
            var token = await RegisterAndLoginAsync("valid");
            var orderId = await CreateOrderAsync(token);

            var payment = await CreateVnPayPaymentAsync(token, orderId);

            Assert.Equal(orderId, payment.OrderId);
            Assert.True(payment.PaymentTransactionId > 0);
            Assert.True(payment.Amount > 0);
            Assert.False(string.IsNullOrWhiteSpace(payment.VnpTxnRef));
            Assert.Contains("sandbox.vnpayment.vn", payment.PaymentUrl);
            Assert.Contains("vnp_SecureHash=", payment.PaymentUrl);
        }

        [Fact]
        public async Task CreatePayment_CreatesPendingTransactionInDb()
        {
            var token = await RegisterAndLoginAsync("pendingdb");
            var orderId = await CreateOrderAsync(token);

            var payment = await CreateVnPayPaymentAsync(token, orderId);

            using var scope = _factory.Services.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<CreationStoreDbContext>();

            var transaction = await db.PaymentTransactions
                .AsNoTracking()
                .FirstOrDefaultAsync(pt =>
                    pt.PaymentTransactionId == payment.PaymentTransactionId
                );

            Assert.NotNull(transaction);
            Assert.Equal(orderId, transaction!.OrderId);
            Assert.Equal(CPaymentMethod.VnPay, transaction.PaymentMethod);
            Assert.Equal(CPaymentTransactionStatus.Pending, transaction.TransactionStatus);
            Assert.Equal(payment.VnpTxnRef, transaction.VnpTxnRef);
        }

        [Fact]
        public async Task GetMyTransactions_WithoutToken_Returns401()
        {
            SetBearerToken(null);

            var response = await _client.GetAsync(
                "/api/payments/my-transactions"
            );

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetMyTransactions_ValidToken_Returns200()
        {
            var token = await RegisterAndLoginAsync("list");
            var orderId = await CreateOrderAsync(token);

            await CreateVnPayPaymentAsync(token, orderId);

            SetBearerToken(token);

            var response = await _client.GetAsync(
                "/api/payments/my-transactions"
            );

            var result = await response.Content
                .ReadFromJsonAsync<
                    ResponseTypeDTO<List<PaymentTransactionResponseDTO>>
                >(JsonOptions);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
            Assert.NotNull(result.Content);
            Assert.NotEmpty(result.Content!);
        }

        [Fact]
        public async Task GetMyTransactions_OnlyOwnTransactions()
        {
            var tokenA = await RegisterAndLoginAsync("owna");
            var tokenB = await RegisterAndLoginAsync("ownb");

            var orderA = await CreateOrderAsync(tokenA);
            var paymentA = await CreateVnPayPaymentAsync(tokenA, orderA);

            var orderB = await CreateOrderAsync(tokenB);
            var paymentB = await CreateVnPayPaymentAsync(tokenB, orderB);

            SetBearerToken(tokenA);

            var response = await _client.GetAsync(
                "/api/payments/my-transactions"
            );

            var result = await response.Content
                .ReadFromJsonAsync<
                    ResponseTypeDTO<List<PaymentTransactionResponseDTO>>
                >(JsonOptions);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.NotNull(result!.Content);

            Assert.Contains(
                result.Content!,
                item => item.PaymentTransactionId ==
                    paymentA.PaymentTransactionId
            );

            Assert.DoesNotContain(
                result.Content!,
                item => item.PaymentTransactionId ==
                    paymentB.PaymentTransactionId
            );
        }

        [Fact]
        public async Task GetMyTransactionById_WithoutToken_Returns401()
        {
            SetBearerToken(null);

            var response = await _client.GetAsync("/api/payments/1");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetMyTransactionById_NotFound_Returns404()
        {
            var token = await RegisterAndLoginAsync("detailnotfound");
            SetBearerToken(token);

            var response = await _client.GetAsync(
                "/api/payments/999999999"
            );

            var result = await response.Content
                .ReadFromJsonAsync<ResponseTypeDTO<object>>(JsonOptions);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(404, result!.StatusCode);
        }

        [Fact]
        public async Task GetMyTransactionById_OtherUser_Returns404()
        {
            var tokenA = await RegisterAndLoginAsync("detaila");
            var tokenB = await RegisterAndLoginAsync("detailb");

            var orderB = await CreateOrderAsync(tokenB);
            var paymentB = await CreateVnPayPaymentAsync(tokenB, orderB);

            SetBearerToken(tokenA);

            var response = await _client.GetAsync(
                $"/api/payments/{paymentB.PaymentTransactionId}"
            );

            var result = await response.Content
                .ReadFromJsonAsync<ResponseTypeDTO<object>>(JsonOptions);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(404, result!.StatusCode);
        }

        [Fact]
        public async Task GetMyTransactionById_Valid_Returns200()
        {
            var token = await RegisterAndLoginAsync("detailvalid");
            var orderId = await CreateOrderAsync(token);
            var payment = await CreateVnPayPaymentAsync(token, orderId);

            SetBearerToken(token);

            var response = await _client.GetAsync(
                $"/api/payments/{payment.PaymentTransactionId}"
            );

            var result = await response.Content
                .ReadFromJsonAsync<
                    ResponseTypeDTO<PaymentTransactionResponseDTO>
                >(JsonOptions);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
            Assert.NotNull(result.Content);
            Assert.Equal(
                payment.PaymentTransactionId,
                result.Content!.PaymentTransactionId
            );
            Assert.Equal(payment.VnpTxnRef, result.Content.VnpTxnRef);
        }

        [Fact]
        public async Task VnPayReturn_InvalidSignature_Returns400()
        {
            var token = await RegisterAndLoginAsync("invalidsig");
            var orderId = await CreateOrderAsync(token);
            var payment = await CreateVnPayPaymentAsync(token, orderId);

            var url = BuildVnPayReturnUrl(
                payment.VnpTxnRef,
                payment.Amount,
                "00",
                "00"
            );

            url += "wrong";

            var response = await _client.GetAsync(url);

            var result = await response.Content
                .ReadFromJsonAsync<ResponseTypeDTO<VnPayReturnResponseDTO>>(
                    JsonOptions
                );

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(400, result!.StatusCode);
            Assert.NotNull(result.Content);
            Assert.False(result.Content!.IsValidSignature);
        }

        [Fact]
        public async Task VnPayReturn_Success_UpdatesOrderAndTransaction()
        {
            var token = await RegisterAndLoginAsync("returnsuccess");
            var orderId = await CreateOrderAsync(token);
            var payment = await CreateVnPayPaymentAsync(token, orderId);

            var url = BuildVnPayReturnUrl(
                payment.VnpTxnRef,
                payment.Amount,
                "00",
                "00"
            );

            var response = await _client.GetAsync(url);

            var result = await response.Content
                .ReadFromJsonAsync<ResponseTypeDTO<VnPayReturnResponseDTO>>(
                    JsonOptions
                );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
            Assert.NotNull(result.Content);
            Assert.True(result.Content!.IsValidSignature);
            Assert.True(result.Content.IsSuccess);

            using var scope = _factory.Services.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<CreationStoreDbContext>();

            var transaction = await db.PaymentTransactions
                .AsNoTracking()
                .Include(pt => pt.Order)
                .FirstAsync(pt =>
                    pt.PaymentTransactionId ==
                    payment.PaymentTransactionId
                );

            Assert.Equal(
                CPaymentTransactionStatus.Succeeded,
                transaction.TransactionStatus
            );
            Assert.Equal("00", transaction.VnpResponseCode);
            Assert.Equal("00", transaction.VnpTransactionStatus);
            Assert.NotNull(transaction.PaidAt);
            Assert.NotNull(transaction.RawResponse);

            Assert.Equal(COrderStatus.Paid, transaction.Order.Status);
            Assert.Equal(
                CPaymentStatus.Succeeded,
                transaction.Order.PaymentStatus
            );
        }

        [Fact]
        public async Task VnPayReturn_Failed_UpdatesFailedStatus()
        {
            var token = await RegisterAndLoginAsync("returnfailed");
            var orderId = await CreateOrderAsync(token);
            var payment = await CreateVnPayPaymentAsync(token, orderId);

            var url = BuildVnPayReturnUrl(
                payment.VnpTxnRef,
                payment.Amount,
                "24",
                "02"
            );

            var response = await _client.GetAsync(url);

            var result = await response.Content
                .ReadFromJsonAsync<ResponseTypeDTO<VnPayReturnResponseDTO>>(
                    JsonOptions
                );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
            Assert.NotNull(result.Content);
            Assert.True(result.Content!.IsValidSignature);
            Assert.False(result.Content.IsSuccess);

            using var scope = _factory.Services.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<CreationStoreDbContext>();

            var transaction = await db.PaymentTransactions
                .AsNoTracking()
                .Include(pt => pt.Order)
                .FirstAsync(pt =>
                    pt.PaymentTransactionId ==
                    payment.PaymentTransactionId
                );

            Assert.Equal(
                CPaymentTransactionStatus.Failed,
                transaction.TransactionStatus
            );
            Assert.Equal("24", transaction.VnpResponseCode);
            Assert.Equal("02", transaction.VnpTransactionStatus);
            Assert.Null(transaction.PaidAt);

            Assert.Equal(
                COrderStatus.PendingPayment,
                transaction.Order.Status
            );
            Assert.Equal(
                CPaymentStatus.Failed,
                transaction.Order.PaymentStatus
            );
        }

        [Fact]
        public async Task VnPayReturn_SuccessCalledTwice_ReturnsAlreadyProcessed()
        {
            var token = await RegisterAndLoginAsync("twice");
            var orderId = await CreateOrderAsync(token);
            var payment = await CreateVnPayPaymentAsync(token, orderId);

            var url = BuildVnPayReturnUrl(
                payment.VnpTxnRef,
                payment.Amount,
                "00",
                "00"
            );

            var firstResponse = await _client.GetAsync(url);
            Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

            var secondResponse = await _client.GetAsync(url);

            var secondResult = await secondResponse.Content
                .ReadFromJsonAsync<ResponseTypeDTO<VnPayReturnResponseDTO>>(
                    JsonOptions
                );

            Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
            Assert.NotNull(secondResult);
            Assert.Equal(200, secondResult!.StatusCode);
            Assert.Contains(
                "already processed",
                secondResult.Message!,
                StringComparison.OrdinalIgnoreCase
            );
        }

        [Fact]
        public async Task VnPayReturn_AmountMismatch_Returns400()
        {
            var token = await RegisterAndLoginAsync("amountmismatch");
            var orderId = await CreateOrderAsync(token);
            var payment = await CreateVnPayPaymentAsync(token, orderId);

            var wrongAmount = payment.Amount + 1000;

            var url = BuildVnPayReturnUrl(
                payment.VnpTxnRef,
                wrongAmount,
                "00",
                "00"
            );

            var response = await _client.GetAsync(url);

            var result = await response.Content
                .ReadFromJsonAsync<ResponseTypeDTO<VnPayReturnResponseDTO>>(
                    JsonOptions
                );

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(400, result!.StatusCode);
            Assert.Contains(
                "amount",
                result.Message!,
                StringComparison.OrdinalIgnoreCase
            );
        }

        private async Task<string> RegisterAndLoginAsync(string testName)
        {
            var suffix = Guid.NewGuid()
                .ToString("N")
                .Substring(0, 10);

            var username = $"paymenttest{testName}{suffix}";
            var password = "123456";
            var phone = "09" + Random.Shared
                .Next(10000000, 99999999)
                .ToString();

            var registerDto = new RegisterDTO
            {
                Username = username,
                Password = password,
                FullName = "Payment Auto Test User",
                Email = $"{username}@gmail.com",
                Phone = phone
            };

            var registerResponse = await _client.PostAsJsonAsync(
                "/api/auth/register",
                registerDto
            );

            Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

            var loginDto = new LoginDTO
            {
                LoginIdentifier = username,
                Password = password
            };

            var loginResponse = await _client.PostAsJsonAsync(
                "/api/auth/login",
                loginDto
            );

            var loginResult = await loginResponse.Content
                .ReadFromJsonAsync<ResponseTypeDTO<LoginResponseDTO>>(
                    JsonOptions
                );

            Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
            Assert.NotNull(loginResult);
            Assert.NotNull(loginResult!.Content);
            Assert.False(
                string.IsNullOrWhiteSpace(loginResult.Content!.Token)
            );

            return loginResult.Content.Token;
        }

        private async Task<int> CreateOrderAsync(string token)
        {
            SetBearerToken(token);

            var productId = await GetActiveProductIdAsync();

            var addCartResponse = await _client.PostAsJsonAsync(
                "/api/cart/items",
                new AddCartItemDTO
                {
                    ProductId = productId,
                    Quantity = 1
                }
            );

            Assert.Equal(HttpStatusCode.OK, addCartResponse.StatusCode);

            var checkoutResponse = await _client.PostAsJsonAsync(
                "/api/orders/checkout",
                new CheckoutOrderDTO
                {
                    Note = "Payment auto test checkout"
                }
            );

            var checkoutResult = await checkoutResponse.Content
                .ReadFromJsonAsync<ResponseTypeDTO<OrderResponseDTO>>(
                    JsonOptions
                );

            Assert.Equal(HttpStatusCode.Created, checkoutResponse.StatusCode);
            Assert.NotNull(checkoutResult);
            Assert.NotNull(checkoutResult!.Content);
            Assert.True(checkoutResult.Content!.OrderId > 0);

            return checkoutResult.Content.OrderId;
        }

        private async Task<CreateVnPayPaymentResponseDTO>
            CreateVnPayPaymentAsync(string token, int orderId)
        {
            SetBearerToken(token);

            var response = await _client.PostAsync(
                $"/api/payments/vnpay/create-payment/{orderId}",
                null
            );

            var result = await response.Content
                .ReadFromJsonAsync<
                    ResponseTypeDTO<CreateVnPayPaymentResponseDTO>
                >(JsonOptions);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
            Assert.NotNull(result.Content);

            return result.Content!;
        }

        private async Task<int> GetActiveProductIdAsync()
        {
            using var scope = _factory.Services.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<CreationStoreDbContext>();

            var product = await db.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.IsActive);

            Assert.NotNull(product);

            return product!.ProductId;
        }

        private void SetBearerToken(string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                _client.DefaultRequestHeaders.Authorization = null;
                return;
            }

            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        private string BuildVnPayReturnUrl(
            string vnpTxnRef,
            decimal amount,
            string responseCode,
            string transactionStatus
        )
        {
            var vnPayConfig = GetVnPayConfig();

            var now = DateTime.UtcNow
                .AddHours(7)
                .ToString("yyyyMMddHHmmss");

            var vnpAmount = Convert.ToInt64(amount * 100)
                .ToString(CultureInfo.InvariantCulture);

            var vnpParams = new SortedDictionary<string, string>(
                StringComparer.Ordinal
            )
            {
                { "vnp_Amount", vnpAmount },
                { "vnp_BankCode", "NCB" },
                { "vnp_BankTranNo", "VNP12345678" },
                { "vnp_CardType", "ATM" },
                { "vnp_OrderInfo", "Thanh toan don hang auto test" },
                { "vnp_PayDate", now },
                { "vnp_ResponseCode", responseCode },
                { "vnp_TmnCode", vnPayConfig.TmnCode },
                { "vnp_TransactionNo", Random.Shared.Next(10000000, 99999999).ToString() },
                { "vnp_TransactionStatus", transactionStatus },
                { "vnp_TxnRef", vnpTxnRef }
            };

            var signData = BuildQueryString(vnpParams);
            var secureHash = HmacSha512(vnPayConfig.HashSecret, signData);

            return "/api/payments/vnpay-return?" +
                signData +
                "&vnp_SecureHash=" +
                secureHash;
        }

        private VnPayTestConfig GetVnPayConfig()
        {
            using var scope = _factory.Services.CreateScope();

            var configuration = scope.ServiceProvider
                .GetRequiredService<IConfiguration>();

            var tmnCode = configuration["VnPay:TmnCode"]?.Trim();
            var hashSecret = configuration["VnPay:HashSecret"]?.Trim();

            Assert.False(string.IsNullOrWhiteSpace(tmnCode));
            Assert.False(string.IsNullOrWhiteSpace(hashSecret));

            return new VnPayTestConfig(tmnCode!, hashSecret!);
        }

        private static string BuildQueryString(
            SortedDictionary<string, string> data
        )
        {
            var queryParts = new List<string>();

            foreach (var item in data)
            {
                if (string.IsNullOrWhiteSpace(item.Value))
                {
                    continue;
                }

                var key = WebUtility.UrlEncode(item.Key);
                var value = WebUtility.UrlEncode(item.Value);

                queryParts.Add($"{key}={value}");
            }

            return string.Join("&", queryParts);
        }

        private static string HmacSha512(string key, string inputData)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var inputBytes = Encoding.UTF8.GetBytes(inputData);

            using var hmac = new HMACSHA512(keyBytes);

            var hashBytes = hmac.ComputeHash(inputBytes);

            return Convert.ToHexString(hashBytes).ToLower();
        }

        private async Task CleanupPaymentTestDataAsync()
        {
            using var scope = _factory.Services.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<CreationStoreDbContext>();

            var userIds = await db.Users
                .Where(u => u.Username.StartsWith("paymenttest"))
                .Select(u => u.UserId)
                .ToListAsync();

            if (!userIds.Any())
            {
                return;
            }

            var orderIds = await db.Orders
                .Where(o => userIds.Contains(o.UserId))
                .Select(o => o.OrderId)
                .ToListAsync();

            var paymentTransactions = await db.PaymentTransactions
                .Where(pt => orderIds.Contains(pt.OrderId))
                .ToListAsync();

            db.PaymentTransactions.RemoveRange(paymentTransactions);

            var orderItems = await db.OrderItems
                .Where(oi => orderIds.Contains(oi.OrderId))
                .ToListAsync();

            db.OrderItems.RemoveRange(orderItems);

            var orders = await db.Orders
                .Where(o => orderIds.Contains(o.OrderId))
                .ToListAsync();

            db.Orders.RemoveRange(orders);

            var cartIds = await db.Carts
                .Where(c => userIds.Contains(c.UserId))
                .Select(c => c.CartId)
                .ToListAsync();

            var cartItems = await db.CartItems
                .Where(ci => cartIds.Contains(ci.CartId))
                .ToListAsync();

            db.CartItems.RemoveRange(cartItems);

            var carts = await db.Carts
                .Where(c => cartIds.Contains(c.CartId))
                .ToListAsync();

            db.Carts.RemoveRange(carts);

            var userRoles = await db.UserRoles
                .Where(ur => userIds.Contains(ur.UserId))
                .ToListAsync();

            db.UserRoles.RemoveRange(userRoles);

            var users = await db.Users
                .Where(u => userIds.Contains(u.UserId))
                .ToListAsync();

            db.Users.RemoveRange(users);

            await db.SaveChangesAsync();
        }

        private record VnPayTestConfig(
            string TmnCode,
            string HashSecret
        );
    }
}