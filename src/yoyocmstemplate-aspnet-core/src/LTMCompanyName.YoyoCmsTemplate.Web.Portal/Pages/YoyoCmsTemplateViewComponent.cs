﻿using Abp.AspNetCore.Mvc.ViewComponents;

namespace LTMCompanyName.YoyoCmsTemplate.Web.Portal.Pages
{
    public abstract class YoyoCmsTemplateViewComponent : AbpViewComponent
    {
        protected YoyoCmsTemplateViewComponent()
        {
            LocalizationSourceName = AppConsts.LocalizationSourceName;
        }
    }
}
