using CrawData.DesignPattern;
using CrawData.Model.Etax;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Text.Json;
using CrawData.Utils.Clients;

namespace CrawData.BL
{
    public class BLETaxIndividual
    {
        #region Decleration
        private HttpClientHandler _handle;
        private HttpClient _client;
        private CookieContainer _cookieContainer;
        private string _baseUrl = "https://canhan.gdt.gov.vn/";
        private string _sessionId;
        private string _processorId;
        private string _taxCode;
        private string _password;
        #endregion

        #region method
        private void ResetSessionContext()
        {
            if (_handle != null)
            {
                _handle.Dispose();
            }

            if (_client != null)
            {
                _client.Dispose();
            }

            _cookieContainer = new CookieContainer();
            _handle = new HttpClientHandler() { CookieContainer = _cookieContainer };
            _client = new HttpClient(_handle) { BaseAddress = new Uri(_baseUrl) };
            SetDefaultHeadersGet();
        }

        private void SetDefaultHeadersGet()
        {
            _client.DefaultRequestHeaders.Clear();
            _client.Timeout = TimeSpan.FromSeconds(300);
            _client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
            _client.DefaultRequestHeaders.Add("Accept-Language", "vi-VN,vi;q=0.9");
            _client.DefaultRequestHeaders.Add("Connection", "keep-alive");
            _client.DefaultRequestHeaders.Add("Host", "canhan.gdt.gov.vn");
            _client.DefaultRequestHeaders.Add("sec-ch-ua", "\"Google Chrome\";v=\"117\", \"Not; A = Brand\";v=\"8\", \"Chromium\";v=\"117\"");
            _client.DefaultRequestHeaders.Add("sec-ch-ua-mobile", "?0");
            _client.DefaultRequestHeaders.Add("sec-ch-ua-platform", "Windows");
            _client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/117.0.0.0 Safari/537.36");
        }

