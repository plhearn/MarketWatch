using System;
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
        List<float> points = new List<float>();
        List<float> hodlPoints = new List<float>();
        List<float> divs = new List<float>();
        List<DateTime> dates = new List<DateTime>();
        float zoom = 1;
        private Timer timer1;
        public int interval = 2000;
        public List<float> centers = new List<float>();
        public List<float> dips = new List<float>();
        public List<float> dipPoints = new List<float>();
        float dipCooldown = 0;

        List<float> pctTally = new List<float>();
        List<float> pctTallyHodlMode = new List<float>();
        List<float> emaShort = new List<float>();
        List<float> emaLong = new List<float>();
        List<float> smasShort = new List<float>();
        List<float> smasLong = new List<float>();
        int emaShortLength = 130;
        int emaLongLength = 340;
        float divergence = 0;
        float divergencePrev = 0;
        float divergenceMax = 0;
        float divergenceMin = 0;

        float asks = 0;
        float bids = 0;
        float boughtAt = 0;
        float pctSum = 0;
        string strLog = "";
        string logPath = "";
        string strTradeAction = "";

        float buyThreshLong = 0.3f;
        float buyThresh = 0.5f;
        int buyTally = 50;
        int buyLongTally = 3000;
        float sellThreshLong = -0.3f;
        float sellThresh = -0.5f;
        int sellTally = 20;
        int sellLongTally = 6000;

        int chartStartIdx = 0;
        int chartEndIdx = 0;

        float tradeAmount = 0.00034f;

        int ticks = 0;

        bool testMode = false;
        bool tradeEnabled = false;

        int splitSpan = 250;
        float fit = 10f;

        Random r = new Random();

        private static readonly HttpClient client = new HttpClient();

        public enum tradeState
        {
            buy,
            sell,
            hodl
        }

        public List<tradeState> states = new List<tradeState>();

        public enum emaState
        {
            shortAbove,
            shortBelow,
            shortCrossedUp,
            shortCrossedDown
        }

        public List<emaState> emaStates = new List<emaState>();

        public struct testResult
        {
            public float buyThresh;
            public float sellThresh;
            public int buyTally;
            public int sellTally;
            public float gain;
            public float gainPct;
        }

        public struct testResultMA
        {
            public int emaShort;
            public int emaLong;
            public float gain;
            public float gainPct;
        }

        public List<testResult> results = new List<testResult>();
        public int testNum = 0;
        public float highestGain = -10;

        public string buyMsg = "";
        //public string buyException = "";
        public string buyMsgPrev = "";
        public string sellMsg = "";
        //public string buyExceptionPrev = "";
        public string sellMsgPrev = "";
        //public string sellExceptionPrev = "";
        
        Task<ulong> bID;
        Task<ulong> sID;

        public Form1()
        {
            InitializeComponent();

            logPath = DateTime.Now.ToString().Replace("/", "-").Replace(":", ".") + ".csv";
            strLog += "last" + ",," + "pct" + ",," + "tally" + ",," + "TimeStamp" + ",," + "Trade Action" + "\n";

            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (timer1 == null)
            {
                InitTimer();
                button1.Text = "Stop";
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
            //run_cmd(@"C:\Users\Peter\Desktop\tr\test.py", "");

            ticks++;
            
            getHttpBittrex();

        }

        public async void getHttpPolo()
        {
            var responseString = "";

            try
            {
                responseString = await client.GetStringAsync("https://poloniex.com/public?command=returnOrderBook&currencyPair=USDT_BTC&depth=1");
            }
            catch (TaskCanceledException e1)
            {
                return;
            }

            responseString = responseString.Replace("{\"asks\":[[\"", "");

            asks = float.Parse(responseString.Substring(0, responseString.IndexOf("\",")));

            string phrase = "\"bids\":[[\"";
            responseString = responseString.Substring(responseString.IndexOf(phrase) + phrase.Length, 30);

            bids = float.Parse(responseString.Substring(0, responseString.IndexOf("\"")));

            //{ "asks":[["2671.30000000",0.33697612]],"bids":[["2670.33904965",9.32269263]],"isFrozen":"0","seq":114438621}

            float lastPrice = (asks + bids) / 2f;

            centers.Add(lastPrice);

            if (centers.Count > 100)
                centers.RemoveAt(0);

            httpStrat();
            //centerLiteStrat();
        }

        public async void getHttpBittrex()
        {
            var responseString = "";


            try
            {
                responseString = await client.GetStringAsync("https://bittrex.com/api/v1.1/public/getorderbook?market=USDT-BTC&type=both&depth=1");
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


            string phrase = "\"Rate\":";
            int startIdx = responseString.IndexOf(phrase) + phrase.Length;
            responseString = responseString.Substring(startIdx, responseString.Length - startIdx);
            float asks = float.Parse(responseString.Substring(0, responseString.IndexOf("},{")));

            phrase = "sell\":[{\"Quantity\":";
            startIdx = responseString.IndexOf(phrase) + phrase.Length;
            responseString = responseString.Substring(startIdx, responseString.Length - startIdx);

            phrase = "\"Rate\":";
            startIdx = responseString.IndexOf(phrase) + phrase.Length;
            responseString = responseString.Substring(startIdx, responseString.Length - startIdx);

            float bids = float.Parse(responseString.Substring(0, responseString.IndexOf("},{")));


            float lastPrice = (asks + bids) / 2f;

            centers.Add(lastPrice);

            if (centers.Count > 100)
                centers.RemoveAt(0);

            httpStrat();
        }

        public void httpStrat()
        {
            float pctIncrease = 0;

            if (centers.Count > 1)
            {
                pctIncrease = centers[centers.Count - 1] - centers[centers.Count - 2];
                pctIncrease /= centers[centers.Count - 1];
                pctIncrease *= 100;
            }

            //pctIncrease /= interval;
            //pctIncrease *= 1000 * 60;

            pctTally.Add(pctIncrease);

            if (pctTally.Count > 10000)
                pctTally.Remove(0);

            pctSum = 0;

            int tallyLength = 100;// 300 * 1000 / interval;

            for (int i = 0; i < Math.Min(pctTally.Count, tallyLength); i++)
                pctSum += pctTally[pctTally.Count - 1 - i];

            points.Add(centers[centers.Count - 1]);

            if (states.Contains(tradeState.hodl))
                hodlPoints.Add(centers[centers.Count - 1]);
            else
                hodlPoints.Add(0);

            dipPoints.Add(0);

            points.RemoveAt(0);
            hodlPoints.RemoveAt(0);
            dipPoints.RemoveAt(0);
            

            strTradeAction = "";

            foreach (tradeState s in states)
                strTradeAction += s.ToString();


            float curDip = 0;
            float prevDip = 0;
            float logDipPct = 0;

            if (dips.Count > 0)
            {
                curDip = dips[dips.Count - 1];
                prevDip = dips[Math.Max(0, dips.Count - 2)];
                logDipPct = (curDip - prevDip) / curDip;
            }

            strLog += centers[centers.Count - 1].ToString("N2").Replace(",", "") + ",," + pctIncrease.ToString("N20") + ",," + pctSum.ToString("N20").Replace(",", "") + ",," + DateTime.Now + ",," + dipPoints[dipPoints.Count - 1].ToString("N2").Replace(",", "") + ",," + logDipPct.ToString("N10").Replace(",", "") + ",," + strTradeAction + "\n";

            File.AppendAllText(logPath, strLog);
            strLog = "";

            /*
            label1.Text = lastPrice.ToString() + "\n" + label1.Text;
            label2.Text = pctIncrease.ToString() + "\n" + label2.Text;
            label3.Text = pctSum.ToString() + "\n" + label3.Text;
            */
        }

        
        
    }
}
