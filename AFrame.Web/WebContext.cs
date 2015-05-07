﻿using AFrame.Core;
using AFrame.Web.Controls;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AFrame.Web
{
    public class WebContext : Context
    {
        public IWebDriver Driver { get; private set; }

        /// <summary>
        /// If it is known that jQuery will always exist on the site then make
        /// this false. It will remove an additional check. 
        /// 
        /// Default: true
        /// </summary>
        public bool CheckjQueryExists { get; set; }

        public WebContext(IWebDriver driver)
            : this(driver, null, null)
        { }

        public WebContext(IWebDriver driver, IContext parentContext, SearchPropertyStack searchParameters)
            : base(parentContext, searchParameters)
        {
            this.Driver = driver;
            this.CheckjQueryExists = true;
        }

        public override void Dispose()
        {
            this.Driver.Quit();
        }

        public T NavigateTo<T>(string url) where T : WebControl
        {
            this.Driver.Url = url;
            this.Driver.Navigate();
            return this.As<T>();
        }

        public WebControl NavigateTo(string url)
        {
            return this.NavigateTo<WebControl>(url);
        }

        public object ExecuteScript(string script, params object[] args)
        {
            var javaScriptExecutor = (IJavaScriptExecutor)this.Driver;

            if(this.CheckjQueryExists)
            {
                var timeout = TimeSpan.FromSeconds(60);
                var timeoutThreshold = DateTime.UtcNow.Add(timeout);

                var isJQueryUndefined = new Func<bool>(() => (bool)javaScriptExecutor.ExecuteScript("return (typeof $ === 'undefined')"));
                if (isJQueryUndefined())
                {
                    javaScriptExecutor.ExecuteScript(@"
                        var scheme =  window.location.protocol;
                        if(scheme != 'https:')
                            scheme = 'http:';

                        var script = document.createElement('script');
                        script.type = 'text/javascript';
                        script.src = scheme + '//code.jquery.com/jquery-1.10.1.min.js'; 
                        document.getElementsByTagName('head')[0].appendChild(script);
                    ");

                    while (isJQueryUndefined())
                    {
                        System.Threading.Thread.Sleep(200);

                        //Don't test forever.. bomb out after a bit.
                        if (DateTime.UtcNow > timeoutThreshold)
                            throw new TimeoutException(string.Format("Checking jQuery exists timed out after {0} seconds.", timeout.TotalSeconds));
                    }
                }
            }

            return javaScriptExecutor.ExecuteScript(script, args);
        }
    }
}