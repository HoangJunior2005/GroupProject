using LearningDocumentSystem.Business.DTOs;
using LearningDocumentSystem.Business.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace LearningDocumentSystem.Business.Services.Implementations
{
    public class VnpayService : IVnpayService
    {
        private readonly IConfiguration _configuration;
        private readonly IPackageService _packageService;

        public VnpayService(IConfiguration configuration, IPackageService packageService)
        {
            _configuration = configuration;
            _packageService = packageService;
        }

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(_configuration["Vnpay:TmnCode"])
            && !string.IsNullOrWhiteSpace(_configuration["Vnpay:HashSecret"]);

        public string CreatePaymentUrl(PackagePlanDto plan, int userId, string ipAddress, string returnUrl)
        {
            if (!IsConfigured)
                throw new InvalidOperationException("VNPAY chua duoc cau hinh TmnCode va HashSecret.");
            if (plan.Price <= 0)
                throw new InvalidOperationException("Goi mien phi khong can thanh toan.");

            var now = GetVietnamTime();
            var transactionReference = $"LDS{userId}{plan.Code}{now:yyyyMMddHHmmssfff}";
            var data = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                ["vnp_Version"] = _configuration["Vnpay:Version"] ?? "2.1.0",
                ["vnp_Command"] = _configuration["Vnpay:Command"] ?? "pay",
                ["vnp_TmnCode"] = _configuration["Vnpay:TmnCode"]!,
                ["vnp_Amount"] = ((long)(plan.Price * 100)).ToString(CultureInfo.InvariantCulture),
                ["vnp_CreateDate"] = now.ToString("yyyyMMddHHmmss"),
                ["vnp_ExpireDate"] = now.AddMinutes(15).ToString("yyyyMMddHHmmss"),
                ["vnp_CurrCode"] = "VND",
                ["vnp_IpAddr"] = NormalizeIpAddress(ipAddress),
                ["vnp_Locale"] = "vn",
                ["vnp_OrderInfo"] = $"Thanh toan goi {plan.Code} cho tai khoan LDS {userId}",
                ["vnp_OrderType"] = "other",
                ["vnp_ReturnUrl"] = returnUrl,
                ["vnp_TxnRef"] = transactionReference
            };

            var query = BuildQuery(data);
            var secureHash = HmacSha512(_configuration["Vnpay:HashSecret"]!.Trim(), query);
            var paymentUrl = _configuration["Vnpay:PaymentUrl"]
                ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
            var fullUrl = $"{paymentUrl}?{query}&vnp_SecureHash={secureHash}";

            return fullUrl;
        }

        public VnpayPaymentResultDto ValidatePayment(IQueryCollection query)
        {
            var result = new VnpayPaymentResultDto
            {
                TransactionReference = query["vnp_TxnRef"].ToString()
            };

            if (!IsConfigured)
            {
                result.Message = "VNPAY chua duoc cau hinh.";
                return result;
            }

            var receivedHash = query["vnp_SecureHash"].ToString();
            var data = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (var item in query)
            {
                if (item.Key.Equals("vnp_SecureHash", StringComparison.OrdinalIgnoreCase)
                    || item.Key.Equals("vnp_SecureHashType", StringComparison.OrdinalIgnoreCase)
                    || string.IsNullOrWhiteSpace(item.Value))
                    continue;
                data[item.Key] = item.Value.ToString();
            }

            var expectedHash = HmacSha512(_configuration["Vnpay:HashSecret"]!.Trim(), BuildQuery(data));
            if (receivedHash.Length != expectedHash.Length
                || !CryptographicOperations.FixedTimeEquals(
                    Encoding.ASCII.GetBytes(expectedHash.ToLowerInvariant()),
                    Encoding.ASCII.GetBytes(receivedHash.ToLowerInvariant())))
            {
                result.Message = "Chu ky VNPAY khong hop le.";
                return result;
            }

            result.IsValid = true;
            var referenceMatch = Regex.Match(
                result.TransactionReference,
                @"^LDS(?<userId>\d+)(?<plan>Free|Plus|Pro)(?<timestamp>\d{17})$",
                RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
            if (!referenceMatch.Success
                || !int.TryParse(referenceMatch.Groups["userId"].Value, out var userId))
            {
                result.Message = "Ma giao dich khong hop le.";
                return result;
            }

            var plan = _packageService.FindPlan(referenceMatch.Groups["plan"].Value);
            if (plan == null || !plan.IsActive || plan.Price <= 0)
            {
                result.Message = "Goi thanh toan khong hop le.";
                return result;
            }

            if (!long.TryParse(query["vnp_Amount"], out var amount)
                || amount != (long)(plan.Price * 100))
            {
                result.Message = "So tien thanh toan khong khop voi goi da chon.";
                return result;
            }

            result.UserId = userId;
            result.PlanCode = plan.Code;
            result.Amount = amount / 100m;  // vnp_Amount là đơn vị x100, chuyển về VNĐ thực
            result.IsSuccess = query["vnp_ResponseCode"] == "00"
                && (string.IsNullOrWhiteSpace(query["vnp_TransactionStatus"])
                    || query["vnp_TransactionStatus"] == "00");
            result.Message = result.IsSuccess
                ? "Thanh toan thanh cong."
                : "Giao dich chua thanh cong hoac da bi huy.";
            return result;
        }

        // VNPay yêu cầu hash tính trên chuỗi WebUtility.UrlEncode (space = '+'), khớp với
        // tài liệu chính thức VNPay C# sample: WebUtility.UrlEncode(key)=WebUtility.UrlEncode(value)
        private static string BuildQuery(IEnumerable<KeyValuePair<string, string>> data)
            => string.Join("&", data.Select(item =>
                $"{WebUtility.UrlEncode(item.Key)}={WebUtility.UrlEncode(item.Value)}"));

        private static string HmacSha512(string key, string input)
        {
            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
            return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(input))).ToLowerInvariant();
        }

        private static DateTime GetVietnamTime()
        {
            try { return TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "SE Asia Standard Time"); }
            catch { return TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Asia/Ho_Chi_Minh"); }
        }

        private static string NormalizeIpAddress(string ipAddress)
            => string.IsNullOrWhiteSpace(ipAddress) || ipAddress == "::1" ? "127.0.0.1" : ipAddress;
    }
}
