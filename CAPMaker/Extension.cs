using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace CAPMaker
{
    static class Extension
    {
        public static string StringFormat(this string format, IDictionary<string, object> values)
        {
            var matches = Regex.Matches(format, @"\{(.+?)\}");
            List<string> words = (from Match matche in matches select matche.Groups[1].Value).ToList();

            return words.Aggregate(
                format,
                (current, key) =>
                {
                    int colonIndex = key.IndexOf(':');
                    return current.Replace(
                    "{" + key + "}",
                    colonIndex > 0
                        ? string.Format("{0:" + key.Substring(colonIndex + 1) + "}", values[key.Substring(0, colonIndex)])
                        : values[key].ToString());
                });
        }

        public static string toCapTimeString(this DateTime d)
        {
            return d.ToString("yyyy-MM-ddTHH:mm:ss+08:00");
        }

        public static Boolean IsGreatThan(this DateTime d, DateTime td)
        {
            return d.Ticks > td.Ticks;
        }

        public static Boolean IsSmallThan(this DateTime d, DateTime td)
        {
            return d.Ticks < td.Ticks;
        }

        /// <summary>
        /// 按名稱查找控件
        /// </summary>
        /// <param name="container">查找控件的父容器控件</param>
        /// <param name="controlName">查找控件名稱</param>
        /// <returns>若沒有查找到返回NULL</returns>
        public static Control FindControl(this Control container, string controlName)
        {
            Control findControl = null;
            foreach (Control control in container.Controls)
            {
                if (control.Name == controlName)
                {
                    findControl = control;
                    break;
                }
                else if (control.Controls.Count > 0)
                {
                    findControl = FindControl(control, controlName);
                    if (findControl != null)
                        break;
                }
            }
            return findControl;
        }

        public static string GetXmlValue(this XDocument xml, string TagName)
        {

            var node = xml.Descendants().Where(n => n.Name.LocalName == TagName);

            return node.Any() ? node.First().Value : "";
        }
    }
}
