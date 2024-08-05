namespace NewsAggregation.Services
{
    using OtpNet;
    using QRCoder;
    using System.Security.Cryptography;

    public class MfaService
    {
        public string GenerateTotpSecret()
        {
            var key = KeyGeneration.GenerateRandomKey(40);
            return Base32Encoding.ToString(key);
        }

        public string GenerateQrCodeUri(string email, string secret)
        {
            string issuer = "news-aggregation";
            string otpauth = $"otpauth://totp/{issuer}:{email}?secret={secret}&issuer={issuer}";
            return otpauth;
        }

        public byte[] GenerateQrCodeImage(string otpauth)
        {
            using (var qrGenerator = new QRCodeGenerator())
            {
                using (var qrCodeData = qrGenerator.CreateQrCode(otpauth, QRCodeGenerator.ECCLevel.Q))
                {
                    using (var qrCode = new PngByteQRCode(qrCodeData))
                    {
                        return qrCode.GetGraphic(20);
                    }
                }
            }
        }

        public string GenerateBackupCodes()
        {
            var backupCodes = new List<string>();
            for (int i = 0; i < 10; i++)
            {
                backupCodes.Add(GenerateRandomCode());
            }
            return string.Join(",", backupCodes);
        }

        private string GenerateRandomCode()
        {
            var random = new byte[10];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(random);
            }

            return Convert.ToBase64String(random);

        }
    }

}
