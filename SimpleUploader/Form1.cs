using Microsoft.Win32;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SimpleUploader
{
    public partial class Form1 : Form
    {
        public string FilePath { get; set; }
        public string UploadedUrl { get; set; }
        FileStream fileStream = null;
        
        public Form1(string filePath)
        {
            InitializeComponent();
            this.FilePath = filePath;
            Application.ApplicationExit += Application_ApplicationExit;
            CheckForIllegalCrossThreadCalls = false;
            backgroundWorker1.RunWorkerAsync();
        }

        private void Application_ApplicationExit(object sender, EventArgs e)
        {
            if (fileStream != null)
                fileStream.Close();
        }


        private void btnCopyURL_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(UploadedUrl);
            Application.Exit();
        }

        void UploadFile()
        {
            if (string.IsNullOrEmpty(FilePath))
            {
                Application.Exit();
            }

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.ConnectionStrings["StorageConStr"].ConnectionString);

            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            CloudBlobContainer container = blobClient.GetContainerReference("uploads");

            if (container.CreateIfNotExists())
                container.SetPermissions(
                        new BlobContainerPermissions
                        {
                            PublicAccess = BlobContainerPublicAccessType.Blob
                        });

            CloudBlockBlob blockBlob = container.GetBlockBlobReference(Path.GetFileName(FilePath));
            if (blockBlob.Exists())
                blockBlob = container.GetBlockBlobReference(Path.GetFileNameWithoutExtension(FilePath) + Guid.NewGuid().ToString().Split('-')[0] + Path.GetExtension(FilePath));

            try
            {
                fileStream = File.Open(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);

                this.Text = $"Uploading... {Path.GetFileName(FilePath)}";

                int bytesRead = 0;
                int i = 0;
                int uploadBlockSize = (int)Math.Pow(1024, 2);
                int totalBytesRead = 0;
                byte[] buffer = new byte[uploadBlockSize];

                List<string> blockIDList = new List<string>();
                bytesRead = fileStream.Read(buffer, 0, uploadBlockSize);

                while (bytesRead > 0)
                {
                    using (MemoryStream ms = new MemoryStream(buffer, 0, bytesRead))
                    {
                        char[] tempID = new char[6];
                        string iStr = i.ToString();
                        for (int j = tempID.Length - 1; j > tempID.Length - iStr.Length - 1; j--)
                        {
                            tempID[j] = iStr[tempID.Length - j - 1];
                        }
                        byte[] blockIDBeforeEncoding = Encoding.UTF8.GetBytes(tempID);
                        string blockID = Convert.ToBase64String(blockIDBeforeEncoding);
                        blockIDList.Add(blockID);

                        blockBlob.PutBlock(blockID, ms, null);

                    }

                    totalBytesRead += bytesRead;
                    i++;

                    double dProgressPercentage = ((double)(totalBytesRead) / (double)fileStream.Length);
                    int iProgressPercentage = (int)(dProgressPercentage * 100);
                    backgroundWorker1.ReportProgress(iProgressPercentage);
                    bytesRead = fileStream.Read(buffer, 0, uploadBlockSize);
                }

                blockBlob.PutBlockList(blockIDList);
                UploadedUrl = blockBlob.Uri.ToString();
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Error!";
                this.Text = "Something Wrong Happend!";
                if (progressBar1.Value == 0)
                    progressBar1.Value = 100;
                ModifyProgressBarColor.SetState(progressBar1, 2);
            }
            finally
            {
                if (fileStream != null)
                    fileStream.Close();
            }

            /*try
            {

                await blockBlob.UploadFromFileAsync(FilePath);

                Clipboard.SetText(blockBlob.Uri.ToString());
                btnCopyURL.Enabled = true;
            }
            catch { }*/
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            UploadFile();
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
            lblStatus.Text = $"%{e.ProgressPercentage}";
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (string.IsNullOrEmpty(UploadedUrl)) return;
            Clipboard.SetText(UploadedUrl);
            btnCopyURL.Enabled = true;
        }
    }

    public static class ModifyProgressBarColor
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
        static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr w, IntPtr l);
        public static void SetState(this ProgressBar pBar, int state)
        {
            SendMessage(pBar.Handle, 1040, (IntPtr)state, IntPtr.Zero);
        }
    }
}