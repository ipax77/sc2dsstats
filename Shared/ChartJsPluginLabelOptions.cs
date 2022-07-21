using System.Collections.Generic;

namespace sc2dsstats._2022.Shared
{
    public class ChartJsPluginLabelOptions
    {
        public string render { get; set; } = "image";
        public bool showZero { get; set; } = true;
        public bool? arc { get; set; }
        public string? position { get; set; }
        public bool? showActualPercentages { get; set; }
        public int? fontSize { get; set; }
        public string? fontColor { get; set; }
        public bool? overlap { get; set; }
        public List<ChartJsPluginLabelOptionsImage> images { get; set; } = new List<ChartJsPluginLabelOptionsImage>();
    }

    public class ChartJsPluginLabelOptionsImage
    {
        public string src { get; set; } = "_content/sc2dsstats.rlib/images/btn-unit-hero-swann.png";
        public int width { get; set; } = 45;
        public int height { get; set; } = 45;
    }
}
