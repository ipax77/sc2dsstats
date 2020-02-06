using System;
using System.Collections.Generic;
using System.Text;

namespace sc2dsstats.lib.Models
{
    [Serializable]
    public class ChartJS
    {
        public string type { get; set; }
        public ChartJSData data { get; set; } = new ChartJSData();
        public ChartJsoptions options { get; set; }
    }

    [Serializable]
    public class ChartJsoptions
    {
        public bool responsive { get; set; } = true;
        public bool maintainAspectRatio { get; set; } = true;
        public ChartJSoptionsLegend legend { get; set; } = new ChartJSoptionsLegend();
        public ChartJSoptionsTitle title { get; set; } = new ChartJSoptionsTitle();
        public ChartJSoptionsScales scales { get; set; } = new ChartJSoptionsScales();
        //public ChartJSoptionsScale scale { get; set; }
    }

    [Serializable]
    public class ChartJsoptionsBar : ChartJsoptions
    {
        public ChartJSoptionselements elements { get; set; } = new ChartJSoptionselements();
        public ChartJSoptionsplugins plugins { get; set; } = new ChartJSoptionsplugins();
    }

    [Serializable]
    public class ChartJSoptionsScales
    {
        public List<ChartJSoptionsScalesY> yAxes { get; set; } = new List<ChartJSoptionsScalesY>();
    }

    [Serializable]
    public class ChartJSoptionsScalesY
    {
        public ChartJSoptionsScalesYLabel scaleLabel { get; set; } = new ChartJSoptionsScalesYLabel();
        public ChartJSoptionsScaleTicks ticks { get; set; } = new ChartJSoptionsScaleTicks();
    }

    [Serializable]
    public class ChartJSoptionsScalesYLabel
    {
        public bool display { get; set; } = true;
        public string labelString { get; set; } = "";
    }

    [Serializable]
    public class ChartJsoptionsradar : ChartJsoptions
    {
        public ChartJSoptionsScale scale { get; set; } = new ChartJSoptionsScale();
    }

    [Serializable]
    public class ChartJSoptionsLegend
    {
        public string position { get; set; } = "top";
        public ChartJSoptionslegendlabels labels { get; set; } = new ChartJSoptionslegendlabels();
    }

    [Serializable]
    public class ChartJSoptionslegendlabels
    {
        public int fontSize { get; set; } = 14;
        public string fontColor { get; set; } = "#eaffff";
    }

    [Serializable]
    public class ChartJSoptionsTitle
    {
        public bool display { get; set; } = true;
        public string text { get; set; }
        public int fontSize { get; set; } = 22;
        public string fontColor { get; set; } = "#eaffff";
    }

    [Serializable]
    public class ChartJSoptionselements
    {
        public ChartJSoptionselementsrectangle rectangle { get; set; } = new ChartJSoptionselementsrectangle();
    }

    [Serializable]
    public class ChartJSoptionselementsrectangle
    {
        public string backgroundColor { get; set; } = "cc55aa";
    }

    [Serializable]
    public class ChartJSoptionsScale
    {
        public ChartJSoptionsScaleTicksRadar ticks { get; set; } = new ChartJSoptionsScaleTicksRadar();
        public ChartJSoptionsradargridlines gridLines { get; set; } = new ChartJSoptionsradargridlines();
        public ChartJSoptionsradarangleLines angleLines { get; set; } = new ChartJSoptionsradarangleLines();
        public ChartJSoptionsradarpointLabels pointLabels { get; set; } = new ChartJSoptionsradarpointLabels();
    }

    [Serializable]
    public class ChartJSoptionsScaleTicks
    {
        public bool beginAtZero { get; set; } = false;
    }

    [Serializable]
    public class ChartJSoptionsScaleTicksRadar
    {
        public bool display { get; set; } = true;
        public bool beginAtZero { get; set; } = true;
        public string color = "#808080";
        public string backdropColor = "#041326";
    }

    [Serializable]
    public class ChartJSoptionsradargridlines
    {
        public string color { get; set; } = "#808080";
        public double lineWidth { get; set; } = 0.25;
    }

    [Serializable]
    public class ChartJSoptionsradarangleLines
    {
        public bool display { get; set; } = true;
        public string color { get; set; } = "#808080";
        public double lineWidth { get; set; } = 0.25;
    }

    [Serializable]
    public class ChartJSoptionsradarpointLabels
    {
        public int fontSize { get; set; } = 14;
        public string fontColor { get; set; } = "#46a2c9";
    }

    [Serializable]
    public class ChartJSoptionsplugins
    {
        public ChartJSoptionspluginsdatalabels datalabels { get; set; } = new ChartJSoptionspluginsdatalabels();
        public ChartJSPluginlabels labels { get; set; } = new ChartJSPluginlabels();
    }

    [Serializable]
    public class ChartJSoptionspluginsdatalabels
    {
        public string color = "#eaffff";
        public string align { get; set; } = "bottom";
        public string anchor { get; set; } = "end";
        //[JsonConverter(typeof(PlainJsonStringConverter))]
        //public string display { get; set; } = "function (context) { return context.dataset.data[context.dataIndex] > 15;";
        public string display { get; set; }
        public ChartJSoptionspluginsdatalabelsfont font { get; set; } = new ChartJSoptionspluginsdatalabelsfont();
        //[JsonConverter(typeof(PlainJsonStringConverter))]
        //public string formatter { get; set; } = "Math.Round";
    }

    [Serializable]
    public class ChartJSoptionspluginsdatalabelsfont
    {
        public string weight { get; set; } = "bold";
    }

    [Serializable]
    public class ChartJSData
    {
        public string[] labels { get; set; }
        public List<ChartJSdataset> datasets { get; set; } = new List<ChartJSdataset>();
    }

    [Serializable]
    public class ChartJSdataset
    {
        public string label { get; set; }
        public List<string> backgroundColor { get; set; } = new List<string>();
        public string borderColor { get; set; }
        public string pointBackgroundColor { get; set; }
        public int borderWidth { get; set; } = 1;
        public double[] data { get; set; }
    }

    [Serializable]
    public class ChartJSPluginlabels
    {
        public string render { get; set; } = "image";
        public List<ChartJSPluginlabelsImage> images { get; set; } = new List<ChartJSPluginlabelsImage>();
    }

    [Serializable]
    public class ChartJSPluginlabelsImage
    {
        public string src { get; set; } = "images/dummy.png";
        public int width { get; set; } = 45;
        public int height { get; set; } = 45;
    }

    [Serializable]
    public class PieChart
    {
        public List<double> piedata { get; set; } = new List<double>();
        public List<string> pielabels { get; set; } = new List<string>();
        public List<string> piecolors { get; set; } = new List<string>();
    }
}
