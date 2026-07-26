using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using CreationStore.API.Services.Interfaces;
using Microsoft.AspNetCore.Http;

namespace CreationStore.API.Services.Implementations
{
    public class VnPayService : IVnPayService
    {
        private readonly IConfiguration _configuration;

        public VnPayService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // ============================================================
        // CREATE PAYMENT URL
        // Mục đích:
        // - Tạo URL thanh toán VNPAY sandbox
        // - URL này sẽ trả về cho frontend
        // - Frontend redirect user sang URL này để thanh toán
        // ============================================================
        public string CreatePaymentUrl(
            string vnpTxnRef,
            decimal amount,
            string orderInfo,
            string ipAddress
        )
        {
            var baseUrl = _configuration["VnPay:BaseUrl"];
            var tmnCode = _configuration["VnPay:TmnCode"];
            var hashSecret = _configuration["VnPay:HashSecret"];
            var returnUrl = _configuration["VnPay:ReturnUrl"];

            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new Exception("VNPAY BaseUrl is missing");

            if (string.IsNullOrWhiteSpace(tmnCode))
                throw new Exception("VNPAY TmnCode is missing");

            if (string.IsNullOrWhiteSpace(hashSecret))
                throw new Exception("VNPAY HashSecret is missing");

            if (string.IsNullOrWhiteSpace(returnUrl))
                throw new Exception("VNPAY ReturnUrl is missing");

            var now = DateTime.Now;
            var expireDate = now.AddMinutes(15);

            // VNPAY yêu cầu amount * 100
            // Ví dụ 100000 VND gửi sang là 10000000
            var vnpAmount = Convert.ToInt64(amount * 100)
                .ToString(CultureInfo.InvariantCulture);

            var vnpParams = new SortedDictionary<string, string>
            {
                { "vnp_Version", "2.1.0" },
                { "vnp_Command", "pay" },
                { "vnp_TmnCode", tmnCode },
                { "vnp_Amount", vnpAmount },
                { "vnp_CreateDate", now.ToString("yyyyMMddHHmmss") },
                { "vnp_ExpireDate", expireDate.ToString("yyyyMMddHHmmss") },
                { "vnp_CurrCode", "VND" },
                { "vnp_IpAddr", ipAddress },
                { "vnp_Locale", "vn" },
                { "vnp_OrderInfo", orderInfo },
                { "vnp_OrderType", "other" },
                { "vnp_ReturnUrl", returnUrl },
                { "vnp_TxnRef", vnpTxnRef }
            };

            var signData = BuildQueryString(vnpParams);

            var secureHash = HmacSha512(hashSecret, signData);

            var paymentUrl =
                baseUrl +
                "?" +
                signData +
                "&vnp_SecureHash=" +
                secureHash;

            return paymentUrl;
        }

        // ============================================================
        // VALIDATE SIGNATURE
        // Mục đích:
        // - Khi VNPAY redirect về ReturnUrl
        // - Backend phải kiểm tra vnp_SecureHash
        // - Nếu chữ ký sai thì không được update order thành paid
        // ============================================================
        public bool ValidateSignature(IQueryCollection query)
        {
            var hashSecret = _configuration["VnPay:HashSecret"];

            if (string.IsNullOrWhiteSpace(hashSecret))
                throw new Exception("VNPAY HashSecret is missing");

            var vnpSecureHash = query["vnp_SecureHash"].ToString();

            if (string.IsNullOrWhiteSpace(vnpSecureHash))
                return false;

            var vnpParams = new SortedDictionary<string, string>();

            foreach (var item in query)
            {
                var key = item.Key;
                var value = item.Value.ToString();

                if (string.IsNullOrWhiteSpace(value))
                    continue;

                if (key == "vnp_SecureHash")
                    continue;

                if (key == "vnp_SecureHashType")
                    continue;

                if (!key.StartsWith("vnp_"))
                    continue;

                vnpParams.Add(key, value);
            }

            var signData = BuildQueryString(vnpParams);

            var calculatedHash = HmacSha512(hashSecret, signData);

            return string.Equals(
                calculatedHash,
                vnpSecureHash,
                StringComparison.OrdinalIgnoreCase
            );
        }

        // ============================================================
        // GET IP ADDRESS
        // Mục đích:
        // - Lấy IP của client
        // - Gửi sang VNPAY qua tham số vnp_IpAddr
        // ============================================================
        public string GetIpAddress(HttpContext? httpContext)
        {
            var ipAddress = httpContext?
                .Connection
                .RemoteIpAddress?
                .ToString();

            if (string.IsNullOrWhiteSpace(ipAddress))
            {
                return "127.0.0.1";
            }

            if (ipAddress == "::1")
            {
                return "127.0.0.1";
            }

            return ipAddress;
        }

        // ============================================================
        // BUILD QUERY STRING
        // Mục đích:
        // - Convert dictionary thành chuỗi:
        //   key=value&key=value
        // - SortedDictionary giúp key đã được sắp xếp tăng dần
        // ============================================================
        private static string BuildQueryString(
            SortedDictionary<string, string> data
        )
        {
            var queryParts = new List<string>();

            foreach (var item in data)
            {
                var key = WebUtility.UrlEncode(item.Key);
                var value = WebUtility.UrlEncode(item.Value);

                queryParts.Add($"{key}={value}");
            }

            return string.Join("&", queryParts);
        }

        // ============================================================
        // HMAC SHA512
        // Mục đích:
        // - Tạo chữ ký bảo mật vnp_SecureHash
        // ============================================================
        private static string HmacSha512(string key, string inputData)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var inputBytes = Encoding.UTF8.GetBytes(inputData);

            using var hmac = new HMACSHA512(keyBytes);

            var hashBytes = hmac.ComputeHash(inputBytes);

            return Convert.ToHexString(hashBytes).ToLower();
        }
    }
}