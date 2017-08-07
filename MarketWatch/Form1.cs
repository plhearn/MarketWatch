using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
//using System.Windows.Forms.DataVisualization;
//using System.Windows.Forms.DataVisualization.Charting;

namespace MarketWatch
{
    public partial class Form1 : Form
    {
        private static readonly HttpClient client = new HttpClient();

        string logPath = "";
        string strLog = "";
        Timer timer1;
        int interval = 300000;
        int ticks = 0;
        int numUpdates = 0;

        protected struct coinData
        {
            public string name;
            public double marketCap;
            public double price;
            public double volume;
            public double fiveMin;
            public double hr;
            public double day;
            public double week;
            public int posTicks;
            public int negTicks;
            public List<int> posTicks10;
            public List<int> negTicks10;
            public double upRatio;
            public double upRatio10;
        }

        protected struct snapShot
        {
            public DateTime timeStamp;
            public List<coinData> coins;
        }

        List<snapShot> snapShots = new List<snapShot>();

        public Form1()
        {
            InitializeComponent();

            listView1.ListViewItemSorter = new Sorter();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (timer1 == null)
            {
                InitTimer();
                button1.Text = "Stop";
                timer1_Tick(new object(), new EventArgs());
            }
            else
            {
                timer1.Stop();
                timer1 = null;
                button1.Text = "Start";
            }
        }

        public void InitTimer()
        {
            timer1 = new Timer();
            timer1.Tick += new EventHandler(timer1_Tick);
            timer1.Interval = interval; // in miliseconds
            timer1.Start();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            ticks++;
            getHttpCMC();

        }
        
