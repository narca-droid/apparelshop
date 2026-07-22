using System.Net;
using System.Net.Mail;
using ApparelShop.Models;

namespace ApparelShop.Services;

public interface IEmailService
{
    Task SendOrderConfirmationAsync(Order order);
}

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendOrderConfirmationAsync(Order order)
    {
        var host = _config["Email:SmtpHost"];
        var port = int.TryParse(_config["Email:SmtpPort"], out var p) ? p : 587;
        var user = _config["Email:SmtpUser"];
        var pass = _config["Email:SmtpPass"];
        var from = _config["Email:FromAddress"] ?? "noreply@apparelshop.com";

        if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(user))
        {
            _logger.LogWarning("Email not configured. Skipping order confirmation for {OrderNumber}.", order.OrderNumber);
            return;
        }

        try
        {
            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(user, pass),
                EnableSsl = true
            };

            var itemsHtml = string.Join("", order.Items.Select(i =>
                $"<tr><td>{i.ProductName}</td><td>{i.Size ?? "-"}</td><td>{i.Quantity}</td><td>AED {i.LineTotal:F2}</td></tr>"));

            var body = $"""
                <div style="font-family:Arial,sans-serif;max-width:600px;margin:auto;">
                    <h2 style="color:#c9a227;">Order Confirmed!</h2>
                    <p>Thank you <strong>{order.CustomerName}</strong>, your order <strong>{order.OrderNumber}</strong> has been placed.</p>
                    <table style="width:100%;border-collapse:collapse;margin:20px 0;">
                        <thead><tr style="background:#f4f2ec;"><th>Product</th><th>Size</th><th>Qty</th><th>Total</th></tr></thead>
                        <tbody>{itemsHtml}</tbody>
                    </table>
                    <p><strong>Subtotal:</strong> AED {order.Subtotal:F2}</p>
                    <p><strong>Shipping:</strong> AED {order.ShippingFee:F2}</p>
                    <p style="font-size:1.2em;"><strong>Total:</strong> AED {order.Total:F2}</p>
                    <hr/>
                    <p style="color:#888;font-size:0.85em;">Shipping to: {order.ShippingAddress}, {order.City}, {order.Emirate}</p>
                </div>
                """;

            var mailMessage = new MailMessage
            {
                From = new MailAddress(from, "AURA Apparel"),
                Subject = $"Order {order.OrderNumber} Confirmed",
                Body = body,
                IsBodyHtml = true
            };
            mailMessage.To.Add(order.CustomerEmail);

            await client.SendMailAsync(mailMessage);
            _logger.LogInformation("Order confirmation email sent to {Email} for order {OrderNumber}.", order.CustomerEmail, order.OrderNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send order confirmation email for {OrderNumber}.", order.OrderNumber);
        }
    }
}
