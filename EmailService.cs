using System;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace FinancyApplication
{
	public class EmailService
	{
		private readonly string senderEmail = "chimc8699@gmail.com";  
		private readonly string senderName = "Financy App";
		private readonly string appPassword = "egzgwususpzjcirq";    

		public bool SendResetToken(string toEmail, string recipientName, string token)
		{
			try
			{
				var message = new MimeMessage();
				message.From.Add(new MailboxAddress(senderName, senderEmail));
				message.To.Add(new MailboxAddress(recipientName, toEmail));
				message.Subject = "Your Financy Password Reset Code";

				message.Body = new TextPart("html")
				{
					Text = BuildEmailBody(recipientName, token)
				};

				using (var client = new SmtpClient())
				{
					client.Connect("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
					client.Authenticate(senderEmail, appPassword);
					client.Send(message);
					client.Disconnect(true);
				}

				return true;
			}
			catch (Exception ex)
			{
				Console.WriteLine("Email error: " + ex.Message);
				return false;
			}
		}

		private string BuildEmailBody(string name, string token)
		{
			return $@"
            <div style='font-family:Arial,sans-serif;max-width:480px;margin:auto;border:1px solid #e0e0e0;border-radius:10px;padding:32px;'>
                <h2 style='color:#2e7d32;'>Financy</h2>
                <p>Hi {name},</p>
                <p>We received a request to reset your password. Use the code below:</p>
                <div style='text-align:center;margin:32px 0;'>
                    <span style='font-size:36px;font-weight:bold;letter-spacing:8px;color:#2e7d32;'>{token}</span>
                </div>
                <p>Enter this code in the app to reset your password.</p>
                <p style='color:#999;font-size:12px;'>If you didn't request this, you can safely ignore this email.</p>
            </div>";
		}

		public bool SendVerificationCode(string toEmail, string recipientName, string code)
		{
			try
			{
				var message = new MimeMessage();
				message.From.Add(new MailboxAddress(senderName, senderEmail));
				message.To.Add(new MailboxAddress(recipientName, toEmail));
				message.Subject = "Verify your Financy account";

				message.Body = new TextPart("html")
				{
					Text = BuildVerificationBody(recipientName, code)
				};

				using (var client = new SmtpClient())
				{
					client.Connect("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
					client.Authenticate(senderEmail, appPassword);
					client.Send(message);
					client.Disconnect(true);
				}

				return true;
			}
			catch (Exception ex)
			{
				Console.WriteLine("Email error: " + ex.Message);
				return false;
			}
		}

		private string BuildVerificationBody(string name, string code)
		{
			return $@"
    <div style='font-family:Arial,sans-serif;max-width:480px;margin:auto;border:1px solid #e0e0e0;border-radius:10px;padding:32px;'>
        <h2 style='color:#2e7d32;'>Financy</h2>
        <p>Hi {name},</p>
        <p>Welcome! Please verify your account using the code below:</p>
        <div style='text-align:center;margin:32px 0;'>
            <span style='font-size:36px;font-weight:bold;letter-spacing:8px;color:#2e7d32;'>{code}</span>
        </div>
        <p>Enter this code in the app to activate your account.</p>
        <p style='color:#999;font-size:12px;'>If you didn't create this account, you can safely ignore this email.</p>
    </div>";
		}
	}

}