        public async void getHttpCMC()
        {
            label1.Text = "Updating";

            var responseString = "";


            try
            {
                responseString = await client.GetStringAsync("https://coinmarketcap.com/all/views/all/");
            }
            catch (TaskCanceledException e1)
            {
                //txtStatus.Text = e1.Message + "\n" + txtStatus.Text;
                return;
            }
            catch (HttpRequestException e1)
            {
                //txtStatus.Text = e1.Message + "\n" + txtStatus.Text;
                return;
            }

            if (snapShots.Count > 0)
                numUpdates++;

            //Debug.Print(responseString);

            string phrase = "<tbody>";
            int startIdx = responseString.IndexOf(phrase) + phrase.Length;
            phrase = "</tbody>";
            int endIdx = responseString.IndexOf(phrase);
            responseString = responseString.Substring(startIdx, endIdx - startIdx);

            //Debug.Print(responseString);

            snapShot s;

            s.timeStamp = DateTime.Now;
            s.coins = new List<coinData>();

            while (responseString.Contains("</tr>"))
            {
                coinData c;
                c.posTicks10 = new List<int>();
                c.negTicks10 = new List<int>();

                phrase = "<tr id=\"id-";
                startIdx = responseString.IndexOf(phrase) + phrase.Length;
                phrase = "\"  class=";
                endIdx = responseString.IndexOf(phrase);

                if (endIdx < 0)
                    endIdx = 10;
                string coinName = responseString.Substring(startIdx, endIdx - startIdx);
                responseString = responseString.Substring(endIdx + phrase.Length, responseString.Length - (endIdx + phrase.Length));

                phrase = "data-btc=\"";
                startIdx = responseString.IndexOf(phrase) + phrase.Length;
                phrase = "\" >";
                endIdx = responseString.IndexOf(phrase);

                if (endIdx < 0)
                    endIdx = 10;
                string strMarketCap = responseString.Substring(startIdx, endIdx - startIdx);
                double marketCap = 0;
                double.TryParse(strMarketCap, out marketCap);
                responseString = responseString.Substring(endIdx + phrase.Length, responseString.Length - (endIdx + phrase.Length));

                phrase = "data-btc=\"";
                startIdx = responseString.IndexOf(phrase) + phrase.Length;
                phrase = "\" >";
                endIdx = responseString.IndexOf(phrase);

                if (endIdx < 0)
                    endIdx = 10;
                string strPrice = responseString.Substring(startIdx, endIdx - startIdx);
                double price = 0;
                double.TryParse(strPrice, out price);
                responseString = responseString.Substring(endIdx + phrase.Length, responseString.Length - (endIdx + phrase.Length));

                phrase = "data-btc=\"";
                startIdx = responseString.IndexOf(phrase) + phrase.Length;
                phrase = "\" >";
                endIdx = responseString.IndexOf(phrase);

                if (endIdx < 0)
                    endIdx = 10;
                string strVolume = responseString.Substring(startIdx, endIdx - startIdx);
                double volume = 0;
                double.TryParse(strVolume, out volume);
                responseString = responseString.Substring(endIdx + phrase.Length, responseString.Length - (endIdx + phrase.Length));

                phrase = "data-btc=\"";
                startIdx = responseString.IndexOf(phrase) + phrase.Length;
                phrase = "\" >";
                endIdx = responseString.IndexOf(phrase);

                if (endIdx < 0)
                    endIdx = 10;
                string strHr = responseString.Substring(startIdx, endIdx - startIdx);
                double hr = 0;
                double.TryParse(strHr, out hr);
                responseString = responseString.Substring(endIdx + phrase.Length, responseString.Length - (endIdx + phrase.Length));

                phrase = "data-btc=\"";
                startIdx = responseString.IndexOf(phrase) + phrase.Length;
                phrase = "\" >";
                endIdx = responseString.IndexOf(phrase);

                if (endIdx < 0)
                    endIdx = 10;
                string strDay = responseString.Substring(startIdx, endIdx - startIdx);
                double day = 0;
                double.TryParse(strDay, out day);
                responseString = responseString.Substring(endIdx + phrase.Length, responseString.Length - (endIdx + phrase.Length));
                
                phrase = "data-btc=\"";
                startIdx = responseString.IndexOf(phrase) + phrase.Length;
                phrase = "\" >";
                endIdx = responseString.IndexOf(phrase);

                if (endIdx < 0)
                    endIdx = 10;
                string strWeek = responseString.Substring(startIdx, endIdx - startIdx);
                double week = 0;
                double.TryParse(strWeek, out week);
                responseString = responseString.Substring(endIdx + phrase.Length, responseString.Length - (endIdx + phrase.Length));
                
                phrase = "</tr>";
                startIdx = responseString.IndexOf(phrase) + phrase.Length;
                responseString = responseString.Substring(startIdx + phrase.Length, responseString.Length - (startIdx + phrase.Length));

                c.name = coinName;
                c.marketCap = marketCap;
                c.price = price;
                c.volume = volume;
                c.hr = hr;
                c.day = day;
                c.week = week;

                c.fiveMin = 0;
                c.posTicks = 0;
                c.negTicks = 0;
                c.upRatio = 0;
                c.upRatio10 = 0;

                if (snapShots.Count > 0)
                {
                    foreach (coinData d in snapShots[snapShots.Count - 1].coins)
                        if (d.name == c.name)
                        {
                            c.fiveMin = hr - d.hr;

                            c.posTicks = d.posTicks;
                            c.negTicks = d.negTicks;
                            c.posTicks10 = d.posTicks10;
                            c.negTicks10 = d.negTicks10;

                            if (c.fiveMin > 0)
                            {
                                c.posTicks++;
                                c.posTicks10.Add(1);
                                c.negTicks10.Add(0);
                            }
                            else if (c.fiveMin < 0)
                            {
                                c.negTicks++;
                                c.posTicks10.Add(0);
                                c.negTicks10.Add(1);
                            }
                            else
                            {
                                c.posTicks10.Add(0);
                                c.negTicks10.Add(0);
                            }

                            if (c.posTicks10.Count > 10)
                                c.posTicks10.RemoveAt(0);

                            if (c.negTicks10.Count > 10)
                                c.negTicks10.RemoveAt(0);

                            c.upRatio = (c.posTicks - c.negTicks) / (double)numUpdates;

                            int pos10sum = 0;
                            int neg10sum = 0;

                            foreach (int i in c.posTicks10)
                                pos10sum += i;

                            foreach (int i in c.negTicks10)
                                neg10sum += i;

                            c.upRatio10 = (pos10sum - neg10sum) / Math.Min((double)c.posTicks10.Count, 10.0);
                        }
                }
                


                if(c.volume > 300)
                    s.coins.Add(c);
            }

            snapShots.Add(s);

            /*
            strLog += "name" + ",," + "marketCap" + ",," + "price" + ",," + "volume" + ",," + "5min" + ",," + "hr" + ",," + "day" + ",," + "week" + "\n";

            foreach(coinData c in s.coins)
                strLog += c.name + ",," + c.marketCap + ",," + c.price + ",," + c.volume + ",," + c.fiveMin + ",," + c.hr + ",," + c.day + ",," + c.week + "\n";

            logPath = DateTime.Now.ToString().Replace("/", "-").Replace(":", ".") + ".csv";

            File.AppendAllText(logPath, strLog);
            */

            Stats();
        }