        private void SetDefaultHeadersPost()
        {
            _client.DefaultRequestHeaders.Clear();
            _client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
            _client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
            _client.DefaultRequestHeaders.Add("Accept-Language", "vi-VN,vi;q=0.9,fr-FR;q=0.8,fr;q=0.7,en-US;q=0.6,en;q=0.5");
            _client.DefaultRequestHeaders.Add("Connection", "keep-alive");
            _client.DefaultRequestHeaders.Add("Host", "canhan.gdt.gov.vn");
            _client.DefaultRequestHeaders.Add("Referer", "https://canhan.gdt.gov.vn/ICanhan/Request");
            _client.DefaultRequestHeaders.Add("sec-ch-ua", "\"Google Chrome\";v=\"117\", \"Not; A = Brand\";v=\"8\", \"Chromium\";v=\"117\"");
            _client.DefaultRequestHeaders.Add("sec-ch-ua-mobile", "?0");
            _client.DefaultRequestHeaders.Add("sec-ch-ua-platform", "Windows");
            _client.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "document");
            _client.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "navigate");
            _client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-origin");
            _client.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
            _client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/117.0.0.0 Safari/537.36");
        }

        private async Task GotoJumpPage()
        {
            HttpResponseMessage resMes = null;
            try
            {
                resMes = await _client.GetAsync("/");
                resMes.EnsureSuccessStatusCode();
                var html = resMes.Content.ReadAsStringAsync().Result;
                string regexPattern = @"&dse\S+&dse";
                Regex regex = new Regex(regexPattern);
                Match match = regex.Match(html);
                if (match.Success)
                {
                    var sessionStr = match.ToString().Replace("&dse_", ";").Split(';');
                    for (int i = 0; i < sessionStr.Length; i++)
                    {
                        if (sessionStr[i].ToString().Contains("sessionId"))
                        {
                            var sessionId = sessionStr[i].ToString().Replace("sessionId=", "");
                            _sessionId = sessionId;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private async Task GotoIndexPage()
        {
            HttpResponseMessage resMes = null;
            try
            {
                var requestUri = $"/ICanhan/Request?dse_sessionId={_sessionId}";
                requestUri = string.Concat(requestUri, $"&dse_applicationId=-1&dse_pageId=11&dse_operationName=retailIndexProc");
                requestUri = string.Concat(requestUri, $"&dse_errorPage=error_page.jsp&dse_processorState=initial&dse_nextEventName=start");

                resMes = await _client.GetAsync(requestUri);
                resMes.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private async Task GotoLoginOption()
        {
            HttpResponseMessage resMes = null;
            try
            {
                var requestUri = $"/ICanhan/Request?dse_sessionId={_sessionId}";
                requestUri = string.Concat(requestUri, $"&dse_applicationId=-1&dse_pageId=17&dse_operationName=retailUserLoginProc");
                requestUri = string.Concat(requestUri, $"&dse_errorPage=error_page.jsp&dse_processorState=initial&dse_nextEventName=start");
                resMes = await _client.GetAsync(requestUri);
                resMes.EnsureSuccessStatusCode();
                string html = await resMes.Content.ReadAsStringAsync();
                string pattern = @"&dse_processorId=(.*?)&";
                foreach (Match match in Regex.Matches(html, pattern))
                {
                    if (match.Success && match.Groups.Count > 0)
                    {
                        _processorId = match.Groups[1].Value;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private async Task Click_SelectLoginOption()
        {
            HttpResponseMessage resMes = null;
            try
            {
                var requestUri = $"/ICanhan/Request?dse_sessionId={_sessionId}";
                requestUri = string.Concat(requestUri, $"&dse_applicationId=-1&dse_pageId=19&dse_operationName=retailUserLoginProc");
                requestUri = string.Concat(requestUri, $"&dse_processorId={_processorId}dse_processorState=loginOptions&dse_nextEventName=ok");
                resMes = await _client.GetAsync(requestUri);
                resMes.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        private async Task<string> Resolve_Captcha()
        {
            var capcha = "";
            _taxCode = "4201526463";
            _password = "Thienchi@2022";
            HttpResponseMessage resMes = null;
            try
            {
                resMes = await _client.GetAsync("ICanhan/servlet/ImageServlet");
                resMes.EnsureSuccessStatusCode();
                //var decapcha = new DecapchaUtil();
                //capcha = await decapcha.DeCapcha(await resMes.Content.ReadAsStreamAsync());
                var folderPath = $"{Directory.GetCurrentDirectory()}\\CapchaImg\\capcha.png";
                using (var fs = new FileStream(folderPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
                {
                    await resMes.Content.CopyToAsync(fs);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return capcha;
        }

        private async Task PostLogin_Step1()
        {
            HttpResponseMessage resMes = null;
            try
            {
                var capcha = await Resolve_Captcha();
                SetDefaultHeadersPost();
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("dse_sessionId", _sessionId),
                    new KeyValuePair<string, string>("dse_applicationId", "-1"),
                    new KeyValuePair<string, string>("dse_operationName", "retailUserLoginProc"),
                    new KeyValuePair<string, string>("dse_pageId", "20"),
                    new KeyValuePair<string, string>("dse_processorState", "checkloginpage"),
                    new KeyValuePair<string, string>("dse_processorId", _processorId),
                    new KeyValuePair<string, string>("dse_errorPage", "error_page.jsp"),
                    new KeyValuePair<string, string>("dse_nextEventName", "ok"),
                    new KeyValuePair<string, string>("_userName", _taxCode),
                    new KeyValuePair<string, string>("capcha", capcha)
                });
                resMes = await _client.PostAsync("/ICanhan/Request", content);
                resMes.EnsureSuccessStatusCode();

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private async Task PostLogin_Step2()
        {
            HttpResponseMessage resMes = null;
            try
            {
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("dse_sessionId", _sessionId),
                    new KeyValuePair<string, string>("dse_applicationId", "-1"),
                    new KeyValuePair<string, string>("dse_operationName", "retailUserLoginProc"),
                    new KeyValuePair<string, string>("dse_pageId", "9"),
                    new KeyValuePair<string, string>("dse_processorState", "viewloginpage"),
                    new KeyValuePair<string, string>("dse_processorId", _processorId),
                    new KeyValuePair<string, string>("dse_errorPage", "error_page.jsp"),
                    new KeyValuePair<string, string>("dse_nextEventName", "ok"),
                    new KeyValuePair<string, string>("_password", _password)
                });
                resMes = await _client.PostAsync("/ICanhan/Request", content);
                resMes.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private async Task GotoHomePage()
        {
            HttpResponseMessage resMes = null;
            try
            {
                var requestUri = $"/ICanhan/Request?dse_sessionId={_sessionId}";
                requestUri = string.Concat(requestUri, $"&dse_applicationId=-1&dse_pageId=5&dse_operationName=retailHomePageProc");
                requestUri = string.Concat(requestUri, $"&dse_processorState=initial&dse_nextEventName=start");
                resMes = await _client.GetAsync(requestUri);
                resMes.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private async Task SignInPage()
        {
            ResetSessionContext();
            await Task.Delay(1000);
            await GotoJumpPage();
            await Task.Delay(1000);
            await GotoIndexPage();
            await Task.Delay(1000);
            await GotoLoginOption();
            await Task.Delay(1000);
            await Click_SelectLoginOption();
            await Task.Delay(1000);
            await PostLogin_Step1();
            await Task.Delay(1000);
            await PostLogin_Step2();
            await Task.Delay(1000);
            await GotoHomePage();
        }

        private async Task GotoResearchETaxPage()
        {
            HttpResponseMessage resMes = null;
            try
            {
                var requestUri = $"/ICanhan/Request?dse_sessionId={_sessionId}";
                requestUri = string.Concat(requestUri, $"&dse_applicationId=-1&dse_pageId=5&dse_operationName=retailSearchVoucherProc");
                requestUri = string.Concat(requestUri, $"&dse_processorState=initial&dse_nextEventName=start");
                resMes = await _client.GetAsync(requestUri);
                resMes.EnsureSuccessStatusCode();

                var doc = new HtmlDocument();
                doc.LoadHtml(await resMes.Content.ReadAsStringAsync());
                var inputProcessorId = doc.DocumentNode.SelectNodes("//*[@name='dse_processorId']");
                if (inputProcessorId != null)
                {
                    _processorId = inputProcessorId.FirstOrDefault().Attributes["value"].Value;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private async Task<HtmlDocument> Click_ButtonTraCuu(string fromDate, string toDate)
        {
            var doc = new HtmlDocument();
            HttpResponseMessage resMes = null;
            try
            {
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("dse_sessionId", _sessionId),
                    new KeyValuePair<string, string>("dse_applicationId", "-1"),
                    new KeyValuePair<string, string>("dse_operationName", "retailSearchVoucherProc"),
                    new KeyValuePair<string, string>("dse_pageId", "10"),
                    new KeyValuePair<string, string>("dse_processorState", "searchTaxJsp"),
                    new KeyValuePair<string, string>("dse_processorId", _processorId),
                    new KeyValuePair<string, string>("dse_errorPage", "error_page.jsp"),
                    new KeyValuePair<string, string>("dse_nextEventName", "ok"),
                    new KeyValuePair<string, string>("cboTenTKhai", ""),
                    new KeyValuePair<string, string>("maHdong", ""),
                    new KeyValuePair<string, string>("_ngaygui", fromDate),
                    new KeyValuePair<string, string>("_denngay", toDate)
                });
                resMes = await _client.PostAsync("/ICanhan/Request", content);
                resMes.EnsureSuccessStatusCode();
                doc.LoadHtml(await resMes.Content.ReadAsStringAsync());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw ex;
            }
            return doc;
        }

        private async Task<List<EtaxSubmitted>> ExtractEtaxSubmitteds(string fromDate, string toDate, HtmlDocument doc, List<EtaxNotification> notifys)
        {
            var lstEtaxSubmitted = new List<EtaxSubmitted>();
            try
            {
                var lstTrEls = doc.DocumentNode.SelectNodes("//*[@id='frmtracuu']/div/table/tr");
                if (lstTrEls != null && lstTrEls.Count > 0)
                {
                    for (int i = 0; i < lstTrEls.Count; i++)
                    {
                        try
                        {
                            if (i > 0)
                            {
                                var trEl = lstTrEls[i];
                                if (trEl.ChildNodes.Count > 0)
                                {
                                    var lstTds = trEl.Elements("td").ToList();
                                    var etaxSubmitted = new EtaxSubmitted()
                                    {
                                        SortOrder = lstTds[0].InnerText.Trim(),
                                        TaxCode = _taxCode,
                                        Name = lstTds[1].InnerText.Trim(),
                                        TaxPeriod = lstTds[2].InnerText.Trim(),
                                        DeClarationType = lstTds[5].InnerText.Trim(),
                                        SubmitTimes = lstTds[6].InnerText.Trim(),
                                        SubmitDate = lstTds[7].InnerText.Trim(),
                                        TaxAgencyName = lstTds[8].InnerText.Trim(),
                                        StateTitle = lstTds[9].InnerText.Trim()
                                    };

                                    // tag tải xml
                                    var downfileTag = lstTds[14].Element("a");
                                    if (downfileTag != null)
                                    {
                                        var downfileEl = downfileTag.Attributes["href"].Value;
                                        if (!string.IsNullOrWhiteSpace(downfileEl))
                                        {
                                            var transactionId = "";
                                            var pattern = @"downloadGNT\((.*?)\)";
                                            // record có quan hệ cha con
                                            if (!string.IsNullOrWhiteSpace(etaxSubmitted.SortOrder) && etaxSubmitted.SortOrder.IndexOf(".") > -1)
                                            {
                                                pattern = @"downloadBKe\('(.*?)'\)";
                                                var parentOrder = etaxSubmitted.SortOrder.Split('.')[0];
                                                var parentEtax = lstEtaxSubmitted.FirstOrDefault(_ => _.SortOrder == parentOrder);
                                                if(parentEtax != null)
                                                {
                                                    etaxSubmitted.ParentTransactionID = parentEtax.TransactionID;
                                                }
                                            }
                                            
                                            foreach (Match match in Regex.Matches(downfileEl, pattern))
                                            {
                                                if (match.Success && match.Groups.Count > 0)
                                                {
                                                    transactionId = match.Groups[1].Value;
                                                }
                                            }

                                            if (!string.IsNullOrWhiteSpace(transactionId))
                                            {
                                                var downLoadParam = transactionId.Replace("'", "").Split(',');
                                                etaxSubmitted.TransactionID = downLoadParam[0];
                                                if (!string.IsNullOrWhiteSpace(etaxSubmitted.ParentTransactionID))
                                                {
                                                    await DownLoadAttachmentFile(etaxSubmitted);
                                                }
                                                else
                                                {
                                                    var formatType = (downLoadParam.Length >= 1) ? downLoadParam[1] : "";
                                                    await DownLoadTransactionFile(etaxSubmitted, formatType);
                                                }
                                            }

                                            if (!string.IsNullOrWhiteSpace(etaxSubmitted.TransactionID) && notifys != null && notifys.Count > 0)
                                            {
                                                var notifyByTransIds = notifys.Where(_ => _.TransactionID == etaxSubmitted.TransactionID).ToList();
                                                etaxSubmitted.Notifications = notifyByTransIds;
                                            }
                                        }
                                    }
                                    lstEtaxSubmitted.Add(etaxSubmitted);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return lstEtaxSubmitted;
        }

        private async Task DownLoadAttachmentFile(EtaxSubmitted etaxSubmitted)
        {
            HttpResponseMessage resMes = null;
            try
            {
                var content = new FormUrlEncodedContent(new[]
               {
                    new KeyValuePair<string, string>("dse_sessionId", _sessionId),
                    new KeyValuePair<string, string>("dse_applicationId", "-1"),
                    new KeyValuePair<string, string>("dse_operationName", "retailSearchVoucherProc"),
                    new KeyValuePair<string, string>("dse_pageId", "10"),
                    new KeyValuePair<string, string>("dse_processorState", "searchTaxJsp"),
                    new KeyValuePair<string, string>("dse_processorId", _processorId),
                    new KeyValuePair<string, string>("dse_errorPage", "error_page.jsp"),
                    new KeyValuePair<string, string>("dse_nextEventName", "downloadBKe"),
                    new KeyValuePair<string, string>("idBKe", etaxSubmitted.TransactionID),
                    new KeyValuePair<string, string>("chkTTS", "1")
                });
                await Task.Delay(1000);
                resMes = await _client.PostAsync("/ICanhan/Request", content);
                resMes.EnsureSuccessStatusCode();
                etaxSubmitted.FileName = resMes.Content.Headers.ContentDisposition.FileName;
                var folderPath = $"{Directory.GetCurrentDirectory()}/OutputFiles/{etaxSubmitted.TransactionID}";
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }
                using (var fs = new FileStream($"{folderPath}\\{etaxSubmitted.FileName}", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
                {
                    await resMes.Content.CopyToAsync(fs);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private async Task DownLoadTransactionFile(EtaxSubmitted etaxSubmitted, string fileFormat)
        {
            HttpResponseMessage resMes = null;
            try
            {
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("dse_sessionId", _sessionId),
                    new KeyValuePair<string, string>("dse_applicationId", "-1"),
                    new KeyValuePair<string, string>("dse_operationName", "retailSearchVoucherProc"),
                    new KeyValuePair<string, string>("dse_pageId", "13"),
                    new KeyValuePair<string, string>("dse_processorState", "searchTaxJsp"),
                    new KeyValuePair<string, string>("dse_processorId", _processorId),
                    new KeyValuePair<string, string>("dse_errorPage", "error_page.jsp"),
                    new KeyValuePair<string, string>("dse_nextEventName", "download"),
                    new KeyValuePair<string, string>("idTBao", etaxSubmitted.TransactionID),
                    new KeyValuePair<string, string>("fileFormat", fileFormat),
                    new KeyValuePair<string, string>("chkTTS", "1")
                });
                await Task.Delay(1000);
                resMes = await _client.PostAsync("/ICanhan/Request", content);
                resMes.EnsureSuccessStatusCode();
                etaxSubmitted.FileName = resMes.Content.Headers.ContentDisposition.FileName;
                
                var folderPath = $"{Directory.GetCurrentDirectory()}/OutputFiles/{etaxSubmitted.TransactionID}";
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }
                
                using (var fs = new FileStream($"{folderPath}\\{etaxSubmitted.FileName}", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
                {
                    await resMes.Content.CopyToAsync(fs);
                }
                if (Path.GetExtension(etaxSubmitted.FileName) == ".xml")
                {
                    try
                    {
                        var xmldoc = new XmlDocument();
                        var xmlString = await resMes.Content.ReadAsStringAsync();
                        xmldoc.LoadXml(xmlString);
                        var xmlNameSp = new XmlNamespaceManager(xmldoc.NameTable);
                        xmlNameSp.AddNamespace("msbld", "http://kekhaithue.gdt.gov.vn/TKhaiThue");
                        var nodeCode = xmldoc.SelectSingleNode("//msbld:maTKhai", xmlNameSp);
                        if(nodeCode != null)
                        {
                            etaxSubmitted.Code = nodeCode.InnerText;
                        }
                        var nodeAgencyCode = xmldoc.SelectSingleNode("//msbld:maCQTNoiNop", xmlNameSp);
                        if(nodeAgencyCode != null)
                        {
                            etaxSubmitted.TaxAgencyCode = nodeAgencyCode.InnerText;
                        }
                        decimal debitAmount = CaculteDebitAmount(xmldoc, etaxSubmitted.Code, xmlNameSp);
                        etaxSubmitted.DebitAmount = debitAmount.ToString();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private decimal CaculteDebitAmount(XmlDocument xmldoc, string Code, XmlNamespaceManager xmlNameSp)
        {
            decimal debitAmount = 0;
            switch (Code)
            {
                case "470":
                    var nodeCaNhanct25 = xmldoc.SelectSingleNode("//msbld:CaNhanKeKhai/msbld:ct25", xmlNameSp);
                    if (nodeCaNhanct25 != null)
                    {
                        if (decimal.TryParse(nodeCaNhanct25.InnerText, out decimal result))
                        {
                            debitAmount = debitAmount + result;
                        }
                    }
                    var nodeCaNhanct29 = xmldoc.SelectSingleNode("//msbld:CaNhanKeKhai/msbld:ct29", xmlNameSp);
                    if (nodeCaNhanct29 != null)
                    {
                        if (decimal.TryParse(nodeCaNhanct29.InnerText, out decimal result))
                        {
                            debitAmount = debitAmount + result;
                        }
                    }
                    break;
                case "473":
                    var nodeGTGTCt32 = xmldoc.SelectSingleNode("//msbld:SoThueGTGT/msbld:ct32", xmlNameSp);
                    if (nodeGTGTCt32 != null)
                    {
                        if (decimal.TryParse(nodeGTGTCt32.InnerText, out decimal result))
                        {
                            debitAmount = debitAmount + result;
                        }
                    }
                    var nodeTNCNCt32 = xmldoc.SelectSingleNode("//msbld:SoThueTNCN/msbld:ct32", xmlNameSp);
                    if (nodeTNCNCt32 != null)
                    {
                        if (decimal.TryParse(nodeTNCNCt32.InnerText, out decimal result))
                        {
                            debitAmount = debitAmount + result;
                        }
                    }
                    var nodeTTDBTongct5 = xmldoc.SelectSingleNode("//msbld:KKhaiThueTTDB/msbld:tong_ct5", xmlNameSp);
                    if (nodeTTDBTongct5 != null)
                    {
                        if (decimal.TryParse(nodeTTDBTongct5.InnerText, out decimal result))
                        {
                            debitAmount = debitAmount + result;
                        }
                    }
                    var nodeTTDBTongct7 = xmldoc.SelectSingleNode("//msbld:KKhaiThueTTDB/msbld:tong_ct7", xmlNameSp);
                    if (nodeTTDBTongct7 != null)
                    {
                        if (decimal.TryParse(nodeTTDBTongct7.InnerText, out decimal result))
                        {
                            debitAmount = debitAmount + result;
                        }
                    }
                    var nodeTaiNguyenTong = xmldoc.SelectSingleNode("//msbld:ThueTaiNguyen/msbld:tongCong", xmlNameSp);
                    if (nodeTaiNguyenTong != null)
                    {
                        if (decimal.TryParse(nodeTaiNguyenTong.InnerText, out decimal result))
                        {
                            debitAmount = debitAmount + result;
                        }
                    }
                    var nodeBVMTTong = xmldoc.SelectSingleNode("//msbld:ThueBVMT/msbld:tongCong", xmlNameSp);
                    if (nodeBVMTTong != null)
                    {
                        if (decimal.TryParse(nodeBVMTTong.InnerText, out decimal result))
                        {
                            debitAmount = debitAmount + result;
                        }
                    }
                    var nodePhiBVMTTong = xmldoc.SelectSingleNode("//msbld:PhiBVMT/msbld:tongCong", xmlNameSp);
                    if (nodePhiBVMTTong != null)
                    {
                        if (decimal.TryParse(nodePhiBVMTTong.InnerText, out decimal result))
                        {
                            debitAmount = debitAmount + result;
                        }
                    }
                    break;
                default:
                    break;
            }
            return debitAmount;
        }

        private async Task<string> GotoResearchNotification()
        {
            HttpResponseMessage resMes = null;
            var processorId = string.Empty;
            try
            {
                var requestUri = $"/ICanhan/Request?dse_sessionId={_sessionId}";
                requestUri = string.Concat(requestUri, $"&dse_applicationId=-1&dse_pageId=7&dse_operationName=retailSearchTaxNoticesProc");
                requestUri = string.Concat(requestUri, $"&dse_processorState=initial&dse_nextEventName=start");
                resMes = await _client.GetAsync(requestUri);
                resMes.EnsureSuccessStatusCode();

                var doc = new HtmlDocument();
                doc.LoadHtml(await resMes.Content.ReadAsStringAsync());
                var inputProcessorId = doc.DocumentNode.SelectNodes("//*[@name='dse_processorId']");
                if (inputProcessorId != null)
                {
                    processorId = inputProcessorId.FirstOrDefault().Attributes["value"].Value;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return processorId;
        }

        private async Task<HtmlDocument> Click_ResearchNotification(string processorId, string fromDate, string toDate)
        {
            var doc = new HtmlDocument();
            HttpResponseMessage resMes = null;
            try
            {
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("dse_sessionId", _sessionId),
                    new KeyValuePair<string, string>("dse_applicationId", "-1"),
                    new KeyValuePair<string, string>("dse_operationName", "retailSearchTaxNoticesProc"),
                    new KeyValuePair<string, string>("dse_pageId", "11"),
                    new KeyValuePair<string, string>("dse_processorState", "searchTaxJsp"),
                    new KeyValuePair<string, string>("dse_processorId", processorId),
                    new KeyValuePair<string, string>("dse_errorPage", "error_page.jsp"),
                    new KeyValuePair<string, string>("dse_nextEventName", "ok"),
                    new KeyValuePair<string, string>("cboThongBao", ""),
                    new KeyValuePair<string, string>("_ngaygui", fromDate),
                    new KeyValuePair<string, string>("_denngay", toDate)
                });
                resMes = await _client.PostAsync("/ICanhan/Request", content);
                resMes.EnsureSuccessStatusCode();
                doc.LoadHtml(await resMes.Content.ReadAsStringAsync());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return doc;
        }

        private async Task<List<EtaxNotification>> Extract_Notification_NextPage(int page, string processorId)
        {
            var etaxNotifications = new List<EtaxNotification>();
            HttpResponseMessage resMes = null;
            try
            {
                var requestUri = $"/ICanhan/Request?dse_sessionId={_sessionId}";
                requestUri = string.Concat(requestUri, $"&dse_applicationId=-1&dse_pageId=13&dse_operationName=retailSearchTaxNoticesProc");
                requestUri = string.Concat(requestUri, $"&dse_processorState=searchTaxJsp&dse_processorId={processorId}");
                requestUri = string.Concat(requestUri, $"&dse_errorPage=error_page.jsp&dse_nextEventName=nextPage&pn={page}");
                resMes = await _client.GetAsync(requestUri);
                resMes.EnsureSuccessStatusCode();

                var doc = new HtmlDocument();
                doc.LoadHtml(await resMes.Content.ReadAsStringAsync());
                await Task.Delay(1000);
                etaxNotifications = await Extract_Notification(doc, processorId);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return etaxNotifications;
        }

        private async Task DownLoadNotificationFile(EtaxNotification itaxNoti, string processorId, string notifiType)
        {
            HttpResponseMessage resMes = null;
            try
            {
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("dse_sessionId", _sessionId),
                    new KeyValuePair<string, string>("dse_applicationId", "-1"),
                    new KeyValuePair<string, string>("dse_operationName", "retailSearchTaxNoticesProc"),
                    new KeyValuePair<string, string>("dse_pageId", "15"),
                    new KeyValuePair<string, string>("dse_processorState", "searchTaxJsp"),
                    new KeyValuePair<string, string>("dse_processorId", processorId),
                    new KeyValuePair<string, string>("dse_errorPage", "error_page.jsp"),
                    new KeyValuePair<string, string>("dse_nextEventName", "download"),
                    new KeyValuePair<string, string>("idTBao", itaxNoti.NotificationID),
                    new KeyValuePair<string, string>("loaitbao", notifiType)
                });
                resMes = await _client.PostAsync("/ICanhan/Request", content);
                resMes.EnsureSuccessStatusCode();

                itaxNoti.FileName = resMes.Content.Headers.ContentDisposition.FileName;
                if (Path.GetExtension(itaxNoti.FileName) == ".xml")
                {
                    try
                    {
                        var xmldoc = new XmlDocument();
                        var xmlString = await resMes.Content.ReadAsStringAsync();
                        xmldoc.LoadXml(xmlString);
                        var xmlNameSp = new XmlNamespaceManager(xmldoc.NameTable);
                        xmlNameSp.AddNamespace("msbld", "http://kekhaithue.gdt.gov.vn/TBaoThue");
                        var nodeNotiCode = xmldoc.SelectSingleNode("//msbld:maTBao", xmlNameSp);
                        if (nodeNotiCode != null)
                        {
                            itaxNoti.Code = nodeNotiCode.InnerText;
                        }
                        var nodeStatus = xmldoc.SelectSingleNode("//msbld:trangThai", xmlNameSp);
                        if(nodeStatus != null)
                        {
                            itaxNoti.XMLTrangThai = nodeStatus.InnerText; 
                        }
                        var nodeNotiName = xmldoc.SelectSingleNode("//msbld:tenTBao", xmlNameSp);
                        if(nodeNotiName!= null)
                        {
                            itaxNoti.XMLTenTBao = nodeNotiName.InnerText;
                        }
                        var nodeTransID = xmldoc.SelectSingleNode("//msbld:maGiaoDichDTu", xmlNameSp);
                        if(nodeTransID != null)
                        {
                            itaxNoti.TransactionID = nodeTransID.InnerText;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
                if(itaxNoti.TransactionID != null)
                {
                    var folderPathTrans = $"{Directory.GetCurrentDirectory()}/OutputFiles/{itaxNoti.TransactionID}";
                    Directory.CreateDirectory(folderPathTrans);
                    using (var fs = new FileStream($"{folderPathTrans}\\{itaxNoti.FileName}", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
                    {
                        await resMes.Content.CopyToAsync(fs);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private async Task<List<EtaxNotification>> Extract_Notification(HtmlDocument doc, string processorId)
        {
            var etaxNotifications = new List<EtaxNotification>();
            try
            {
                var lstTrEl = doc.DocumentNode.SelectNodes("//*[@id='frmtracuu']/table/tr");
                if (lstTrEl != null && lstTrEl.Count > 0)
                {
                    for (int i = 0; i < lstTrEl.Count; i++)
                    {
                        try
                        {
                            if (i > 0)
                            {
                                var trEl = lstTrEl[i];
                                if (trEl.ChildNodes.Count > 0)
                                {
                                    var tds = trEl.Elements("td").ToList();
                                    var itaxNoti = new EtaxNotification()
                                    {
                                        Name = tds[1].InnerText.Trim(),
                                        Message = tds[2].InnerText.Trim(),
                                        SendDate = $"{tds[3].InnerText.Trim()} 12:00:00"
                                    };
                                    var downLoadTag = tds[4].Element("a");
                                    if (downLoadTag != null)
                                    {
                                        var notifiID = downLoadTag.Attributes["href"].Value;
                                        if (!string.IsNullOrWhiteSpace(notifiID))
                                        {
                                            var pattern = @"downloadGNT\((.*?)\)";
                                            foreach (Match match in Regex.Matches(notifiID, pattern))
                                            {
                                                if (match.Success && match.Groups.Count > 0)
                                                {
                                                    notifiID = match.Groups[1].Value;
                                                }
                                            }
                                            if (!string.IsNullOrWhiteSpace(notifiID))
                                            {
                                                var downLoadParam = notifiID.Split(',');
                                                notifiID = downLoadParam[0];
                                                var notifiType = downLoadParam[1];
                                                itaxNoti.NotificationID = notifiID;
                                                await Task.Delay(1000);
                                                await DownLoadNotificationFile(itaxNoti, processorId, notifiType);
                                            }

                                        }
                                    }
                                    etaxNotifications.Add(itaxNoti);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return etaxNotifications;
        }

        private async Task<List<EtaxNotification>> GetNotification(string fromDate)
        {
            var etaxNotifications = new List<EtaxNotification>();
            try
            {
                var today = DateTime.Now.ToString("dd/MM/yyyy");
                var processorId = await GotoResearchNotification();
                await Task.Delay(1000);
                var doc = await Click_ResearchNotification(processorId, fromDate, today);
                var curPage = 1;
                var nodeCountPage = doc.DocumentNode.SelectSingleNode("//*[@id='currAcc']/b[3]");
                if(nodeCountPage != null)
                {
                    var countPage = int.Parse(nodeCountPage.InnerText);
                    // extract noti page 1
                    var notifiTaxs = await Extract_Notification(doc, processorId);
                    etaxNotifications.AddRange(notifiTaxs);

                    while (curPage < countPage)
                    {
                        curPage += 1;
                        await Task.Delay(1000);
                        var notifiTaxNextPages = await Extract_Notification_NextPage(curPage, processorId);
                        etaxNotifications.AddRange(notifiTaxNextPages);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return etaxNotifications;
        }

        public async Task<List<EtaxSubmitted>> GetEtaxSubmitteds(string fromDate, string toDate)
        {
            var notifys = new List<EtaxNotification>();
            var folderPath = $"{Directory.GetCurrentDirectory()}/OutputFiles";
            Directory.CreateDirectory(folderPath);
            var doc = await CrawPattern.ReTry(async () =>
            {
                await SignInPage();
                await Task.Delay(1000);
                notifys = await GetNotification(fromDate);
                await Task.Delay(1000);
                await GotoResearchETaxPage();
                await Task.Delay(1000);
                return await Click_ButtonTraCuu(fromDate, toDate);
            });
            await Task.Delay(1000);
            return await ExtractEtaxSubmitteds(fromDate, toDate, doc, notifys);
        }
        #endregion
    }
}
