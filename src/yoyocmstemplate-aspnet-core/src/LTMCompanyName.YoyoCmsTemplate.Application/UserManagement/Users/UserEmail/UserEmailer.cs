using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Abp.Configuration;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Extensions;
using Abp.Localization;
using Abp.Net.Mail;
using Abp.Runtime.Security;
using Abp.UI;
using LTMCompanyName.YoyoCmsTemplate.Editions;
using LTMCompanyName.YoyoCmsTemplate.Emailing;
using LTMCompanyName.YoyoCmsTemplate.MultiTenancy;
using LTMCompanyName.YoyoCmsTemplate.UserManagerment.Users;

namespace LTMCompanyName.YoyoCmsTemplate.UserManagement.Users.UserEmail
{
    /// <summary>
    /// Used to send email to users.
    /// </summary>
    public class UserEmailer : YoyoCmsTemplateAppServiceBase, IUserEmailer
    {
        private readonly IEmailTemplateProvider _emailTemplateProvider;
        private readonly IEmailSender _emailSender;
        private readonly IRepository<Tenant> _tenantRepository;
        private readonly ICurrentUnitOfWorkProvider _unitOfWorkProvider;
        private readonly IUnitOfWorkManager _unitOfWorkManager;
        private readonly EditionManager _editionManager;
        private readonly UserManager _userManager;
        private readonly ISettingManager _settingManager;


        // used for styling action links on email messages.
        private string _emailButtonStyle =
            "padding-left: 30px; padding-right: 30px; padding-top: 12px; padding-bottom: 12px; color: #ffffff; background-color: #00bb77; font-size: 14pt; text-decoration: none;";
        private string _emailButtonColor = "#00bb77";

        public UserEmailer(
            IEmailTemplateProvider emailTemplateProvider,
            IEmailSender emailSender,
            IRepository<Tenant> tenantRepository,
            ICurrentUnitOfWorkProvider unitOfWorkProvider,
            IUnitOfWorkManager unitOfWorkManager,

            EditionManager editionManager,
            UserManager userManager, ISettingManager settingManager)
        {
            _emailTemplateProvider = emailTemplateProvider;
            _emailSender = emailSender;
            _tenantRepository = tenantRepository;
            _unitOfWorkProvider = unitOfWorkProvider;
            _unitOfWorkManager = unitOfWorkManager;

            _editionManager = editionManager;
            _userManager = userManager;
            _settingManager = settingManager;
        }

        /// <summary>
        /// Send email activation link to user's email address.
        /// </summary>
        /// <param name="user">User</param>
        /// <param name="link">Email activation link</param>
        /// <param name="plainPassword">
        /// Can be set to user's plain password to include it in the email.
        /// </param>
        [UnitOfWork]
        public virtual async Task SendEmailActivationLinkAsync(User user, string link, string plainPassword = null)
        {
            if (user.EmailConfirmationCode.IsNullOrEmpty())
            {
                throw new Exception("EmailConfirmationCode should be set in order to send email activation link.");
            }

            link = link.Replace("{userId}", user.Id.ToString());
            link = link.Replace("{confirmationCode}", Uri.EscapeDataString(user.EmailConfirmationCode));

            if (user.TenantId.HasValue)
            {
                link = link.Replace("{tenantId}", user.TenantId.ToString());
            }

            link = EncryptQueryParameters(link);

            var tenancyName = GetTenancyNameOrNull(user.TenantId);
            var emailTemplate = GetTitleAndSubTitle(user.TenantId, L("EmailActivation_Title"), L("EmailActivation_SubTitle"));
            var mailMessage = new StringBuilder();

            mailMessage.AppendLine("<b>" + L("NameSurname") + "</b>: " + user.Name + " " + user.Surname + "<br />");

            if (!tenancyName.IsNullOrEmpty())
            {
                mailMessage.AppendLine("<b>" + L("TenancyName") + "</b>: " + tenancyName + "<br />");
            }

            mailMessage.AppendLine("<b>" + L("UserName") + "</b>: " + user.UserName + "<br />");

            if (!plainPassword.IsNullOrEmpty())
            {
                mailMessage.AppendLine("<b>" + L("Password") + "</b>: " + plainPassword + "<br />");
            }

            mailMessage.AppendLine("<br />");
            mailMessage.AppendLine(L("EmailActivation_ClickTheLinkBelowToVerifyYourEmail") + "<br /><br />");
            mailMessage.AppendLine("<a style=\"" + _emailButtonStyle + "\" bg-color=\"" + _emailButtonColor + "\" href=\"" + link + "\">" + L("Verify") + "</a>");
            mailMessage.AppendLine("<br />");
            mailMessage.AppendLine("<br />");
            mailMessage.AppendLine("<br />");
            mailMessage.AppendLine("<span style=\"font-size: 9pt;\">" + L("EmailMessage_CopyTheLinkBelowToYourBrowser") + "</span><br />");
            mailMessage.AppendLine("<span style=\"font-size: 8pt;\">" + link + "</span>");

            await ReplaceBodyAndSend(user.EmailAddress, L("EmailActivation_Subject"), emailTemplate, mailMessage);
        }

