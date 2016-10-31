using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Collections.Specialized;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace WHMCS_License_Test_CSharp
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            progressBar1.Maximum = 100;
            progressBar1.Value = 10;
            string[] array = new string[] {textBox2.Text,textBox7.Text,textBox3.Text};
            backgroundWorker1.RunWorkerAsync(array);
            textBox8.Text = Environment.UserDomainName;
            //string clientIP = currentIP();
            if (!checkBox1.Checked)
            {
                //clientIP = GetPublicIP(); //Change this to public ip if you would like
            }
            //textBox4.Text = clientIP;
            textBox5.Text = Environment.CurrentDirectory;
        }

        public Dictionary<string, string> checkLicense(ref BackgroundWorker sender, string licensekey, string whmcsUrl, string licensingSecretKey)
        {
            var backgroundWorker = sender as BackgroundWorker;
            Random rand = new Random();
            Dictionary<string, string> results = new Dictionary<string, string>();
            long ticks = DateTime.UtcNow.Ticks - DateTime.Parse("01/01/1970 00:00:00").Ticks;
            ticks /= 10000000; //Convert windows ticks to seconds
            string timestamp = ticks.ToString();
            string checkToken = timestamp + CalculateMD5Hash(rand.Next(100000000, 999999999)*10 + licensekey);
            string clientIP = currentIP();
            if (!checkBox1.Checked)
            {
                clientIP = GetPublicIP(); //Change this to public ip if you would like
                backgroundWorker.ReportProgress(30);
            }
            WebClient WHMCSclient = new WebClient();
            NameValueCollection form = new NameValueCollection();
            form.Add("licensekey", licensekey);
            form.Add("domain", Environment.UserDomainName);
            form.Add("ip", clientIP);
            form.Add("dir", Environment.CurrentDirectory);
            form.Add("check_token", checkToken);
            //Check our local key (optional)
            if (!validateLocalKey())
            {
                // Post the data and read the response
                if (validURL(whmcsUrl))
                {


                    Byte[] responseData = WHMCSclient.UploadValues(whmcsUrl + "modules/servers/licensing/verify.php", form);
                    // Decode and display the response. 
                    //textBox1.AppendText("Response received was " + Encoding.ASCII.GetString(responseData) + Environment.NewLine);

                    backgroundWorker.ReportProgress(60);
                    XDocument doc = XDocument.Parse("<?xml version='1.0' encoding='utf-8' ?><Response>" + Encoding.ASCII.GetString(responseData) + "</Response>");
                    Dictionary<string, string> dataDictionary = new Dictionary<string, string>();

                    foreach (XElement element in doc.Descendants().Where(p => p.HasElements == false))
                    {
                        int keyInt = 0;
                        string keyName = element.Name.LocalName;

                        while (dataDictionary.ContainsKey(keyName))
                        {
                            keyName = element.Name.LocalName + "_" + keyInt++;
                        }

                        dataDictionary.Add(keyName, element.Value);
                    }

                    dataDictionary["responseReceived"] = "Response received was " + Encoding.ASCII.GetString(responseData) + Environment.NewLine;
                    dataDictionary["originalCheckToken"] = checkToken;

                    if (dataDictionary.ContainsKey("md5hash"))
                    {
                        if (!dataDictionary["md5hash"].Equals(CalculateMD5Hash(licensingSecretKey + checkToken)))
                        {
                            dataDictionary["status"] = "Invalid";
                            dataDictionary["description"] = "MD5 Checksum Verification Failed " + dataDictionary["md5hash"] + " != " + CalculateMD5Hash(licensingSecretKey + checkToken);
                            return dataDictionary;
                        }
                    }

                    results = dataDictionary;
                }
                else
                {
                    results["responseReceived"] = "URL invalid." + Environment.NewLine;
                    results["status"] = "Invalid";
                    results["description"] = "URL invalid";
                }
            }
            else
            {
                results["status"] = "Active";
                results["description"] = "Validated by local key.";
            }
            
            return results;
        }

        private string CalculateMD5Hash(string input)
        {

            // byte array representation of that string
            byte[] encodedPassword = new UTF8Encoding().GetBytes(input);

            // need MD5 to calculate the hash
            byte[] hash = ((HashAlgorithm)CryptoConfig.CreateFromName("MD5")).ComputeHash(encodedPassword);

            // string representation (similar to UNIX format)
            string encoded = BitConverter.ToString(hash)
                // without dashes
               .Replace("-", string.Empty)
                // make lowercase
               .ToLower();
            return encoded;
        }

        public static string ConvertStringtoMD5(string strword)
        {
            MD5 md5 = MD5.Create();
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(strword);
            byte[] hash = md5.ComputeHash(inputBytes);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("x2"));
            }
            return sb.ToString().ToLower();
        }

        private bool validURL(string uriName)
        {
            //http://stackoverflow.com/questions/7578857/how-to-check-whether-a-string-is-a-valid-http-url
            Uri uriResult;
            bool result = Uri.TryCreate(uriName, UriKind.Absolute, out uriResult) && (   uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps );
            return result;
        }

        private string currentIP()
        {
            IPHostEntry host;
            string localIP = "?";
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIP = ip.ToString();
                }
            }
            return localIP;
        }

        private string GetPublicIP()
        {
            String direction = "";
            try 
            {
            WebRequest request = WebRequest.Create("http://checkip.dyndns.org/");
            using (WebResponse response = request.GetResponse())
            using (StreamReader stream = new StreamReader(response.GetResponseStream()))
            {
                direction = stream.ReadToEnd();
            }

            //Search for the ip in the html
            int first = direction.IndexOf("Address: ") + 9;
            int last = direction.LastIndexOf("</body>");
            direction = direction.Substring(first, last - first);

            return direction;
            }
            catch
            {
                return "0.0.0.0";
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            textBox2.Text = "Testing-c79621ced16422a5cbccee29f044d1aa6d5";
            textBox3.Text = "MD2X54236f7GT4z";
            textBox7.Text = "http://anveto.com/members/";
        }

        private string generateLocalKey()
        {
            // this becomes really software specific, basically you want to generate and store a hash of some kind here perhaps
            // if you want to have the license be checked everytime then just return an empty string here, such as when the program is launched
            // if this application is meant to run for a long time you might want to have the license check done during specific events but only so often. If this is the case then you should set the last check time and an interval between checks.
            return "";
        }

        private bool validateLocalKey()
        {
            return false;
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            var backgroundWorker = sender as BackgroundWorker;
            string[] stringarray = (string[])e.Argument;
            Dictionary<string, string> data = checkLicense(ref backgroundWorker, stringarray[0],stringarray[1],stringarray[2]);
            e.Result = data;
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.BeginInvoke(new MethodInvoker(() => { progressBar1.Value = e.ProgressPercentage; }));
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Dictionary<string, string> data = (Dictionary<string,string>)e.Result;
            progressBar1.Value = 100;
            string active = "Active";
            if (data.ContainsKey("responseReceived"))
            {
                textBox1.Text = data["responseReceived"];
            }
            if (data.ContainsKey("originalCheckToken"))
            {
                textBox9.Text = data["originalCheckToken"];
            }
            if (data.ContainsKey("validip"))
            {
                textBox4.Text = data["validip"];
            }
            if (active.Equals(data["status"]))
            {
                label10.Text = "Active";
                label10.ForeColor = Color.FromArgb(0, 192, 0);
            }
            else
            {
                label10.Text = data["status"];
                label10.ForeColor = Color.FromArgb(192, 0, 0);
                if (active.Equals(data["status"]))
                {
                    label11.Text = data["description"];
                }
            }
        }
    }
}