        public void Stats()
        {
            List<coinData> coins = snapShots[snapShots.Count - 1].coins.OrderByDescending(x => x.upRatio10).ThenByDescending(x => x.fiveMin).ToList();

            //label1.Text += "name" + ",," + "volume" + ",," + "5min" + ",," + "hr" + ",," + "day" + ",," + "week" + "\n";

            listView1.Items.Clear();

            for(int i=0; i< coins.Count; i++)
            {
                //label1.Text += coins[i].name + ",," + coins[i].volume + ",," + coins[i].fiveMin + ",," + coins[i].hr + ",," + coins[i].day + ",," + coins[i].week + "\n";

                string[] row = { coins[i].name, coins[i].volume.ToString(), coins[i].fiveMin.ToString(), coins[i].hr.ToString(), coins[i].day.ToString(), coins[i].week.ToString(), coins[i].upRatio.ToString(), coins[i].upRatio10.ToString() };
                var listViewItem = new ListViewItem(row);
                listView1.Items.Add(listViewItem);
            }

            label1.Text = "";
        }


        private int sortColumn = -1;
        private void listView1_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            Sorter s = (Sorter)listView1.ListViewItemSorter;
            s.Column = e.Column;

            if (s.Order == System.Windows.Forms.SortOrder.Ascending)
            {
                s.Order = System.Windows.Forms.SortOrder.Descending;
            }
            else
            {
                s.Order = System.Windows.Forms.SortOrder.Ascending;
            }
            listView1.Sort();
        }

        public class ListViewItemComparer : IComparer
        {

            private int col;
            private SortOrder order;
            public ListViewItemComparer()
            {
                col = 0;
                order = SortOrder.Ascending;
            }
            public ListViewItemComparer(int column, SortOrder order)
            {
                col = column;
                this.order = order;
            }
            public int Compare(object x, object y)
            {
                int returnVal = -1;
                returnVal = String.Compare(((ListViewItem)x).SubItems[col].Text,
                                ((ListViewItem)y).SubItems[col].Text);
                // Determine whether the sort order is descending.
                if (order == SortOrder.Descending)
                    // Invert the value returned by String.Compare.
                    returnVal *= -1;
                return returnVal;
            }


        }
        class Sorter : System.Collections.IComparer
        {
            public int Column = 0;
            public System.Windows.Forms.SortOrder Order = SortOrder.Ascending;
            public int Compare(object x, object y) // IComparer Member
            {
                if (!(x is ListViewItem))
                    return (0);
                if (!(y is ListViewItem))
                    return (0);

                ListViewItem l1 = (ListViewItem)x;
                ListViewItem l2 = (ListViewItem)y;

                if (l1.ListView.Columns[Column].Tag == null)
                {
                    l1.ListView.Columns[Column].Tag = "Text";
                }

                float f = 0;
                if (float.TryParse(l1.SubItems[Column].Text, out f))
                {
                    float fl1 = float.Parse(l1.SubItems[Column].Text);
                    float fl2 = float.Parse(l2.SubItems[Column].Text);

                    if (Order == SortOrder.Ascending)
                    {
                        return fl1.CompareTo(fl2);
                    }
                    else
                    {
                        return fl2.CompareTo(fl1);
                    }
                }
                else
                {
                    string str1 = l1.SubItems[Column].Text;
                    string str2 = l2.SubItems[Column].Text;

                    if (Order == SortOrder.Ascending)
                    {
                        return str1.CompareTo(str2);
                    }
                    else
                    {
                        return str2.CompareTo(str1);
                    }
                }
            }
        }
    }
}