        /// <summary>
        /// Sends a password reset link to user's email.
        /// </summary>
        /// <param name="user">User</param>
        /// <param name="link">Reset link</param>
        public async Task SendPasswordResetLinkAsync(User user, string link = null)
        {
            if (user.PasswordResetCode.IsNullOrEmpty())
            {
                throw new UserFriendlyException("密码重置码应设置，以便发送密码重置链接。");
            }

            var tenancyName = GetTenancyNameOrNull(user.TenantId);
            var emailTemplate = GetTitleAndSubTitle(user.TenantId, L("PasswordResetEmail_Title"), L("PasswordResetEmail_SubTitle"));


            var mailMessage = new StringBuilder();

            //mailMessage.AppendLine("<b>" + L("NameSurname") + "</b>: " + user.Name + " " + user.Surname + "<br />");

            if (!tenancyName.IsNullOrEmpty())
            {
                mailMessage.AppendLine("<b>" + L("TenancyName") + "</b>: " + tenancyName + "<br />");
            }

            mailMessage.AppendLine("<b>" + L("UserName") + "</b>: " + user.UserName + "<br />");
            mailMessage.AppendLine("<b>" + L("ResetCode") + "</b>: " + user.PasswordResetCode + "<br />");

            if (!link.IsNullOrEmpty())
            {
                link = link.Replace("{userId}", user.Id.ToString());
                link = link.Replace("{resetCode}", Uri.EscapeDataString(user.PasswordResetCode));

                if (user.TenantId.HasValue)
                {
                    link = link.Replace("{tenantId}", user.TenantId.ToString());
                }
                //对字符串进行转义
                link = EncryptQueryParameters(link);

                mailMessage.AppendLine("<br />");
                mailMessage.AppendLine(L("PasswordResetEmail_ClickTheLinkBelowToResetYourPassword") + "<br /><br />");
                mailMessage.AppendLine("<a style=\"" + _emailButtonStyle + "\" bg-color=\"" + _emailButtonColor + "\" href=\"" + link + "\">" + L("Reset") + "</a>");
                mailMessage.AppendLine("<br />");
                mailMessage.AppendLine("<br />");
                mailMessage.AppendLine("<br />");
                mailMessage.AppendLine("<span style=\"font-size: 9pt;\">" + L("EmailMessage_CopyTheLinkBelowToYourBrowser") + "</span><br />");
                mailMessage.AppendLine("<span style=\"font-size: 8pt;\">" + link + "</span>");
            }

            await ReplaceBodyAndSend(user.EmailAddress, L("PasswordResetEmail_Subject"), emailTemplate, mailMessage);
        }

        public async Task SendEmailAddressConfirmCode(string emailAddress, string code)
        {



            var body = $"您好:{emailAddress},欢迎注册52ABP，请将验证码填写到注册页面,</br>验证码：{code}";
            var emailTemplate = GetTitleAndSubTitle(AbpSession.TenantId, "注册52ABP邮箱验证", "邮箱验证");
            var mailMessage = new StringBuilder();
            mailMessage.Append(body);


            await ReplaceBodyAndSend(emailAddress, "注册52ABP邮箱验证", emailTemplate, mailMessage);

        }


