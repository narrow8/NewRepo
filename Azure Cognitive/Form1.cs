using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using Azure_Cognitive.Properties;

namespace Azure_Cognitive
{
    public partial class Form1 : Form
    {

        const string subscriptionKey = "bbeacdccdcbc474787bee03d9e8096bf";
        const string uriBase =
         "https://westeurope.api.cognitive.microsoft.com/face/v1.0/detect";
        static string[] faceID = new string[100];
        static double[] result = new double[100];
        static string[] knownFace = new string[100];
        static Rectangle[] rect = new Rectangle[100];
        static int flag = 0;
        static int ready = 0;
        static int k1 = 0;

        Timer timer = new Timer();

        public Form1()
        {
            InitializeComponent();

            timer.Enabled = true;
            timer.Tick += delegate
            {
                try
                {
                    
                    if (ready == 1)
                    {
                        DetectFace(faceID);

                        ready = 0;
                    }

                    if (flag == 1)
                    {
                        flag = 0;
                        
                        pictureBox1.Refresh();
                    }
                }
                catch { }
            };

        }

        private void button1_Click(object sender, EventArgs e)
        {
            string imageFilePath = "";

            OpenFileDialog fileDialog = new OpenFileDialog();

            if (fileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                imageFilePath = fileDialog.FileName;
            }

            pictureBox1.Image = new Bitmap(imageFilePath);

            MakeAnalysisRequest(imageFilePath, 1);
        }

        static async void MakeAnalysisRequest(string imageFilePath, int type)
        {
            HttpClient client = new HttpClient();

            client.DefaultRequestHeaders.Add(
                "Ocp-Apim-Subscription-Key", subscriptionKey);

            string requestParameters = "returnFaceId=true&returnFaceLandmarks=false";

            string uri = uriBase + "?" + requestParameters;

            HttpResponseMessage response;

            byte[] byteData = GetImageAsByteArray(imageFilePath);

            using (ByteArrayContent content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType =
                    new MediaTypeHeaderValue("application/octet-stream");

                response = await client.PostAsync(uri, content);

                string contentString = await response.Content.ReadAsStringAsync();

                Console.WriteLine(contentString);

                int k = 0;
                foreach (var s in contentString.Split(new string[] { "}}" }, StringSplitOptions.None))
                {
                    try
                    {
                        if (type == 1)
                            faceID[k] = s.Split('\"')[3];
                        else
                            knownFace[k1] = s.Split('\"')[3];

                        Console.WriteLine(faceID[k1]);

                        rect[k].Y = Convert.ToInt32(s.Split(new string[] { "faceRectangle\":" }, StringSplitOptions.None)[1].Split(',')[0].Split(':')[1]);
                        rect[k].X = Convert.ToInt32(s.Split(new string[] { "faceRectangle\":" }, StringSplitOptions.None)[1].Split(',')[1].Split(':')[1]);
                        rect[k].Height = Convert.ToInt32(s.Split(new string[] { "faceRectangle\":" }, StringSplitOptions.None)[1].Split(',')[3].Split(':')[1]);
                        rect[k].Width = Convert.ToInt32(s.Split(new string[] { "faceRectangle\":" }, StringSplitOptions.None)[1].Split(',')[2].Split(':')[1]);

                        k++;
                        k1++;
                    }
                    catch { }
                }
            }
            ready = 1;
        }

        static byte[] GetImageAsByteArray(string imageFilePath)
        {
            using (FileStream fileStream =
                new FileStream(imageFilePath, FileMode.Open, FileAccess.Read))
            {
                BinaryReader binaryReader = new BinaryReader(fileStream);
                return binaryReader.ReadBytes((int)fileStream.Length);
            }
        }

        static async void DetectFace(string[] faceID)
        {
            var client = new HttpClient();

            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "bbeacdccdcbc474787bee03d9e8096bf");

            var uri = "https://westeurope.api.cognitive.microsoft.com/face/v1.0/findsimilars";

            HttpResponseMessage response;
            
            int j = 0;
            foreach ( var face in faceID)
            {
                if (face != string.Empty && face != null)
                {
                    // Request body
                    string apireq = "{ \"faceId\": \"" + face + "\", \"faceIds\": [";
                    
                    foreach (var f in knownFace)
                    {
                        if (f != String.Empty && f!= null)
                            apireq += "\"" + f + "\", ";
                        else
                            break;
                    }

                    apireq += "]}";
                    Console.WriteLine(apireq);

                    byte[] byteData = Encoding.UTF8.GetBytes(apireq);

                    using (var content = new ByteArrayContent(byteData))
                    {
                        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                        response = await client.PostAsync(uri, content);

                        string contentString = await response.Content.ReadAsStringAsync();

                        int k = 0;
                        double sum = 0;

                        foreach (var s in contentString.Split(new string[] { "confidence\":" }, StringSplitOptions.None))
                        {
                            try
                            {
                                k++;
                                sum += Convert.ToDouble(s.Split('}')[0]);
                            }
                            catch { }
                        }

                        result[j++] = sum / (k - 1);
                        Console.WriteLine(face);
                    }
                }
                else
                    break;
            }

            flag = 1;
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            Pen pen = new Pen(Color.Black, 3);

            int k = 0;
            foreach (var res in result)
            {
                if (res > 0.5 && res != 0)
                {
                    e.Graphics.DrawRectangle(pen, rect[k++]);
                }
                else
                    e.Graphics.FillRectangle(Brushes.Black, rect[k++]);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string imageFilePath = "";

            OpenFileDialog fileDialog = new OpenFileDialog();

            if (fileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                imageFilePath = fileDialog.FileName;
            }

            pictureBox1.Image = new Bitmap(imageFilePath);

            MakeAnalysisRequest(imageFilePath, 2);
        }
    }
}
