﻿using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Analogy.Common.Interfaces;
using Analogy.Interfaces;
using DevExpress.XtraEditors;

namespace Analogy.CommonControls.Forms
{
    public partial class AnalogyAddCommentsToMessage : XtraForm
    {
        public AnalogyLogMessage Message { get; set; }
        private IUserSettingsManager Settings { get; }

        public AnalogyAddCommentsToMessage()
        {
            InitializeComponent();
        }

        public AnalogyAddCommentsToMessage(AnalogyLogMessage m, IUserSettingsManager settings) : this()
        {
            Message=m;
            Settings = settings;
        }
        private void sBtnOk_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void sBtnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void AnalogyAddCommentsToMessage_Load(object sender, EventArgs e)
        {
            lblMessage.Text = $"Message: {Message.Text}";
        }

        private void btnFull_Click(object sender, EventArgs e)
        {
            var details=new FormMessageDetails(Message, new List<AnalogyLogMessage>() {Message}, "",Settings);
            details.Show(this);
        }

        private void btnAddNote_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(memoNoteKey.Text))
            {
                if( Message.AdditionalInformation==null)
                {
                    Message.AdditionalInformation=new Dictionary<string, string>();
                }

                Message.AdditionalInformation[memoNoteKey.Text] = memoText.Text;
            }
        }
    }
}