        public async Task TryToSendSubscriptionExpireEmail(int tenantId, DateTime utcNow)
        {
            try
            {
                using (_unitOfWorkManager.Begin())
                {
                    using (_unitOfWorkManager.Current.SetTenantId(tenantId))
                    {
                        var tenantAdmin = await _userManager.GetAdminAsync();
                        if (tenantAdmin == null || string.IsNullOrEmpty(tenantAdmin.EmailAddress))
                        {
                            return;
                        }

                        var hostAdminLanguage = _settingManager.GetSettingValueForUser(LocalizationSettingNames.DefaultLanguage, tenantAdmin.TenantId, tenantAdmin.Id);
                        //    var culture = CultureHelper.GetCultureInfoByChecking(hostAdminLanguage);
                        var emailTemplate = GetTitleAndSubTitle(tenantId, L("SubscriptionExpire_Title"), L("SubscriptionExpire_SubTitle"));
                        var mailMessage = new StringBuilder();

                        //   mailMessage.AppendLine("<b>" + L("Message") + "</b>: " + L("SubscriptionExpire_Email_Body", culture, utcNow.ToString("yyyy-MM-dd") + " UTC") + "<br />");
                        mailMessage.AppendLine("<br />");

                        await ReplaceBodyAndSend(tenantAdmin.EmailAddress, L("SubscriptionExpire_Email_Subject"), emailTemplate, mailMessage);
                    }
                }
            }
            catch (Exception exception)
            {
                Logger.Error(exception.Message, exception);
            }
        }

        public async Task TryToSendSubscriptionAssignedToAnotherEmail(int tenantId, DateTime utcNow, int expiringEditionId)
        {
            try
            {
                using (_unitOfWorkManager.Begin())
                {
                    using (_unitOfWorkManager.Current.SetTenantId(tenantId))
                    {
                        var tenantAdmin = await _userManager.GetAdminAsync();
                        if (tenantAdmin == null || string.IsNullOrEmpty(tenantAdmin.EmailAddress))
                        {
                            return;
                        }

                        //   var hostAdminLanguage = _settingManager.GetSettingValueForUser(LocalizationSettingNames.DefaultLanguage, tenantAdmin.TenantId, tenantAdmin.Id);
                        //   var culture = CultureHelper.GetCultureInfoByChecking(hostAdminLanguage);
                        var expringEdition = await _editionManager.GetByIdAsync(expiringEditionId);
                        var emailTemplate = GetTitleAndSubTitle(tenantId, L("SubscriptionExpire_Title"), L("SubscriptionExpire_SubTitle"));
                        var mailMessage = new StringBuilder();

                        //   mailMessage.AppendLine("<b>" + L("Message") + "</b>: " + L("SubscriptionAssignedToAnother_Email_Body", culture, expringEdition.DisplayName, utcNow.ToString("yyyy-MM-dd") + " UTC") + "<br />");
                        mailMessage.AppendLine("<br />");

                        await ReplaceBodyAndSend(tenantAdmin.EmailAddress, L("SubscriptionExpire_Email_Subject"), emailTemplate, mailMessage);
                    }
                }
            }
            catch (Exception exception)
            {
                Logger.Error(exception.Message, exception);
            }
        }

        public async Task TryToSendFailedSubscriptionTerminationsEmail(List<string> failedTenancyNames, DateTime utcNow)
        {
            try
            {
                var hostAdmin = await _userManager.GetAdminAsync();
                if (hostAdmin == null || string.IsNullOrEmpty(hostAdmin.EmailAddress))
                {
                    return;
                }

                //  var hostAdminLanguage = _settingManager.GetSettingValueForUser(LocalizationSettingNames.DefaultLanguage, hostAdmin.TenantId, hostAdmin.Id);
                //    var culture = CultureHelper.GetCultureInfoByChecking(hostAdminLanguage);
                var emailTemplate = GetTitleAndSubTitle(null, L("FailedSubscriptionTerminations_Title"), L("FailedSubscriptionTerminations_SubTitle"));
                var mailMessage = new StringBuilder();

                //    mailMessage.AppendLine("<b>" + L("Message") + "</b>: " + L("FailedSubscriptionTerminations_Email_Body", culture, string.Join(",", failedTenancyNames), utcNow.ToString("yyyy-MM-dd") + " UTC") + "<br />");
                mailMessage.AppendLine("<br />");

                await ReplaceBodyAndSend(hostAdmin.EmailAddress, L("FailedSubscriptionTerminations_Email_Subject"), emailTemplate, mailMessage);
            }
            catch (Exception exception)
            {
                Logger.Error(exception.Message, exception);
            }
        }

