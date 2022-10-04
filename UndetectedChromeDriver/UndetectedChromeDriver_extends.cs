using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using UndetectedChromeDriver.Models.Network.Proxy;

namespace SeleniumUndetectedChromeDriver
{
    public partial class UndetectedChromeDriver
    {

        public static readonly List<UndetectedChromeDriver> RunningWebBrowsers = new List<UndetectedChromeDriver>();

        public static void DisposeAll()
        {
            List<Task> runningTasks = new List<Task>();
            foreach (var browser in RunningWebBrowsers.ToArray())
            {
                if (browser.IsDisposed == false)
                {
                    runningTasks.Add(Task.Run(() =>
                    {
                        try { browser.KillItSelf(); } catch { }
                    }));
                }
            }

            Task.WaitAll(runningTasks.ToArray());
        }

        //=====================================================================

        public int ProcessId
        {
            get { return _service.ProcessId; }
        }

        public Size Size
        {
            get { return Manage().Window.Size; }
            set { Manage().Window.Size = value; }
        }

        public bool IsDisposed { get; private set; } = false;

        //=================================  CREATION  =====================================
        public static UndetectedChromeDriver Create(bool hidden, bool isLoadImages = false, string userDataDir = null, string userAgent = null,
            ProxyObject proxy = null, int width = -1, int height = -1, string[] extensions = null, params string[] args)
        {
            ChromeOptions options = new ChromeOptions();
            Dictionary<string, object> prefs = new Dictionary<string, object>();

            options.AddArgument("--disable-infobars");
            options.AddArgument("--disable-notifications");
            options.AddArgument("--disable-popup-blocking");
            options.AddArgument("--ignore-certificate-errors");
            options.AddArguments("--disable-blink-features=AutomationControlled");
            options.AddExcludedArgument("enable-automation");
            options.AddAdditionalOption("useAutomationExtension", false);

            if (string.IsNullOrWhiteSpace(userAgent) == false)
                options.AddArgument($"--user-agent={userAgent}");

            if (width > 0 && height > 0)
                options.AddArgument($"--window-size={width},{height}");

            if (proxy != null)
            {
                var proxyOption = new Proxy();
                proxyOption.Kind = ProxyKind.Manual;
                proxyOption.IsAutoDetect = false;
                proxyOption.HttpProxy =
                proxyOption.SslProxy = $"{proxy.Host}:{proxy.Port}";
                options.Proxy = proxyOption;
            }

            // Personal arguments
            foreach (var arg in args)
                options.AddArgument(arg);

            if (isLoadImages == false)
                prefs.Add("profile.default_content_setting_values.images", 2);

            if (extensions != null)
            {
                foreach (var extension in extensions)
                    options.AddExtension(extension);
            }

            var driver = Create(options: options,
                userDataDir: userDataDir,
                headless: hidden,
                hideCommandPromptWindow: true,
                prefs: prefs);

            RunningWebBrowsers.Add(driver);

            return driver;
        }

        //======================================================================

        /// <summary>
        /// Refresh (F5)
        /// </summary>
        public void Refresh()
        {
            Navigate().Refresh();
        }

        /// <summary>
        /// Cancel page loading
        /// </summary>
        public void CancelPageLoad()
        {
            try
            {
                FindElement(By.TagName("body")).SendKeys(Keys.Escape);
            }
            catch { }
        }

        #region TAB - WINDOW - FRAME - ALERT

        /// <summary>
        /// Open new blank tab
        /// </summary>
        /// <param name="url"></param>
        public void NewTab(string url = null)
        {
            this.ExecuteScript($"window.open('{url}');");
        }

        /// <summary>
        /// Oprn special url in new tab
        /// </summary>
        /// <param name="url"></param>
        public void OpenLinkInNewTab(string url)
        {
            this.NewTab(url);
        }

        /// <summary>
        /// Active special tab by index
        /// </summary>
        /// <param name="index"></param>
        public void SwitchToTabIndex(int index)
        {
            SwitchToWindowHandle(WindowHandles[index]);
        }

