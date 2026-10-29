using QRCoder;

namespace CarProject.Services;

public static class VietQr
{
    public const string NapasGuid = "A000000727";
    public const string ServiceCode = "QRIBFTTA";
    public const string VndCurrency = "704";

    public static string BuildPayload(string bin, string account, long? amount, string? content)
    {
        string Tlv(string id, string value) => id + value.Length.ToString("00") + value;

        var merchantAccount =
            Tlv("00", NapasGuid) +
            Tlv("01", Tlv("00", bin) + Tlv("01", account)) +
            Tlv("02", ServiceCode);

        var parts = "000201" + "010211" + Tlv("38", merchantAccount) + Tlv("53", VndCurrency);
        if (amount.HasValue) parts += Tlv("54", amount.Value.ToString());
        parts += Tlv("58", "VN");
        if (!string.IsNullOrEmpty(content)) parts += Tlv("62", Tlv("08", content));

        var withCrc = parts + "6304";
        return withCrc + Crc16(withCrc);
    }

    public static string BuildDataUri(string bin, string account, long? amount, string? content)
    {
        var payload = BuildPayload(bin, account, amount, content);

        using var generator = new QRCodeGenerator();
        using var qrData = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q, forceUtf8: true);
        var pngBytes = new PngByteQRCode(qrData).GetGraphic(20);
        return "data:image/png;base64," + Convert.ToBase64String(pngBytes);
    }

    private static string Crc16(string data)
    {
        ushort crc = 0xFFFF;
        foreach (var ch in data)
        {
            crc ^= (ushort)((byte)ch << 8);
            for (var i = 0; i < 8; i++)
                crc = (crc & 0x8000) != 0
                    ? (ushort)((crc << 1) ^ 0x1021)
                    : (ushort)(crc << 1);
        }
        return crc.ToString("X4");
    }
}
