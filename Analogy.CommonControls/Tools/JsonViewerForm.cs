﻿using System;
using System.Windows.Forms;
using Analogy.Interfaces;

namespace Analogy.CommonControls.Tools
{
    public partial class JsonViewerForm : DevExpress.XtraEditors.XtraForm
    {
        private JsonTreeUC _jsonTreeView;
        private AnalogyLogMessage? Message { get; }
        private string JsonData { get; set; }
        private readonly bool _useRawField;

        public JsonViewerForm()
        {
            InitializeComponent();
            JsonData = string.Empty;
        }

        public JsonViewerForm(AnalogyLogMessage message) : this()
        {
            Message = message;
            _useRawField = message.RawTextType == AnalogyRowTextType.JSON;
        }
        public JsonViewerForm(string json) : this()
        {
            JsonData = json;
            _useRawField = false;
        }
        private void JsonViewerForm_Load(object sender, EventArgs e)
        {
            if (DesignMode)
            {
                return;
            }

            _jsonTreeView = new JsonTreeUC(); 
            splitContainerControl1.Panel1.Controls.Add(_jsonTreeView);
            _jsonTreeView.Dock = DockStyle.Fill;
            if (string.IsNullOrEmpty(JsonData) && Message != null)
            {
                memoEdit1.Text = _useRawField ? Message.RawText : Message.Text;
                JsonData =Utils.ExtractJsonObject(_useRawField ? Message.RawText : Message.Text);
                if (!string.IsNullOrEmpty(JsonData))
                {
                    _jsonTreeView.ShowJson(JsonData);
                }
                return;
            }
            if (!string.IsNullOrEmpty(JsonData))
            {
                memoEdit1.Text = JsonData;
                _jsonTreeView.ShowJson(JsonData);
            }


        }

        private void sbtnLoad_Click(object sender, EventArgs e)
        {
            _jsonTreeView.ShowJson(memoEdit1.Text);
        }
    }
}