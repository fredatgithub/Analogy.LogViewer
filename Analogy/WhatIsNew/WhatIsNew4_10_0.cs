﻿using DevExpress.XtraEditors;

namespace Analogy.WhatIsNew
{
    public partial class WhatIsNew4_10_0 : DevExpress.XtraEditors.XtraUserControl
    {
        public WhatIsNew4_10_0()
        {
            InitializeComponent();
        }
        private void OpenGithubIssue(object sender, DevExpress.Utils.HyperlinkClickEventArgs e)
        {
            ((HyperlinkLabelControl)sender).LinkVisited = true;
            Utils.OpenLink(e.Link);
        }

    }
}
