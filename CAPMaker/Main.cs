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
using System.Xml.Linq;
using System.Reflection;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace CAPMaker
{
    public partial class Main : Form
    {
        public Dictionary<string, object> valueDicetonary = new Dictionary<string, object>();

        public List<string> invalidString = new List<string>();

        public Main()
        {
            InitializeComponent();

            init();

            this.Text = string.Format("{0} (beta) ", this.Text);
        }

        void init()
        {
            cbCertainty.SelectedIndex = 0;
            cbMsgType.SelectedIndex = 0;
            cbCategory.SelectedIndex = 2;
            cbSeverity.SelectedIndex = 0;
            cbStatus.SelectedIndex = 0;
            cbUrgency.SelectedIndex = 0;

            var now = DateTime.Now;

            nudSentHour.Value = nudExpiresHour.Value = nudEffectiveHour.Value = nudOnsetHour.Value = now.Hour;
            nudSentMinute.Value = nudExpiresMinute.Value = nudEffectiveMinute.Value = nudOnsetMinute.Value = now.Minute;
            nudSentSecond.Value = nudExpiresSecond.Value = nudEffectiveSecond.Value = nudOnsetSecond.Value = now.Second;


            tTrigger.Start();
        }

        private void rbCircle_CheckedChanged(object sender, EventArgs e)
        {
            tbCircle.Enabled = true;
            tbPolygon.Enabled = false;
        }

        private void rbPolygon_CheckedChanged(object sender, EventArgs e)
        {
            tbCircle.Enabled = false;
            tbPolygon.Enabled = true;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            string CapTemplate = File.ReadAllText(Environment.CurrentDirectory + "\\template.cap");

            valueDicetonary.Clear();
            invalidString.Clear();

            DateTime sent, effective, onset, expires;

            sent = GetDateTimeByName("Sent");
            effective = GetDateTimeByName("Effective");
            onset = GetDateTimeByName("Onset");
            expires = GetDateTimeByName("Expires");

            if (!CheckTime(sent, effective, onset, expires)) return;

            AddValue("identifier", tbIdentifier);
            AddValue("sender", tbSender);
            AddValue("sent", sent.toCapTimeString());
            AddValue("status", cbStatus);
            AddValue("msgtype", cbMsgType);
            AddValue("scope", lbScope);
            
            var refRequire = cbMsgType.SelectedItem.ToString() == "Update" || cbMsgType.SelectedItem.ToString() == "Cancel";

            if (refRequire && checkReferences(tbReference.Text) == false) return;

            AddValue("references", tbReference, refRequire, "", "references", true);

            AddValue("category", cbCategory);
            AddValue("event", tbEvent);
            AddValue("eventcode", tbEventCode);
            AddValue("urgency", cbUrgency);
            AddValue("severity", cbSeverity);
            AddValue("certainty", cbCertainty);
            AddValue("effective", effective.toCapTimeString());

            string onsettxt = cbOnset.Checked ? onset.toCapTimeString() : "";

            AddValue("onset", onsettxt, false, "", "onset", true);

            AddValue("expires", expires.toCapTimeString());

            AddValue("sendername", tbSenderName);

            AddValue("headline", tbHeadline);

            if (tbHeadline.Text.Length > 0)
                AddValue("alert_title", tbHeadline);

            AddValue("description", tbDescription);
            AddValue("web", tbWeb, false);
            AddValue("areadesc", tbAreadesc);

            if (rbPolygon.Checked)
                AddValue("area", tbPolygon , true, "Polygon", "polygon");
            else
                AddValue("area", tbCircle, true, "Circle", "circle");

            if (invalidString.Count > 0)
            {
                var sb = new StringBuilder();
                sb.AppendLine("以下欄位請務必輸入");
                sb.AppendLine();
                invalidString.ForEach(s => sb.AppendLine("      " + s));
                showValidError(sb.ToString());
            }
            else
            {

                var cap = CapTemplate.StringFormat(valueDicetonary);


                sfdSave.FileName = string.Format("{0}_{1}_{2}.cap", tbSender.Text, tbEventCode.Text, sent.ToString("yyyyMMddhhmm"));

                var result = sfdSave.ShowDialog();

                if (result == DialogResult.OK)
                {
                    File.WriteAllText(sfdSave.FileName, cap);

                    MessageBox.Show("完成存檔", "儲存檔案", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        Boolean checkReferences(string referenceText)
        {
            StringBuilder ErrorsOfRefer = new StringBuilder();
            List<string> References = new List<string>();
            References = referenceText.Replace("\n", String.Empty).Split(new Char[] { ' ' }).ToList();
            Regex regex = new Regex(@"^\d\d\d\d-\d\d-\d\dT\d\d:\d\d:\d\d[-,+]\d\d:\d\d$");
            DateTime tester = new DateTime();
            Boolean ispassed = false;

            foreach (var triplet in References)
            {
                int commaCheck = 0;
                MatchCollection mc;
                Regex r = new Regex(",");

                if (triplet == "")
                {
                    continue;
                }

                commaCheck = r.Matches(triplet).Count;

                if (commaCheck != 2)
                {
                    showValidError("references格式有誤(應為sender,identifier,sent三項一組並以空格分組)。此組填寫的內容為：[{0}]",  triplet );
                    ispassed = false;
                    break;
                }

                int firstComma = triplet.IndexOf(",");
                int secondComma = triplet.Substring(firstComma + 1, triplet.Length - firstComma - 1).IndexOf(",") + firstComma;

                if (firstComma == 0 && secondComma == 0)
                {
                    showValidError("references格式有誤(應為sender,identifier,sent三項一組並以空格分組)");
                    ispassed = false;
                    break;
                }
                if (firstComma == 0 && secondComma != 0)
                {
                    showValidError("references格式有誤(應為sender,identifier,sent三項一組並以空格分組)，其中缺少sender部分");
                    ispassed = false;
                    break;
                }

                if (firstComma != 0 && secondComma == 0)
                {
                    showValidError("references格式有誤(應為sender,identifier,sent三項一組並以空格分組)，其中缺少sent部分");
                    ispassed = false;
                    break;
                }
                if (secondComma == firstComma)
                {
                    showValidError("references格式有誤(應為sender,identifier,sent三項一組並以空格分組)，其中缺少identifier部分" );
                    ispassed = false;
                    break;
                }
                string senderOfRefer = triplet.Substring(0, firstComma);
                try
                {
                    string idOfRefer = triplet.Substring(firstComma + 1, secondComma - firstComma);
                }
                catch (Exception)
                {
                    showValidError("references格式有誤(應為sender,identifier,sent三項一組並以空格分組)");
                    ispassed = false;
                    break;
                }

                string sentOfRefer = triplet.Substring(secondComma + 2, triplet.Length - secondComma - 2);

                //檢查sent是否是標準時間格式
                if (!regex.IsMatch(sentOfRefer) || !DateTime.TryParse(sentOfRefer.Replace("T", " ").Replace("+08:00", ""), out tester))
                {
                    showValidError("references格式有誤(應為sender,identifier,sent三項一組並以空格分組)，其中sent非正確的時間格式");
                    ispassed = false;
                    break;
                }

                ispassed = true;
            }

            return ispassed;
        }

        void showValidError(string msg, params object[] args)
        {
            msg = string.Format(msg, args);

            MessageBox.Show(msg, "欄位檢核", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        

        Boolean CheckTime(DateTime sent, DateTime effective, DateTime onset, DateTime expires)
        {
            Boolean result = true;

            if (expires.IsSmallThan(DateTime.Now))
            {
                var dlg = MessageBox.Show("expires早於現在的時間，此示警將判定為失效，是否仍要儲存?", "欄位檢核", MessageBoxButtons.YesNo);
                result = dlg == DialogResult.Yes;
            }
            else if (cbOnset.Checked && expires.IsSmallThan(onset))
            {
                showValidError("onset必須早於expires" + Environment.NewLine + "結束時間 expires-{0} 早於開始時間 onset-{1}", expires, onset);
                result = false;
            }
            else if (expires.IsSmallThan(effective))
            {
                showValidError("expires必須晚於effective：結束時間 expires -{0}早於生效時間effective-{1}", expires, effective);
                result = false;
            }

            return result;
        }

        void AddValue(string name, object control, Boolean required = true, string aliasName="", string additionTag="", Boolean newLine=false)
        {
            
            string _name = "@" + name;

            string _value;

            switch (control.GetType().Name)
            {
                case "TextBox":
                    var txt = control as TextBox;
                    _value = txt.Text;
                    break;
                case "ComboBox":
                    var cb = control as ComboBox;
                    _value = cb.SelectedItem.ToString();
                    break;
                case "Label":
                    var lb = control as Label;
                    _value = lb.Text;
                    break;
                default:
                    _value = control.ToString();
                    break;
            }

            if (required && _value.Length == 0)
            {
                if (aliasName == "")
                    invalidString.Add(name);
                else
                    invalidString.Add(aliasName);
            }
            else
            {
                if (additionTag != "" && _value != "")
                    _value = string.Format("<{0}>{1}</{0}>", additionTag, _value);

                if (newLine)
                    _value = Environment.NewLine + _value;

                valueDicetonary.Add(_name, _value);
            }
        }

        DateTime GetDateTimeByName(string name)
        {
            
            var dtp = this.FindControl("dtp" + name + "Date") as DateTimePicker;
            var h = this.FindControl("nud" + name + "Hour") as NumericUpDown;
            var m = this.FindControl("nud" + name + "Minute") as NumericUpDown;
            var s = this.FindControl("nud" + name + "Second") as NumericUpDown;

            var dateObj = dtp.Value.Date;

            dateObj = dateObj.AddHours(Convert.ToInt32(h.Value)).AddMinutes(Convert.ToInt32(m.Value)).AddSeconds(Convert.ToInt32(s.Value));

            return dateObj;
        }

        private void btnImport_Click(object sender, EventArgs e)
        {

            var result = ofdReference.ShowDialog();


            if(result== DialogResult.OK)
            {

                List<string> refs = new List<string>();

                foreach (var f in ofdReference.FileNames)
                {

                    var xdoc = XDocument.Load(f);

                    var _identifier = xdoc.GetXmlValue("identifier");
                    var _sender = xdoc.GetXmlValue("sender");
                    var _sent = xdoc.GetXmlValue("sent");
                    
                    refs.Add(string.Format("{0},{1},{2}", _sender, _identifier, _sent));

                }

                string refResult = string.Join(" ", refs.ToArray());

                tbReference.Text = refResult;
            }

        }

        private void tTrigger_Tick(object sender, EventArgs e)
        {
            var idtxt = tbIdentifier.Text;
            var format = Properties.Settings.Default.IdentifierFormat;

            if (format.Contains("{1"))
                tbIdentifier.Text = string.Format(format, idtxt, DateTime.Now);
            else
                tbIdentifier.Text = string.Format(format, idtxt);

            tTrigger.Stop();
        }

        private void tsmAbout_Click(object sender, EventArgs e)
        {
            new About().ShowDialog();
        }

        private void tsmHelp_Click(object sender, EventArgs e)
        {
            new Help().ShowDialog();
        }

        private void cbOnset_CheckedChanged(object sender, EventArgs e)
        {
            var enable = cbOnset.Checked;

            dtpOnsetDate.Enabled = nudOnsetHour.Enabled = nudOnsetMinute.Enabled = nudOnsetSecond.Enabled = enable;
        }

        private void cbMsgType_SelectedIndexChanged(object sender, EventArgs e)
        {
            Boolean enabled = cbMsgType.SelectedIndex > 0;

            btnImport.Enabled = tbReference.Enabled = enabled;

            if (!enabled)
                tbReference.Clear();
        }
    }
}
