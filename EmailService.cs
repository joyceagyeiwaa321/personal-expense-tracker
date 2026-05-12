using System;
using System.Windows;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace FinancyApplication
{
	public class EmailService
	{
		private readonly string senderEmail = "financyapplication@gmail.com";
		private readonly string senderName = "Financy App";
		private readonly string appPassword = "lqojabplujxchvex";

		private bool SendEmailInternal(string toEmail, string recipientName, string subject, string htmlBody)
		{
			try
			{
				var message = new MimeMessage();
				message.From.Add(new MailboxAddress(senderName, senderEmail));
				message.To.Add(new MailboxAddress(recipientName, toEmail));
				message.Subject = subject;

				message.Body = new TextPart("html") { Text = htmlBody };

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
				MessageBox.Show("Email error: " + ex.Message);
				return false;
			}
		}

		public bool SendResetToken(string toEmail, string recipientName, string token)
		{
			string body = BuildModernTemplate(recipientName, "PASSWORD RESET CODE", "We received a request to reset your password. Use the secure code below—it's valid for 15 minutes:", token, isAlert: true);
			return SendEmailInternal(toEmail, recipientName, "Securely Reset Your Financy Password", body);
		}

		public bool SendVerificationCode(string toEmail, string recipientName, string code)
		{
			string body = BuildModernTemplate(recipientName, "ACCOUNT VERIFICATION CODE", "Welcome aboard! To activate your account, enter the unique verification code below directly in the app:", code, isAlert: false);
			return SendEmailInternal(toEmail, recipientName, "Action Required: Verify Your Financy Account", body);
		}



		private string BuildModernTemplate(string name, string title, string message, string code, bool isAlert)
		{
			// Matches the signup screen's gradient palette (#004D40 → #00796B → #004D40, accent #00B894)
			string headerGradient = "linear-gradient(135deg, #004D40 0%, #00796B 50%, #004D40 100%)";
			string headerFallback = "#00796B";   // solid fallback for clients that ignore gradients
			string codeBoxBg = "#E0F2F1";        // pale teal, same family as in-app toast
			string codeColor = "#004D40";        // deep teal for the code itself
			string accentColor = "#00B894";      // signup CTA color, used on the divider

			string alertText = isAlert
				? "<p style='color:#d32f2f; font-size:12px; font-weight:bold; margin-top:20px; text-align:center;'>⚠️ SECURITY: If you did not request this, please secure your account by changing your password immediately.</p>"
				: "";

			return $@"
    <div style='background-color:#fafafa; padding:40px 0;'>
        <div style='font-family:""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif; max-width:500px; margin:0 auto; border:1px solid #e0e0e0; border-radius:16px; overflow:hidden; box-shadow: 0 10px 25px rgba(0,0,0,0.06);'>

            <div style='background:{headerFallback}; background:{headerGradient}; padding:30px; text-align:center;'>
                <h1 style='color:white; margin:0; font-size:24px; letter-spacing:3px; font-weight:bold;'>FINANCY</h1>
                <p style='color:#B2DFDB; margin:6px 0 0 0; font-size:12px; letter-spacing:1px;'>Money Made Simple.</p>
            </div>

            <div style='padding:40px; line-height:1.7; color:#333; background-color:white;'>
                <p style='font-size:17px;'>Hi <b>{name}</b>,</p>
                <p style='font-size:15px; color:#555;'>{message}</p>

                <div style='text-align:center; margin:40px 0; background-color:{codeBoxBg}; padding:30px; border-radius:12px; border:1px solid #B2DFDB;'>
                    <p style='text-transform:uppercase; font-size:11px; letter-spacing:2px; color:#00695C; margin:0 0 10px 0;'>{title}</p>
                    <span style='font-size:42px; font-weight:bold; letter-spacing:8px; color:{codeColor}; font-family:monospace;'>{code}</span>
                </div>

                {alertText}

                <hr style='border:none; border-top:2px solid {accentColor}; margin:30px 0; opacity:0.4;'>
                <p style='color:#999; font-size:12px; text-align:center;'>This is an automated message from the Financy Security System.</p>
            </div>
        </div>
    </div>";
		}
	}

}