        public async Task TryToSendSubscriptionExpiringSoonEmail(int tenantId, DateTime dateToCheckRemainingDayCount)
        {
            try
            {
                using (_unitOfWorkManager.Begin())
                {
                    using (_unitOfWorkManager.Current.SetTenantId(tenantId))
                    {
                        var tenantAdmin = await _userManager.GetAdminAsync();
                        if (tenantAdmin == null || string.IsNullOrEmpty(tenantAdmin.EmailAddress))
                        {
                            return;
                        }

                        //   var tenantAdminLanguage = _settingManager.GetSettingValueForUser(LocalizationSettingNames.DefaultLanguage, tenantAdmin.TenantId, tenantAdmin.Id);
                        //  var culture = CultureHelper.GetCultureInfoByChecking(tenantAdminLanguage);

                        var emailTemplate = GetTitleAndSubTitle(null, L("SubscriptionExpiringSoon_Title"), L("SubscriptionExpiringSoon_SubTitle"));
                        var mailMessage = new StringBuilder();

                        //   mailMessage.AppendLine("<b>" + L("Message") + "</b>: " + L("SubscriptionExpiringSoon_Email_Body", culture, dateToCheckRemainingDayCount.ToString("yyyy-MM-dd") + " UTC") + "<br />");
                        mailMessage.AppendLine("<br />");

                        await ReplaceBodyAndSend(tenantAdmin.EmailAddress, L("SubscriptionExpiringSoon_Email_Subject"), emailTemplate, mailMessage);
                    }
                }
            }
            catch (Exception exception)
            {
                Logger.Error(exception.Message, exception);
            }
        }

        private string GetTenancyNameOrNull(int? tenantId)
        {
            if (tenantId == null)
            {
                return null;
            }

            using (_unitOfWorkProvider.Current.SetTenantId(null))
            {
                return _tenantRepository.Get(tenantId.Value).TenancyName;
            }
        }

        private StringBuilder GetTitleAndSubTitle(int? tenantId, string title, string subTitle)
        {
            var emailTemplate = new StringBuilder(_emailTemplateProvider.GetDefaultTemplate(tenantId));
            emailTemplate.Replace("{EMAIL_TITLE}", title);
            emailTemplate.Replace("{EMAIL_SUB_TITLE}", subTitle);

            return emailTemplate;
        }

        private async Task ReplaceBodyAndSend(string emailAddress, string subject, StringBuilder emailTemplate, StringBuilder mailMessage)
        {
            emailTemplate.Replace("{EMAIL_BODY}", mailMessage.ToString());
            await _emailSender.SendAsync(new MailMessage
            {
                SubjectEncoding = Encoding.UTF8,
                BodyEncoding = Encoding.UTF8,
                To = { emailAddress },
                Subject = subject,
                Body = emailTemplate.ToString(),
                IsBodyHtml = true
            });
        }

        /// <summary>
        ///返回带有加密参数的链接
        /// </summary>
        /// <param name="link"></param>
        /// <param name="encrptedParameterName"></param>
        /// <returns></returns>
        private string EncryptQueryParameters(string link, string encrptedParameterName = "c")
        {
            if (!link.Contains("?"))
            {
                return link;
            }

            var uri = new Uri(link);
            var basePath = link.Substring(0, link.IndexOf('?'));
            var query = uri.Query.TrimStart('?');

            return basePath + "?" + encrptedParameterName + "=" + HttpUtility.UrlEncode(SimpleStringCipher.Instance.Encrypt(query));
        }
    }
}
