﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static RB10.Bot.ToysrusToAmazon.ExecutingStateEvent;

namespace RB10.Bot.ToysrusToAmazon
{
    public partial class ExecForm : Form
    {
        private class Log
        {
            public string ProcessStatus { get; set; }
            public string LogDate { get; set; }
            public string Message { get; set; }
        }

        private BindingList<Log> _logs { get; set; }
        delegate void LogDelegate(string processStatus, string logDate, string message);

        public ExecForm()
        {
            InitializeComponent();
        }

        private void ExecForm_Load(object sender, EventArgs e)
        {
            // 画面チラツキ防止（ダブルバッファ設定）
            System.Type dgvtype = typeof(DataGridView);
            System.Reflection.PropertyInfo dgvPropertyInfo = dgvtype.GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            dgvPropertyInfo.SetValue(dataGridView1, true, null);

            comboBox1.DataSource = Scraping.ToysrusScraping.GetCategories();
            comboBox1.DisplayMember = "StoreName";
        }

        private void RunButton_Click(object sender, EventArgs e)
        {
            try
            {
                SaveFileDialog dlg = new SaveFileDialog();
                dlg.Title = "結果ファイルの出力先を指定して下さい。";
                dlg.Filter = "csvファイル (*.csv)|*.csv|すべてのファイル (*.*)|*.*";
                dlg.FileName = $"result_{DateTime.Now.ToString("yyyyMMddHHmmss")}.csv";
                if (dlg.ShowDialog() == DialogResult.Cancel) return;

                dataGridView1.Rows.Clear();

                var parameters = new Scraping.ScrapingManager.Parameters
                {
                    SaveFileName = dlg.FileName,
                    ToysrusDelay = (int)DelayNumericUpDown.Value,
                    AmazonDelay = (int)AmazonNumericUpDown.Value,
                    SearchKeyword = SearchKeywordTextBox.Text,
                };
                parameters.TargetUrls.AddRange((comboBox2.SelectedItem as Scraping.ToysrusScraping.Category).Urls.ToList());

                var task = new Scraping.ScrapingManager();
                task.ExecutingStateChanged += Task_ExecutingStateChanged;
                task.Start(parameters);
            }
            catch (ApplicationException ex)
            {
                MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Task_ExecutingStateChanged(object sender, ExecutingStateEventArgs e)
        {
            Invoke(new LogDelegate(UpdateLog), e.NotifyStatus.ToString(), e.ExecDate, e.Message);
        }

        private void UpdateLog(string processStatus, string logDate, string message)
        {
            if (_logs == null)
            {
                _logs = new BindingList<Log>();
                dataGridView1.DataSource = _logs;
            }

            _logs.Insert(0, new Log { ProcessStatus = processStatus, LogDate = logDate, Message = message });
        }

        private void dataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex == -1 || e.ColumnIndex == -1) return;

            if (dataGridView1.Columns[e.ColumnIndex].Name == Column4.Name)
            {
                if (e.Value.ToString() == "Warning")
                {
                    DataGridViewCellStyle cellStyle = new DataGridViewCellStyle() { BackColor = System.Drawing.Color.Yellow, ForeColor = System.Drawing.Color.Black };
                    dataGridView1.Rows[e.RowIndex].DefaultCellStyle = cellStyle;
                }
                else if (e.Value.ToString() == "Error")
                {
                    DataGridViewCellStyle cellStyle = new DataGridViewCellStyle() { BackColor = System.Drawing.Color.Red, ForeColor = System.Drawing.Color.White };
                    dataGridView1.Rows[e.RowIndex].DefaultCellStyle = cellStyle;
                }
                else if (e.Value.ToString() == "Exception")
                {
                    DataGridViewCellStyle cellStyle = new DataGridViewCellStyle() { BackColor = System.Drawing.Color.Black, ForeColor = System.Drawing.Color.White };
                    dataGridView1.Rows[e.RowIndex].DefaultCellStyle = cellStyle;
                }
            }
        }

        private const string MY_AWS_ACCESS_KEY_ID = "";
        private const string MY_AWS_SECRET_KEY = "";
        private const string DESTINATION = "ecs.amazonaws.jp";
        private const string ASSOCIATE_TAG = "baggio10cod02-22"; //今回は未指定でOKです。

        private void button1_Click(object sender, EventArgs e)
        {
            var helper = new Helper.SignedRequestHelper(MY_AWS_ACCESS_KEY_ID, MY_AWS_SECRET_KEY, DESTINATION, ASSOCIATE_TAG);

            var keyword = "ニンテンドー スイッチ 本体";

            IDictionary<string, string> request = new Dictionary<string, String>();
            request["Service"] = "AWSECommerceService";
            request["Operation"] = "ItemSearch";
            request["SearchIndex"] = "All";
            request["ResponseGroup"] = "Medium";
            request["Keywords"] = keyword;
            var requestUrl = helper.Sign(request);
            System.Xml.Linq.XDocument xml = System.Xml.Linq.XDocument.Load(requestUrl);


            System.Xml.Linq.XNamespace ex = "http://webservices.amazon.com/AWSECommerceService/2011-08-01";
            var query = xml.Descendants(ex + "Item");

            var elem = query.First();
            var asin = elem.Element(ex + "ASIN");
            var OfferSummary = elem.Element(ex + "OfferSummary");
            var qqq = OfferSummary.Element(ex + "LowestNewPrice").Element(ex + "Amount");


        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            comboBox2.DataSource = (comboBox1.SelectedValue as Scraping.ToysrusScraping.Store).Categories;
            comboBox2.DisplayMember = "CategoryName";
        }
    }
}
