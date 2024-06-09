using System;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace HsaWebModule.ProgramUtil
{
    public class MailServiceModule
    {
        string emailAdress = string.Empty;
        public MailServiceModule(string getMmailAddress, ref int RandomNumber) 
        {
            emailAdress = getMmailAddress;
            if (emailAdress != "") 
            {
                RandomNumber = SendEmail();
            }
        }
        public int SendEmail() 
        {
            int checkSum = 0;
            try
            {
                Random random = new Random();
                checkSum = random.Next(1000, 9000);

                string sendMaileAddress = "anhesu4@gmail.com";
                string sendMailPassword = "hlen mpxc szkd snps";

                MailMessage message = new MailMessage()
                {
                    From = new MailAddress(sendMaileAddress),
                    Subject = "일회용 코드",
                    SubjectEncoding = Encoding.UTF8,
                    Body = "안녕하세요 anhesu4@gmail.com 님,\r\n\r\nHsaWebModule에서 사용할 일회용 코드에 대한 요청을 받았습니다.\r\n\r\n일회용 코드: " + checkSum + "\r\n\r\n이 코드를 요청하지 않았다면 이 전자 메일을 무시해 주세요. 누군가 귀하의 전자 메일 주소를 잘못 입력한 것일 수 있습니다.\r\n\r\n감사합니다.\r\nHsaWebModule",
                    BodyEncoding = Encoding.UTF8,
                };
                message.To.Add(emailAdress);
                SmtpClient smtpClient = new SmtpClient()
                {
                    Port = 587,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Host = "smtp.gmail.com",
                    EnableSsl = true,
                    Credentials = new NetworkCredential(sendMaileAddress, sendMailPassword)
                };
                smtpClient.Send(message);
                Program.trayIcon.notifyIcon.BalloonTipTitle = "인증번호 발송";
                Program.trayIcon.notifyIcon.BalloonTipText = "인증번호가 발송되었습니다.";
                Program.trayIcon.notifyIcon.ShowBalloonTip(500);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return checkSum;
        }
    }
}