        /// <summary>
        /// Active last tab
        /// </summary>
        public void SwitchToLastTab()
        {
            SwitchToWindowHandle(WindowHandles.Last());
        }

        /// <summary>
        /// Active special tab by handle
        /// </summary>
        /// <param name="handle"></param>
        public void SwitchToWindowHandle(string handle)
        {
            SwitchTo().Window(handle);
        }

        /// <summary>
        /// Swicth controler to special frame
        /// </summary>
        /// <param name="frame"></param>
        public void SwitchToFrame(IWebElement frame)
        {
            SwitchTo().Frame(frame);
        }

        /// <summary>
        /// Accept an alert
        /// </summary>
        /// <returns></returns>
        public bool AcceptAlert()
        {
            try
            {
                var alert = SwitchTo().Alert();
                if (alert != null)
                {
                    alert.Accept();
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// Switch contrller to activated element
        /// </summary>
        /// <returns></returns>
        public IWebElement SwitchToActiveElement()
        {
            return this.SwitchTo().ActiveElement();
        }

        #endregion TAB - WINDOW - FRAME - ALERT


        #region Customize Selenium selection

        /// <summary>
        /// Wait until a special element matched selector (Include hidden elements)
        /// </summary>
        /// <param name="by">The selector</param>
        /// <param name="timeout">The timeout in miliseconds. Default 60000</param>
        /// <returns></returns>
        public IWebElement WaitUntilElementExists(By by, int timeout = 60000)
        {
            try
            {
                var wait = new WebDriverWait(this, TimeSpan.FromMilliseconds(timeout));
                return wait.Until(
                    (d) => {
                        try
                        {
                            var element = d.FindElement(by);
                            if (element == null)
                                Sleep(10);

                            return element;
                        }
                        catch
                        {
                            return null;
                        }
                    });
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// Wait until a special element matched selector and matched special matcher (Include hidden elements)
        /// </summary>
        /// <param name="by">The selector</param>
        /// <param name="match">The matcher</param>
        /// <param name="timeout">The timeout in miliseconds. Default 60000</param>
        /// <returns></returns>
        public IWebElement WaitUntilElementExists(By by, Func<IWebElement, bool> match, int timeout = 60000)
        {
            try
            {
                var wait = new WebDriverWait(this, TimeSpan.FromMilliseconds(timeout));
                return wait.Until(
                    (d) => {
                        try
                        {
                            var element = this.FindElements(by).FirstOrDefault(elem => match(elem) == true);
                            if (element == null)
                                Sleep(10);

                            return element;
                        }
                        catch
                        {
                            return null;
                        }
                    });
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// Wait until a special element matched one of selectors (Include hidden elements)
        /// </summary>
        /// <param name="bys">The selectors</param>
        /// <param name="timeout">The timeout in miliseconds. Default 60000</param>
        /// <returns></returns>
        public IWebElement WaitUntilOneOfElementsExists(IEnumerable<By> bys, int timeout = 60000)
        {
            try
            {
                var wait = new WebDriverWait(this, TimeSpan.FromMilliseconds(timeout));
                return wait.Until(
                    (d) => {
                        try
                        {
                            IWebElement element = null;
                            foreach (var by in bys)
                            {
                                try
                                {
                                    element = d.FindElement(by);
                                    if (element != null)
                                        break;
                                }
                                catch { }
                            }
                            if (element == null)
                                Sleep(10);

                            return element;
                        }
                        catch
                        {
                            return null;
                        }
                    });
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// Wait until special elements matched selector (Include hidden elements)
        /// </summary>
        /// <param name="by">The selector</param>
        /// <param name="timeout">The timeout in miliseconds. Default 60000</param>
        /// <returns></returns>
        public IReadOnlyCollection<IWebElement> WaitUntilElementsExists(By by, int timeout = 60000)
        {
            try
            {
                var wait = new WebDriverWait(this, TimeSpan.FromMilliseconds(timeout));
                return wait.Until(
                    (d) => {
                        try
                        {
                            var elems = d.FindElements(by);
                            if (elems == null || elems.Count == 0)
                                Sleep(10);
                            return elems;
                        }
                        catch
                        {
                            return null;
                        }
                    });
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// Wait until special elements matched selector (Include hidden elements)
        /// </summary>
        /// <param name="by">The selector</param>
        /// <param name="match">The matcher</param>
        /// <param name="timeout">The timeout in miliseconds. Default 60000</param>
        /// <returns></returns>
        public List<IWebElement> WaitUntilElementsExists(By by, Func<IWebElement, bool> match, int timeout = 60000)
        {
            try
            {
                var wait = new WebDriverWait(this, TimeSpan.FromMilliseconds(timeout));
                return wait.Until(
                    (d) => {
                        try
                        {
                            var elems = d.FindElements(by).Where(elem => match(elem) == true).ToList();
                            if (elems == null || elems.Count == 0)
                                Sleep(10);
                            return elems;
                        }
                        catch
                        {
                            return null;
                        }
                    });
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// Wait until NO element matched selector (Include hidden elements)
        /// </summary>
        /// <param name="by">The selector</param>
        /// <param name="timeout">The timeout in miliseconds. Default 60000</param>
        /// <returns></returns>
        public bool WaitUntilElementNotExists(By by, int timeout = 60000)
        {
            try
            {
                var wait = new WebDriverWait(this, TimeSpan.FromMilliseconds(timeout));
                return wait.Until(
                    (d) => {
                        try
                        {
                            var elem = d.FindElement(by);
                            Sleep(10);
                            return false;
                        }
                        catch
                        {
                            return true;
                        }
                    });
            }
            catch (Exception ex)
            {
                return false;
            }
        }


        /// <summary>
        /// Wait until a special element matched selector displayed.
        /// </summary>
        /// <param name="by">The selector</param>
        /// <param name="timeout">The timeout in miliseconds. Default 60000</param>
        /// <returns></returns>
        public IWebElement WaitUntilElementDisplayed(By by, int timeout = 60000)
        {
            return this.WaitUntilElementExists(by, match: elem => elem.Displayed == true, timeout: timeout);
        }

        /// <summary>
        /// Wait until a special element matched selector displayed and matched special matcher.
        /// </summary>
        /// <param name="by">The selector</param>
        /// <param name="match">The matcher</param>
        /// <param name="timeout">The timeout in miliseconds. Default 60000</param>
        /// <returns></returns>
        public IWebElement WaitUntilElementDisplayed(By by, Func<IWebElement, bool> match, int timeout = 60000)
        {
            return this.WaitUntilElementExists(by, match: elem => elem.Displayed == true && match(elem) == true, timeout: timeout);
        }

        /// <summary>
        /// Wait until special elements matched selector displayed.
        /// </summary>
        /// <param name="by">The selector</param>
        /// <param name="timeout">The timeout in miliseconds. Default 60000</param>
        /// <returns></returns>
        public List<IWebElement> WaitUntilElementsDisplayed(By by, int timeout = 60000)
        {
            try
            {
                var wait = new WebDriverWait(this, TimeSpan.FromMilliseconds(timeout));
                return wait.Until(
                    (d) => {
                        try
                        {
                            var elems = d.FindElements(by);
                            if (elems != null && elems.Any(e => e.Displayed == true))
                            {
                                return elems.Where(e => e.Displayed == true).ToList();
                            }
                            else
                            {
                                Sleep(10);
                                return null;
                            }
                        }
                        catch
                        {
                            return null;
                        }
                    });
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// Wait until one of special elements matched selector displayed.
        /// </summary>
        /// <param name="bys">Array of selectors</param>
        /// <param name="timeout">The timeout in miliseconds. Default 60000</param>
        /// <returns></returns>
        public IWebElement WaitUntilOneOfElementsDisplayed(IEnumerable<By> bys, int timeout = 60000)
        {
            try
            {
                var wait = new WebDriverWait(this, TimeSpan.FromMilliseconds(timeout));
                return wait.Until(
                    (d) => {
                        try
                        {
                            IWebElement element = null;
                            foreach (var by in bys)
                            {
                                try
                                {
                                    element = d.FindElement(by);
                                    if (element != null && element.Displayed == true)
                                        break;
                                    else
                                        element = null;
                                }
                                catch { }
                            }
                            if (element == null)
                                Sleep(10);

                            return element;
                        }
                        catch
                        {
                            return null;
                        }
                    });
            }
            catch (Exception ex)
            {
                return null;
            }
        }


        /// <summary>
        /// Wait until one of special elements matched selector NO displayed.
        /// </summary>
        /// <param name="by">The selector</param>
        /// <param name="timeout">The timeout in miliseconds. Default 60000</param>
        /// <returns></returns>
        public bool WaitUntilElementNoDisplayed(By by, int timeout = 60000)
        {
            try
            {
                var wait = new WebDriverWait(this, TimeSpan.FromMilliseconds(timeout));
                return wait.Until(
                    (d) => {
                        try
                        {
                            var elem = d.FindElement(by);
                            if (elem.Displayed)
                            {
                                Sleep(10);
                                return false;
                            }
                            else
                                return true;
                        }
                        catch
                        {
                            return true;
                        }
                    });
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// Wait until special elements matched selector and matched special matcher NO displayed.
        /// </summary>
        /// <param name="by"></param>
        /// <param name="match">The matcher</param>
        /// <param name="timeout">The timeout in miliseconds. Default 60000</param>
        /// <returns></returns>
        public bool WaitUntilElementNoDisplayed(By by, Func<IWebElement, bool> match, int timeout = 60000)
        {
            try
            {
                IWebElement selectedElem = null;
                var watch = System.Diagnostics.Stopwatch.StartNew();
                while (timeout < 0 || watch.ElapsedMilliseconds < timeout)
                {
                    try
                    {
                        selectedElem = this.FindElements(by).FirstOrDefault(elem => elem.Displayed == true && match(elem) == true);
                        if (selectedElem == null)
                            return true;
                    }
                    catch
                    {
                        return true;
                    }
                    Sleep(10);
                }
                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// Wait until one of special elements matched selector enabled.
        /// </summary>
        /// <param name="by">The selector</param>
        /// <param name="timeout">The timeout in miliseconds. Default 60000</param>
        /// <returns></returns>
        public IWebElement WaitUntilElementEnabled(By by, int timeout = 60000)
        {
            try
            {
                var wait = new WebDriverWait(this, TimeSpan.FromMilliseconds(timeout));
                return wait.Until(
                    (d) => {
                        try
                        {
                            var elem = d.FindElement(by);
                            if (elem.Enabled)
                                return elem;
                            else
                            {
                                Sleep(10);
                                return null;
                            }
                        }
                        catch
                        {
                            return null;
                        }
                    });
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// Wait until browser's url changed
        /// </summary>
        /// <param name="timeout">The timeout in miliseconds. Default 60000</param>
        /// <returns></returns>
        public bool WaitUntilUrlChanged(int timeout = 60000)
        {
            string url = this.Url;
            var watch = System.Diagnostics.Stopwatch.StartNew();

            do
            {
                System.Threading.Thread.Sleep(100);
            }
            while (url.Equals(this.Url) &&
                watch.ElapsedMilliseconds < timeout);
            watch.Stop();

            return !url.Equals(this.Url);
        }

        /// <summary>
        /// Find a special element matched selector.
        /// </summary>
        /// <param name="by">The selector</param>
        /// <param name="match">The matcher</param>
        /// <returns></returns>
        public IWebElement FindElement(By by, Func<IWebElement, bool> match)
        {
            try
            {
                return this.FindElements(by).FirstOrDefault(elem => match(elem) == true);
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// Find special elements matched selector and special matching.
        /// </summary>
        /// <param name="by">The selector</param>
        /// <param name="matchingCallback"></param>
        /// <returns></returns>
        public ReadOnlyCollection<IWebElement> FindElements(By by, Func<IWebElement, bool> matchingCallback)
        {
            try
            {
                return this.FindElements(by).Where(elem => matchingCallback(elem) == true).ToList().AsReadOnly();
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// Find FIRST special elements matched any selector.
        /// </summary>
        /// <param name="bys">Array of selectors</param>
        /// <returns></returns>
        public IWebElement FindOneOfElement(By[] bys)
        {
            try
            {
                IWebElement element = null;
                foreach (var by in bys)
                {
                    try
                    {
                        element = this.FindElement(by);
                        if (element != null && element.Displayed == true)
                            break;
                        else
                            element = null;
                    }
                    catch { }
                }
                return element;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get the activating element.
        /// </summary>
        /// <returns></returns>
        public IWebElement GetActiveElement()
        {
            return this.SwitchTo().ActiveElement();
        }

        /// <summary>
        /// Remove the emlement from site.
        /// </summary>
        /// <param name="elem">Omg omg! He is killing me! Police please!</param>
        /// <returns></returns>
        public bool RemoveElement(IWebElement elem)
        {
            return this.ExecuteScript("return arguments[0].remove();", elem) != null;
        }

        /// <summary>
        /// Get the shadow root of special element.
        /// </summary>
        /// <param name="element">The special element.</param>
        /// <returns></returns>
        public ShadowRoot GetShadowRoot(IWebElement element)
        {
            ShadowRoot ele = (ShadowRoot)this.ExecuteScript("return arguments[0].shadowRoot", element);
            return ele;
        }

        /// <summary>
        /// Find a special element matched the selector and get its shadow.
        /// </summary>
        /// <param name="by">The selector</param>
        /// <returns></returns>
        public ShadowRoot GetShadowRoot(By by)
        {
            var webElement = FindElement(by);
            if (webElement != null)
            {
                return GetShadowRoot(webElement);
            }
            else
            {
                return null;
            }
        }

        #endregion Customize Selenium selection

        //======================================================================

        /// <summary>
        /// Scroll screen to special element position.
        /// </summary>
        /// <param name="element">The special element</param>
        /// <param name="smooth">Yeah! Do you want smooth? Default FALSE</param>
        public void ScrollToElement(IWebElement element, bool smooth = false)
        {
            this.ExecuteScript($"arguments[0].scrollIntoView({{behavior: \"{(smooth ? "smooth" : "auto")}\", block: \"start\"}});", element);
        }

        /// <summary>
        /// Scroll to end of screen. Sometimes, it isn't working :))
        /// </summary>
        public void ScrollToEnd()
        {
            var footer = this.FindElement(By.CssSelector("div[class*='element']"));
            if (footer != null)
                this.ExecuteScript($"window.scrollTo(0, document.body.scrollHeight);");
        }

        #region Fill datas

        /// <summary>
        /// Find a special elemnt and get its text.
        /// </summary>
        /// <param name="by">The selector</param>
        /// <returns></returns>
        public string GetText(By by)
        {
            var element = this.FindElement(by);
            return element?.Text;
        }

        /// <summary>
        /// Find a special element. Give it some texts.
        /// </summary>
        /// <param name="by">The selector</param>
        /// <param name="text">The text</param>
        /// <param name="stepBystep">Step step step. I'm typing by my fingers.</param>
        /// <param name="clearFirst">Clear texts first</param>
        /// <returns></returns>
        public bool FillText(By by, string text, bool stepBystep = false, bool clearFirst = true)
        {
            var webElement = FindElement(by);
            if (webElement != null)
            {
                return FillText(webElement, text, stepBystep, clearFirst);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Oh, I love this element. Give it some texts.
        /// </summary>
        /// <param name="element">Special element</param>
        /// <param name="text">The text</param>
        /// <param name="stepBystep">Step step step. I'm typing by my fingers.</param>
        /// <param name="clearFirst">Clear texts first</param>
        /// <returns></returns>
        public bool FillText(IWebElement element, string text, bool stepBystep = false, bool clearFirst = true)
        {
            if (element != null)
            {
                element.Click();
                Sleep(200);
                if (clearFirst)
                {
                    element.Clear();
                    Sleep(100);
                }

                if (stepBystep)
                    SendKeysStepByStep(element, text);
                else
                    element.SendKeys(text);

                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Find a special element, replace its inner-html
        /// </summary>
        /// <param name="by">The selector</param>
        /// <param name="html">String of html-code</param>
        /// <returns></returns>
        public bool SetInnerHtml(By by, string html)
        {
            var webElement = FindElement(by);
            if (webElement != null)
            {
                return SetInnerHtml(webElement, html);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Replace special element's inner-html
        /// </summary>
        /// <param name="element">Special element</param>
        /// <param name="text">The text</param>
        /// <returns></returns>
        public bool SetInnerHtml(IWebElement element, string text)
        {
            if (element != null)
            {
                this.ExecuteScript($"var ele=arguments[0]; ele.innerHTML = '{text}';", element);

                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Find a special element. Set its attribute value.
        /// </summary>
        /// <param name="by"></param>
        /// <param name="attr"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool SetAttribute(By by, string attr, string value)
        {
            var webElement = FindElement(by);
            if (webElement != null)
            {
                return SetAttribute(webElement, attr, value);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Set attribute value of special element.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="attr"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool SetAttribute(IWebElement element, string attr, string value)
        {
            if (element != null)
            {
                ((IJavaScriptExecutor)this).ExecuteScript($"var ele=arguments[0]; ele.setAttribute('{attr}', '{value}');", element);

                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="elementSelector"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool SelectOptionValue(By elementSelector, string value)
        {
            try
            {
                var webElement = FindElement(elementSelector);
                webElement.Click();
                Sleep();

                var selectElement = new SelectElement(webElement);
                selectElement.SelectByValue(value);
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion Fill datas


        //=====================================================================
        public bool ClickElement(By elementSelector, bool scrollToClick = true)
        {
            try
            {
                var webElement = FindElement(elementSelector);
                if (webElement == null)
                    return false;

                return ClickElement(webElement, scrollToClick);
            }
            catch
            {
                return false;
            }
        }

        public bool ClickElement(By elementSelector, Func<IWebElement, bool> matchingCallback, bool scrollToClick = true)
        {
            try
            {
                var webElement = FindElement(elementSelector, matchingCallback);
                if (webElement == null)
                    return false;

                return ClickElement(webElement, scrollToClick);
            }
            catch
            {
                return false;
            }
        }

        public bool ClickElement(IWebElement element, bool scrollToClick = true)
        {
            try
            {
                if (scrollToClick)
                    ScrollToElement(element);

                element.Click();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool WaitToClickElement(By elementSelector, bool scrollToClick = true, int timeout = 30000)
        {
            try
            {
                IWebElement webElement = this.WaitUntilElementExists(elementSelector, timeout);
                if (webElement == null)
                    return false;

                Sleep();

                ClickElement(webElement, scrollToClick);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool WaitToClickElement(By elementSelector, Func<IWebElement, bool> matchingCallback, bool scrollToClick = true, int timeout = 30000)
        {
            try
            {
                IWebElement webElement = this.WaitUntilElementExists(elementSelector, matchingCallback, timeout);
                if (webElement == null)
                    return false;

                Sleep();

                ClickElement(webElement, scrollToClick);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void ClickElementUsingJs(IWebElement element, bool scrollToClick = true)
        {
            if (scrollToClick)
                ScrollToElement(element);

            ((IJavaScriptExecutor)this).ExecuteScript("arguments[0].click();", element);
        }

        public void ClickElementUsingJs(By elementSelector, bool scrollToClick = true)
        {
            var webElement = FindElement(elementSelector);
            if (webElement == null || webElement.Displayed == false)
                return;

            ClickElementUsingJs(webElement, scrollToClick);
        }


        public bool SendMouseOver(By elementSelector)
        {
            try
            {
                var webElement = FindElement(elementSelector);
                if (webElement == null)
                    return false;

                SendMouseOver(webElement);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool SendMouseOver(IWebElement element)
        {
            try
            {
                Actions actions = new Actions(this);
                actions.MoveToElement(element);
                actions.Click().Build().Perform();
                return true;
            }
            catch
            {
                return false;
            }
        }


        public bool DragAndDrop(IWebElement element, IWebElement destinationElememt)
        {
            try
            {
                //Creating object of Actions class to build composite actions
                Actions builder = new Actions(this);

                //Building a drag and drop action
                var dragAndDrop = builder.ClickAndHold(element)
                .MoveToElement(destinationElememt)
                .Release(destinationElememt)
                .Build();

                //Performing the drag and drop action
                dragAndDrop.Perform();
                return true;
            }
            catch
            {
                return false;
            }
        }
        //=====================================================================

        public void SendKeys(string keys)
        {
            this.SwitchTo().ActiveElement().SendKeys(keys);
        }

        public void SendKeys(IWebElement element, string keys)
        {
            element.SendKeys(keys);
        }

        public void SendKeysStepByStep(IWebElement element, string keys)
        {
            Random rand = new Random();

            foreach (var key in keys)
            {
                element.SendKeys($"{key}");
                Sleep(rand.Next(10, 80));
            }
        }

        public void SendKeys(By elementSelector, string keys)
        {
            var webElement = FindElement(elementSelector);
            SendKeys(webElement, keys);
        }

        //=====================================================================

        private Random _sleepRand = new Random(20);
        public void Sleep(int time = -1)
        {
            if (time == -1)
                System.Threading.Thread.Sleep(_sleepRand.Next(321, 1234));
            else
                System.Threading.Thread.Sleep(time);
        }

        public void Sleep(int mintime, int maxtime)
        {
            System.Threading.Thread.Sleep(this._sleepRand.Next(mintime, maxtime));
        }

        public object Get(string url, params object[] inputs)
        {
            string script = $@"let xmlhttp = new XMLHttpRequest();
            xmlhttp.open('GET', '{url}', false);
            xmlhttp.send();
            return xmlhttp.response;";

            return ExecuteScript(script, inputs);
        }

        public object Post(string url, Dictionary<string, string> headers, params object[] inputs)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"let xhr = new XMLHttpRequest();");
            sb.Append($"xhr.open('POST', '{url}', false);");

            foreach (var key in headers.Keys)
                sb.Append($"xhr.setRequestHeader('{key}', '{headers[key]}');");

            sb.Append($"xhr.send(arguments[0]);");
            sb.Append($"return xhr.responseText;");

            return ExecuteScript(sb.ToString(), inputs);
        }

        public object Put(string url, Dictionary<string, string> headers, params object[] inputs)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"let xhr = new XMLHttpRequest();");
            sb.Append($"xhr.open('PUT', '{url}', false);");

            foreach (var key in headers.Keys)
                sb.Append($"xhr.setRequestHeader('{key}', '{headers[key]}');");

            sb.Append($"xhr.send(arguments[0]);");
            sb.Append($"return xhr.responseText;");

            return ExecuteScript(sb.ToString(), inputs);
        }

        public string GetIp()
        {
            return this.Get("http://icanhazip.com").ToString().Trim();
        }
        #region cookies

        public ReadOnlyCollection<Cookie> GetCookies()
        {
            return this.Manage().Cookies.AllCookies;
        }

        public string GetCookiesString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (OpenQA.Selenium.Cookie c in this.Manage().Cookies.AllCookies)
            {
                sb.Append($"{c.Name}={c.Value};");
            }
            return sb.ToString();
        }

        public void SetCookiesString(string cookies)
        {
            cookies = cookies.Replace(" ", "");

            foreach (string cookie in cookies.Split(';'))
            {
                if (string.IsNullOrEmpty(cookie))
                    continue;

                string[] values = cookie.Split('=');
                if (values.Length > 1
                    && !string.IsNullOrEmpty(values[0])
                    && !string.IsNullOrEmpty(values[1]))
                {
                    Cookie c = new Cookie(name: values[0], value: values[1]);
                    this.Manage().Cookies.AddCookie(c);
                }
            }
        }

        public void ClearCookies()
        {
            this.Manage().Cookies.DeleteAllCookies();
        }

        #endregion cookies

        public void KillItSelf()
        {
            this.IsDisposed = true;
            RunningWebBrowsers.Remove(this);
            if (this.ProcessId > 0)
            {
                var cmd = $"taskkill /F /T /PID  {this.ProcessId}";
                Process process = new Process();
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.Arguments = "/c " + cmd;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
            }
        }

    }
}